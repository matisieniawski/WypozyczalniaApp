using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;
using Npgsql;

namespace WypozyczalniaApp.Pages.Pojazdy
{
    [Authorize(Policy = "RequireManagerRole")]
    public class EditModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public EditModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Pojazd Pojazd { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT pojazd_id, model_id, numer_vin, status, numer_rejestracyjny, data_dodania 
                    FROM pojazdy 
                    WHERE pojazd_id = @id";

                var param = command.CreateParameter();
                param.ParameterName = "@id";
                param.Value = id;
                command.Parameters.Add(param);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        Pojazd = new Pojazd
                        {
                            PojazdId = reader.GetInt32(0),
                            ModelId = reader.GetInt32(1),
                            NumerVin = reader.GetString(2),
                            Status = reader.GetString(3),
                            NumerRejestracyjny = reader.IsDBNull(4) ? null : reader.GetString(4),
                            DataDodania = reader.GetDateTime(5)
                        };
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }

            var modele = await GetModeleListAsync(connection);
            ViewData["ModelId"] = new SelectList(modele, "ModelId", "NazwaModelu");

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {

            ModelState.Remove("Pojazd.Model");

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE pojazdy 
                        SET model_id = @modelId, numer_vin = @vin, status = @status, numer_rejestracyjny = @rejestracja, data_dodania = @dataDodania
                        WHERE pojazd_id = @id";

                    var p1 = command.CreateParameter(); p1.ParameterName = "@modelId"; p1.Value = Pojazd.ModelId; command.Parameters.Add(p1);
                    var p2 = command.CreateParameter(); p2.ParameterName = "@vin"; p2.Value = (object?)Pojazd.NumerVin ?? DBNull.Value; command.Parameters.Add(p2);
                    var p3 = command.CreateParameter(); p3.ParameterName = "@status"; p3.Value = (object?)Pojazd.Status ?? DBNull.Value; command.Parameters.Add(p3);


                    var p4 = command.CreateParameter(); p4.ParameterName = "@rejestracja"; p4.Value = (object?)Pojazd.NumerRejestracyjny ?? DBNull.Value; command.Parameters.Add(p4);

                    var p5 = command.CreateParameter(); p5.ParameterName = "@dataDodania"; p5.Value = DateTime.SpecifyKind(Pojazd.DataDodania, DateTimeKind.Utc); command.Parameters.Add(p5);

                    var p6 = command.CreateParameter(); p6.ParameterName = "@id"; p6.Value = Pojazd.PojazdId; command.Parameters.Add(p6);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected == 0) return NotFound();
                }
            }
            catch (PostgresException ex)
            {
                if (connection.State == ConnectionState.Open) await connection.CloseAsync();

                if (ex.SqlState == "23505")
                {
                    string msg = "Wprowadzona wartoœæ narusza unikalnoœæ danych.";
                    if (ex.ConstraintName?.Contains("vin") == true) msg = "Pojazd o podanym numerze VIN ju¿ istnieje.";
                    if (ex.ConstraintName?.Contains("rejestracyjny") == true) msg = "Pojazd o podanym numerze rejestracyjnym ju¿ istnieje.";

                    ModelState.AddModelError("", msg);
                }

                else if (ex.SqlState == "23502")
                {
                    ModelState.AddModelError(string.Empty, $"Pole {ex.ColumnName} jest wymagane.");
                }

                else if (ex.SqlState == "23514")
                {
                    ModelState.AddModelError("Pojazd.Status", "Niepoprawny status pojazdu.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, $"B³¹d bazy danych: {ex.MessageText}");
                }

                await ReloadModelListAndReturn();
                return Page();
            }
            catch (Exception ex)
            {
                if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                ModelState.AddModelError(string.Empty, "Wyst¹pi³ b³¹d krytyczny: " + ex.Message);
                await ReloadModelListAndReturn();
                return Page();
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return RedirectToPage("./Index");
        }

        private async Task ReloadModelListAndReturn()
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            var modele = await GetModeleListAsync(connection);
            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            ViewData["ModelId"] = new SelectList(modele, "ModelId", "NazwaModelu");
        }

        private async Task<List<ModelPojazdu>> GetModeleListAsync(DbConnection connection)
        {
            var lista = new List<ModelPojazdu>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT model_id, nazwa_modelu FROM modelepojazdow ORDER BY nazwa_modelu";
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        lista.Add(new ModelPojazdu
                        {
                            ModelId = reader.GetInt32(0),
                            NazwaModelu = reader.GetString(1)
                        });
                    }
                }
            }
            return lista;
        }
    }
}