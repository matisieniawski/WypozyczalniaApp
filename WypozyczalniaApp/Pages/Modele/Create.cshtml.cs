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
    public class CreateModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public CreateModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            var task = GetProducenciListAsync();
            task.Wait();
            ViewData["ProducentId"] = new SelectList(task.Result, "ProducentId", "Nazwa");
            return Page();
        }

        [BindProperty]
        public ModelPojazdu ModelPojazdu { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {
            
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO modelepojazdow (producent_id, nazwa_modelu, cena_za_dobe) 
                        VALUES (@producentId, @nazwa, @cena)";

               
                    var p1 = command.CreateParameter();
                    p1.ParameterName = "@producentId";
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

                    await command.ExecuteNonQueryAsync();
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
                    {
                        ModelState.AddModelError("ModelPojazdu.CenaZaDobe", "Cena za dobê musi byæ wiêksza od zera.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Naruszenie regu³ poprawnoœci danych.");
                    }
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
            var producenciList = await GetProducenciListAsync();
            ViewData["ProducentId"] = new SelectList(producenciList, "ProducentId", "Nazwa");
        }

        private async Task<List<Producent>> GetProducenciListAsync()
        {
            var list = new List<Producent>();
            var connection = _context.Database.GetDbConnection();

            bool shouldClose = false;
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
                shouldClose = true;
            }

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

            if (shouldClose) await connection.CloseAsync();

            return list;
        }
    }
}