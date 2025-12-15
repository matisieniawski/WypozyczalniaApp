using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;

namespace WypozyczalniaApp.Pages.Klienci
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public DeleteModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Klient Klient { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT klient_id, imie, nazwisko, email FROM klienci WHERE klient_id = @id";

                var param = command.CreateParameter();
                param.ParameterName = "@id";
                param.Value = id;
                command.Parameters.Add(param);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        Klient = new Klient
                        {
                            KlientId = reader.GetInt32(0),
                            Imie = reader.GetString(1),
                            Nazwisko = reader.GetString(2),
                            Email = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty
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

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM klienci WHERE klient_id = @id";

                    var param = command.CreateParameter();
                    param.ParameterName = "@id";
                    param.Value = id;
                    command.Parameters.Add(param);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected == 0) return NotFound();
                }
            }
            catch (DbException)
            {
                if (connection.State == ConnectionState.Open) await connection.CloseAsync();

                ModelState.AddModelError("", "Nie mo¿na usun¹æ klienta, poniewa¿ posiada on historiê wynajmów.");

                return await OnGetAsync(id);
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return RedirectToPage("./Index");
        }
    }
}