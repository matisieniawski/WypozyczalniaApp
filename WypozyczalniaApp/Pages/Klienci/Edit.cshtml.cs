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
    public class EditModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public EditModel(WypozyczalniaDbContext context)
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

        public async Task<IActionResult> OnPostAsync()
        {

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE klienci 
                        SET imie = @imie, nazwisko = @nazwisko, email = @email 
                        WHERE klient_id = @id";

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
                    p4.ParameterName = "@id";
                    p4.Value = Klient.KlientId;
                    command.Parameters.Add(p4);

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
                    ModelState.AddModelError("Klient.Email", "Ten adres email jest ju¿ zajêty przez innego klienta.");
                }
                else if (ex.SqlState == "23502")
                {
                    ModelState.AddModelError(string.Empty, $"Pole {ex.ColumnName} jest wymagane.");
                }

                else if (ex.SqlState == "23514")
                {
                    ModelState.AddModelError("Klient.Email", "Niepoprawny format adresu email.");
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