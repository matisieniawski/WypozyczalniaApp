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
    public class EditModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public EditModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Producent Producent { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT producent_id, nazwa FROM producenci WHERE producent_id = @id";

                var param = command.CreateParameter();
                param.ParameterName = "@id";
                param.Value = id;
                command.Parameters.Add(param);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        Producent = new Producent
                        {
                            ProducentId = reader.GetInt32(0),
                            Nazwa = reader.GetString(1)
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

            ModelState.Remove("Producent.Modele");

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE producenci SET nazwa = @nazwa WHERE producent_id = @id";

                    var p1 = command.CreateParameter();
                    p1.ParameterName = "@nazwa";
                    p1.Value = (object?)Producent.Nazwa ?? DBNull.Value;
                    command.Parameters.Add(p1);

                    var p2 = command.CreateParameter();
                    p2.ParameterName = "@id";
                    p2.Value = Producent.ProducentId;
                    command.Parameters.Add(p2);

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