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
    public class CreateModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public CreateModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Lokalizacja Lokalizacja { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO lokalizacje (nazwa, adres) VALUES (@nazwa, @adres)";

                    var p1 = command.CreateParameter();
                    p1.ParameterName = "@nazwa";
                    p1.Value = (object?)Lokalizacja.Nazwa ?? DBNull.Value;
                    command.Parameters.Add(p1);

                    var p2 = command.CreateParameter();
                    p2.ParameterName = "@adres";
                    p2.Value = (object?)Lokalizacja.Adres ?? DBNull.Value;
                    command.Parameters.Add(p2);

                    await command.ExecuteNonQueryAsync();
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