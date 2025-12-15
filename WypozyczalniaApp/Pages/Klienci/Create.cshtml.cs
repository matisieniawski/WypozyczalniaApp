using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;
using Npgsql;

namespace WypozyczalniaApp.Pages.Klienci
{
    [Authorize]
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
        public Klient Klient { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO klienci (imie, nazwisko, email, numer_telefonu) VALUES (@imie, @nazwisko, @email, @telefon)";

                    var p1 = command.CreateParameter();
                    p1.ParameterName = "@imie";
                    p1.Value = (object?)Klient.Imie ?? DBNull.Value;
                    command.Parameters.Add(p1);

                    var p2 = command.CreateParameter();
                    p2.ParameterName = "@nazwisko";
                    p2.Value = (object?)Klient.Nazwisko ?? DBNull.Value;
                    command.Parameters.Add(p2);

                    var p3 = command.CreateParameter();
                    p3.ParameterName = "@email";
                    p3.Value = (object?)Klient.Email ?? DBNull.Value;
                    command.Parameters.Add(p3);

                    var p4 = command.CreateParameter();
                    p4.ParameterName = "@telefon";
                    p4.Value = (object?)Klient.NumerTelefonu ?? DBNull.Value;
                    command.Parameters.Add(p4);


                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (PostgresException ex)
            {

                if (connection.State == ConnectionState.Open) await connection.CloseAsync();

                if (ex.SqlState == "23505")
                {
                    ModelState.AddModelError("Klient.Email", "Ten adres email jest ju¿ zajêty (B³¹d Bazy: Unique Constraint).");
                }

                else if (ex.SqlState == "23502")
                {
                    ModelState.AddModelError(string.Empty, $"Pole '{ex.ColumnName ?? "nieznane"}' jest wymagane (B³¹d Bazy: Not Null Constraint).");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, $"B³¹d bazy danych (SQL: {ex.SqlState}): {ex.MessageText}");
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