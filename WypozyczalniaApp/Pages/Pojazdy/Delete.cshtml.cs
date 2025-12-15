using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;

namespace WypozyczalniaApp.Pages.Pojazdy
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
        public Pojazd Pojazd { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT p.pojazd_id, p.numer_vin, p.status, m.nazwa_modelu, p.numer_rejestracyjny, p.data_dodania 
                    FROM pojazdy p
                    JOIN modelepojazdow m ON p.model_id = m.model_id
                    WHERE p.pojazd_id = @id";

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
                            NumerVin = reader.GetString(1),
                            Status = reader.GetString(2),
                            Model = new ModelPojazdu { NazwaModelu = reader.GetString(3) },
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

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM pojazdy WHERE pojazd_id = @id";

                var param = command.CreateParameter();
                param.ParameterName = "@id";
                param.Value = id;
                command.Parameters.Add(param);

                int rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected == 0) return NotFound();
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return RedirectToPage("./Index");
        }
    }
}