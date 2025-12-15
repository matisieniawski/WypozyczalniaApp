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
        public Producent Producent { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {

            ModelState.Remove("Producent.Modele");

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO producenci (nazwa) VALUES (@nazwa)";

                    var p1 = command.CreateParameter();
                    p1.ParameterName = "@nazwa";
                    p1.Value = (object?)Producent.Nazwa ?? DBNull.Value;
                    command.Parameters.Add(p1);

                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (PostgresException ex)
            {
                if (connection.State == ConnectionState.Open) await connection.CloseAsync();

                if (ex.SqlState == "23505")
                {
                    ModelState.AddModelError("Producent.Nazwa", "Taki producent ju¿ istnieje w bazie.");
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