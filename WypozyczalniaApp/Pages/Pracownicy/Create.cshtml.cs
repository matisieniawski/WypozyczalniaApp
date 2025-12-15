using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;
using Npgsql;

namespace WypozyczalniaApp.Pages.Pracownicy
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
        public Pracownik Pracownik { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {

            ModelState.Remove("Pracownik.PracownicyRole");

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO pracownicy (imie, nazwisko, login, haslo_hash) VALUES (@imie, @nazwisko, @login, @haslo)";

                    var p1 = command.CreateParameter();
                    p1.ParameterName = "@imie";
                    p1.Value = (object?)Pracownik.Imie ?? DBNull.Value;
                    command.Parameters.Add(p1);

                    var p2 = command.CreateParameter();
                    p2.ParameterName = "@nazwisko";
                    p2.Value = (object?)Pracownik.Nazwisko ?? DBNull.Value;
                    command.Parameters.Add(p2);

                    var p3 = command.CreateParameter();
                    p3.ParameterName = "@login";
                    p3.Value = (object?)Pracownik.Login ?? DBNull.Value;
                    command.Parameters.Add(p3);

                    var p4 = command.CreateParameter();
                    p4.ParameterName = "@haslo";
                    p4.Value = (object?)Pracownik.HasloHash ?? DBNull.Value;
                    command.Parameters.Add(p4);

                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (PostgresException ex)
            {
                if (connection.State == ConnectionState.Open) await connection.CloseAsync();

                if (ex.SqlState == "23505")
                {
                    ModelState.AddModelError("Pracownik.Login", "Ten login jest ju¿ zajêty przez innego pracownika.");
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