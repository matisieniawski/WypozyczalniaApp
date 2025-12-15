using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;
using Npgsql;

namespace WypozyczalniaApp.Pages.Lokalizacje
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
        public Lokalizacja Lokalizacja { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT lokalizacja_id, nazwa, adres FROM lokalizacje WHERE lokalizacja_id = @id";

                var param = command.CreateParameter();
                param.ParameterName = "@id";
                param.Value = id;
                command.Parameters.Add(param);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        Lokalizacja = new Lokalizacja
                        {
                            LokalizacjaId = reader.GetInt32(0),
                            Nazwa = reader.GetString(1),
                            Adres = reader.GetString(2)
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

        public async Task<IActionResult> OnPostAsync()
        {
           
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE lokalizacje SET nazwa = @nazwa, adres = @adres WHERE lokalizacja_id = @id";

                    var p1 = command.CreateParameter();
                    p1.ParameterName = "@nazwa";
                    p1.Value = (object?)Lokalizacja.Nazwa ?? DBNull.Value;
                    command.Parameters.Add(p1);

                    var p2 = command.CreateParameter();
                    p2.ParameterName = "@adres";
                    p2.Value = (object?)Lokalizacja.Adres ?? DBNull.Value;
                    command.Parameters.Add(p2);

                    var p3 = command.CreateParameter();
                    p3.ParameterName = "@id";
                    p3.Value = Lokalizacja.LokalizacjaId;
                    command.Parameters.Add(p3);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected == 0)
                    {
                        return NotFound(); 
                    }
                }
            }
            catch (PostgresException ex)
            {

                if (connection.State == ConnectionState.Open) await connection.CloseAsync();

                if (ex.SqlState == "23505")
                {
                    ModelState.AddModelError("Lokalizacja.Nazwa", "Oddzia³ o takiej nazwie ju¿ istnieje.");
                }

                else if (ex.SqlState == "23502")
                {
                    ModelState.AddModelError(string.Empty, $"Pole {ex.ColumnName} jest wymagane.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, $"B³¹d bazy danych: {ex.MessageText}");
                }

                return Page();
            }
            catch (Exception ex)
            {
                if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                ModelState.AddModelError(string.Empty, "Wyst¹pi³ b³¹d krytyczny: " + ex.Message);
                return Page();
            }


            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return RedirectToPage("./Index");
        }
    }
}