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
    public class EditModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public EditModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Pracownik Pracownik { get; set; } = default!;

        public List<Rola> DostepneRole { get; set; } = new List<Rola>();

        [BindProperty]
        public List<int> WybraneRoleIds { get; set; } = new List<int>();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT pracownik_id, imie, nazwisko, login FROM pracownicy WHERE pracownik_id = @id";
                var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = id; cmd.Parameters.Add(p);

                using (var reader = await cmd.ExecuteReaderAsync())
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
                    else return NotFound();
                }
            }


            DostepneRole = await GetRolesAsync(connection);


            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT rola_id FROM pracownicyrole WHERE pracownik_id = @id";
                var p = cmd.CreateParameter(); p.ParameterName = "@id"; p.Value = id; cmd.Parameters.Add(p);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        WybraneRoleIds.Add(reader.GetInt32(0));
                    }
                }
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {

            ModelState.Remove("Pracownik.PracownicyRole");

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();


            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = (DbTransaction)transaction;
                        cmd.CommandText = @"
                            UPDATE pracownicy 
                            SET imie = @imie, nazwisko = @nazwisko, login = @login 
                            WHERE pracownik_id = @id";

                        AddParam(cmd, "@imie", (object?)Pracownik.Imie ?? DBNull.Value);
                        AddParam(cmd, "@nazwisko", (object?)Pracownik.Nazwisko ?? DBNull.Value);
                        AddParam(cmd, "@login", (object?)Pracownik.Login ?? DBNull.Value);
                        AddParam(cmd, "@id", Pracownik.PracownikId);

                        await cmd.ExecuteNonQueryAsync();
                    }


                    if (!string.IsNullOrWhiteSpace(Pracownik.HasloHash))
                    {
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.Transaction = (DbTransaction)transaction;
                            cmd.CommandText = "UPDATE pracownicy SET haslo_hash = @haslo WHERE pracownik_id = @id";
                            AddParam(cmd, "@haslo", Pracownik.HasloHash);
                            AddParam(cmd, "@id", Pracownik.PracownikId);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }


                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = (DbTransaction)transaction;
                        cmd.CommandText = "DELETE FROM pracownicyrole WHERE pracownik_id = @id";
                        AddParam(cmd, "@id", Pracownik.PracownikId);
                        await cmd.ExecuteNonQueryAsync();
                    }


                    foreach (var rolaId in WybraneRoleIds)
                    {
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.Transaction = (DbTransaction)transaction;
                            cmd.CommandText = "INSERT INTO pracownicyrole (pracownik_id, rola_id) VALUES (@pid, @rid)";
                            AddParam(cmd, "@pid", Pracownik.PracownikId);
                            AddParam(cmd, "@rid", rolaId);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    await transaction.CommitAsync();
                }
                catch (PostgresException ex)
                {
                    await transaction.RollbackAsync();

                    if (ex.SqlState == "23505") 
                    {
                        ModelState.AddModelError("Pracownik.Login", "Ten login jest ju¿ zajêty.");
                    }
                    else if (ex.SqlState == "23502")
                    {
                        ModelState.AddModelError(string.Empty, $"Pole {ex.ColumnName} jest wymagane.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, $"B³¹d bazy danych: {ex.MessageText}");
                    }

                    DostepneRole = await GetRolesAsync(connection);
                    return Page();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError(string.Empty, "Wyst¹pi³ b³¹d krytyczny: " + ex.Message);
                    DostepneRole = await GetRolesAsync(connection);
                    return Page();
                }
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return RedirectToPage("./Index");
        }

        private void AddParam(DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }

        private async Task<List<Rola>> GetRolesAsync(DbConnection connection)
        {
            var roles = new List<Rola>();

            bool openedLocally = false;
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
                openedLocally = true;
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT rola_id, nazwa_roli FROM role ORDER BY nazwa_roli";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        roles.Add(new Rola { RolaId = reader.GetInt32(0), NazwaRoli = reader.GetString(1) });
                    }
                }
            }

            if (openedLocally) await connection.CloseAsync();
            return roles;
        }
    }
}