using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;
using Npgsql;

namespace WypozyczalniaApp.Pages.Producenci
{
    [Authorize(Policy = "RequireManagerRole")]
    public class DeleteModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public DeleteModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Producent Producent { get; set; } = default!;
        public int LiczbaPowiazanychAut { get; set; } = 0;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT producent_id, nazwa FROM producenci WHERE producent_id = @id";
                var param = command.CreateParameter(); param.ParameterName = "@id"; param.Value = id; command.Parameters.Add(param);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        Producent = new Producent { ProducentId = reader.GetInt32(0), Nazwa = reader.GetString(1) };
                    }
                    else return NotFound();
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT COUNT(p.pojazd_id)
                    FROM pojazdy p
                    JOIN modelepojazdow m ON p.model_id = m.model_id
                    WHERE m.producent_id = @id";

                var param = command.CreateParameter(); param.ParameterName = "@id"; param.Value = id; command.Parameters.Add(param);

                var result = await command.ExecuteScalarAsync();
                LiczbaPowiazanychAut = Convert.ToInt32(result);
            }


            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            if (LiczbaPowiazanychAut > 0)
            {
                TempData["ErrorMessage"] = $"Nie mo¿na usun¹æ producenta '{Producent.Nazwa}', poniewa¿ ma przypisanych {LiczbaPowiazanychAut} pojazdów w systemie.";
                return RedirectToPage("./Index");
            }


            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();


            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM producenci WHERE producent_id = @id";
                var param = command.CreateParameter(); param.ParameterName = "@id"; param.Value = id; command.Parameters.Add(param);
                await command.ExecuteNonQueryAsync();
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return RedirectToPage("/Producenci/Index");
        }
    }
}