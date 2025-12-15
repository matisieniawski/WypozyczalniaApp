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
    public class CreateModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public CreateModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Pojazd Pojazd { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            var modele = await GetModeleListAsync(connection);
            ViewData["ModelId"] = new SelectList(modele, "ModelId", "NazwaModelu");

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            Pojazd = new Pojazd();
            Pojazd.DataDodania = DateTime.Now;

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
                        INSERT INTO pojazdy (model_id, numer_vin, status, numer_rejestracyjny, data_dodania) 
                        VALUES (@modelId, @vin, @status, @rejestracja, @dataDodania)";

                    var p1 = command.CreateParameter();
                    p1.ParameterName = "@modelId";
                    p1.Value = Pojazd.ModelId;
                    command.Parameters.Add(p1);

                    var p2 = command.CreateParameter();
                    p2.ParameterName = "@vin";
                    p2.Value = (object?)Pojazd.NumerVin ?? DBNull.Value;
                    command.Parameters.Add(p2);

                    var p3 = command.CreateParameter();
                    p3.ParameterName = "@status";
                    p3.Value = (object?)Pojazd.Status ?? DBNull.Value;
                    command.Parameters.Add(p3);

                    var p4 = command.CreateParameter();
                    p4.ParameterName = "@rejestracja";
                    p4.Value = (object?)Pojazd.NumerRejestracyjny ?? DBNull.Value;
                    command.Parameters.Add(p4);

                    var p5 = command.CreateParameter();
                    p5.ParameterName = "@dataDodania";
                    p5.Value = DateTime.SpecifyKind(Pojazd.DataDodania, DateTimeKind.Utc);
                    command.Parameters.Add(p5);


                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (PostgresException ex)
            {
                if (connection.State == ConnectionState.Open) await connection.CloseAsync();

                if (ex.SqlState == "23505")
                {
                    string msg = "Wprowadzona wartoœæ narusza unikalnoœæ danych (duplikat).";
                    if (ex.ConstraintName?.Contains("vin") == true) msg = "Pojazd o podanym numerze VIN ju¿ istnieje w systemie.";
                    if (ex.ConstraintName?.Contains("rejestracyjny") == true) msg = "Pojazd o podanym numerze rejestracyjnym ju¿ istnieje w systemie.";

                    ModelState.AddModelError("", msg);
                }
                else if (ex.SqlState == "23502")
                {
                    ModelState.AddModelError("", $"Pole {ex.ColumnName} jest wymagane.");
                }
                else if (ex.SqlState == "23514")
                {
                    ModelState.AddModelError("Pojazd.Status", "Niepoprawny status pojazdu.");
                }
                else
                {
                    ModelState.AddModelError("", $"B³¹d bazy danych (SQL {ex.SqlState}): {ex.MessageText}");
                }

                await ReloadModelListAndReturn();
                return Page();
            }
            catch (Exception ex)
            {
                if (connection.State == ConnectionState.Open) await connection.CloseAsync();

                ModelState.AddModelError("", "Wyst¹pi³ b³¹d krytyczny: " + ex.Message);
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
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT model_id, nazwa_modelu FROM modelepojazdow ORDER BY nazwa_modelu";
                using (var reader = await cmd.ExecuteReaderAsync())
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