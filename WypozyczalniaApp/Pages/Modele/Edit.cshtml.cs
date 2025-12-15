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

namespace WypozyczalniaApp.Pages.Modele
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
        public ModelPojazdu ModelPojazdu { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT model_id, producent_id, nazwa_modelu, cena_za_dobe 
                    FROM modelepojazdow 
                    WHERE model_id = @id";

                var param = command.CreateParameter();
                param.ParameterName = "@id";
                param.Value = id;
                command.Parameters.Add(param);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        ModelPojazdu = new ModelPojazdu
                        {
                            ModelId = reader.GetInt32(0),
                            ProducentId = reader.GetInt32(1),
                            NazwaModelu = reader.GetString(2),
                            CenaZaDobe = reader.GetDecimal(3)
                        };
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }

            var producenciList = await GetProducenciListAsync(connection);
            ViewData["ProducentId"] = new SelectList(producenciList, "ProducentId", "Nazwa");

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE modelepojazdow 
                        SET producent_id = @pid, nazwa_modelu = @nazwa, cena_za_dobe = @cena 
                        WHERE model_id = @id";

                    var p1 = command.CreateParameter();
                    p1.ParameterName = "@pid";
                    p1.Value = ModelPojazdu.ProducentId;
                    command.Parameters.Add(p1);

                    var p2 = command.CreateParameter();
                    p2.ParameterName = "@nazwa";
                    p2.Value = (object?)ModelPojazdu.NazwaModelu ?? DBNull.Value;
                    command.Parameters.Add(p2);

                    var p3 = command.CreateParameter();
                    p3.ParameterName = "@cena";
                    p3.Value = ModelPojazdu.CenaZaDobe;
                    command.Parameters.Add(p3);

                    var p4 = command.CreateParameter();
                    p4.ParameterName = "@id";
                    p4.Value = ModelPojazdu.ModelId;
                    command.Parameters.Add(p4);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected == 0) return NotFound();
                }
            }
            catch (PostgresException ex)
            {

                if (connection.State == ConnectionState.Open) await connection.CloseAsync();

                if (ex.SqlState == "23505")
                {
                    ModelState.AddModelError("ModelPojazdu.NazwaModelu", "Taki model ju¿ istnieje dla wybranego producenta.");
                }
                else if (ex.SqlState == "23502")
                {
                    ModelState.AddModelError(string.Empty, $"Pole {ex.ColumnName} jest wymagane.");
                }
                else if (ex.SqlState == "23514")
                {
                    if (ex.ConstraintName == "check_cena_modelu")
                        ModelState.AddModelError("ModelPojazdu.CenaZaDobe", "Cena za dobê musi byæ wiêksza od zera.");
                    else
                        ModelState.AddModelError(string.Empty, "Naruszenie regu³ poprawnoœci danych.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, $"B³¹d bazy danych: {ex.MessageText}");
                }

                await ReloadProducenciListAndReturn();
                return Page();
            }
            catch (Exception ex)
            {
                if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                ModelState.AddModelError(string.Empty, "Wyst¹pi³ b³¹d krytyczny: " + ex.Message);
                await ReloadProducenciListAndReturn();
                return Page();
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return RedirectToPage("./Index");
        }

        private async Task ReloadProducenciListAndReturn()
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();
            var list = await GetProducenciListAsync(connection);
            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            ViewData["ProducentId"] = new SelectList(list, "ProducentId", "Nazwa");
        }

        private async Task<List<Producent>> GetProducenciListAsync(DbConnection connection)
        {
            var list = new List<Producent>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT producent_id, nazwa FROM producenci ORDER BY nazwa";
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new Producent
                        {
                            ProducentId = reader.GetInt32(0),
                            Nazwa = reader.GetString(1)
                        });
                    }
                }
            }
            return list;
        }
    }
}