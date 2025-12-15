using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;

namespace WypozyczalniaApp.Pages.Pracownicy
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
        public Pracownik Pracownik { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT pracownik_id, imie, nazwisko, login FROM pracownicy WHERE pracownik_id = @id";

                var param = command.CreateParameter();
                param.ParameterName = "@id";
                param.Value = id;
                command.Parameters.Add(param);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        Pracownik = new Pracownik
                        {
                            PracownikId = reader.GetInt32(0),
                            Imie = reader.GetString(1),
                            Nazwisko = reader.GetString(2),
                            Login = reader.GetString(3)
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

            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {

                    using (var cmdRoles = connection.CreateCommand())
                    {
                        cmdRoles.Transaction = (DbTransaction)transaction;
                        cmdRoles.CommandText = "DELETE FROM pracownicyrole WHERE pracownik_id = @id";

                        var p1 = cmdRoles.CreateParameter();
                        p1.ParameterName = "@id";
                        p1.Value = id;
                        cmdRoles.Parameters.Add(p1);

                        await cmdRoles.ExecuteNonQueryAsync();
                    }

                    using (var cmdEmp = connection.CreateCommand())
                    {
                        cmdEmp.Transaction = (DbTransaction)transaction;
                        cmdEmp.CommandText = "DELETE FROM pracownicy WHERE pracownik_id = @id";

                        var p2 = cmdEmp.CreateParameter();
                        p2.ParameterName = "@id";
                        p2.Value = id;
                        cmdEmp.Parameters.Add(p2);

                        int rowsAffected = await cmdEmp.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            await transaction.RollbackAsync();
                            return NotFound();
                        }
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw; 
                }
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return RedirectToPage("./Index");
        }
    }
}