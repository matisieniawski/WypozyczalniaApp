using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Npgsql;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;

namespace WypozyczalniaApp.Pages.Wynajmy
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public CreateModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Wynajem Wynajem { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadSelectListsAsync();

            Wynajem = new Wynajem
            {
                DataWypozyczenia = DateTime.Now,
                DataZwrotuPlanowana = DateTime.Now.AddDays(1)
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {

            ModelState.Remove("Wynajem.Klient");
            ModelState.Remove("Wynajem.Pojazd");
            ModelState.Remove("Wynajem.Pracownik");
            ModelState.Remove("Wynajem.LokalizacjaOdbioru");


            var pracownikIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int pracownikId = 1; 
            if (int.TryParse(pracownikIdStr, out int parsedId))
            {
                pracownikId = parsedId;
            }
            Wynajem.PracownikId = pracownikId;

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    decimal cenaZaDobe = 0;
                    using (var cmdCena = connection.CreateCommand())
                    {
                        cmdCena.Transaction = (DbTransaction)transaction;
                        cmdCena.CommandText = @"
                            SELECT m.cena_za_dobe 
                            FROM pojazdy p
                            JOIN modelepojazdow m ON p.model_id = m.model_id
                            WHERE p.pojazd_id = @id";

                        var pId = cmdCena.CreateParameter();
                        pId.ParameterName = "@id";
                        pId.Value = Wynajem.PojazdId;
                        cmdCena.Parameters.Add(pId);

                        var result = await cmdCena.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            cenaZaDobe = (decimal)result;
                        }
                        else
                        {
    
                            throw new Exception("Wybrany pojazd nie istnieje.");
                        }
                    }

                    var dni = (Wynajem.DataZwrotuPlanowana - Wynajem.DataWypozyczenia).TotalDays;
                    var dniDoRozliczenia = Math.Max(1, Math.Ceiling(dni));
                    Wynajem.KosztCalkowity = (decimal)dniDoRozliczenia * cenaZaDobe;

                    using (var cmdInsert = connection.CreateCommand())
                    {
                        cmdInsert.Transaction = (DbTransaction)transaction;
                        cmdInsert.CommandText = @"
                            INSERT INTO wynajmy (
                                klient_id, pojazd_id, pracownik_id, lokalizacja_odbioru_id, 
                                data_wypozyczenia, data_zwrotu_planowana, koszt_calkowity
                            ) VALUES (
                                @klient, @pojazd, @pracownik, @lokalizacja, 
                                @dataOd, @dataDo, @koszt
                            )";

                        AddParam(cmdInsert, "@klient", Wynajem.KlientId);
                        AddParam(cmdInsert, "@pojazd", Wynajem.PojazdId);
                        AddParam(cmdInsert, "@pracownik", Wynajem.PracownikId);
                        AddParam(cmdInsert, "@lokalizacja", Wynajem.LokalizacjaOdbioruId);
                        AddParam(cmdInsert, "@dataOd", DateTime.SpecifyKind(Wynajem.DataWypozyczenia, DateTimeKind.Utc));
                        AddParam(cmdInsert, "@dataDo", DateTime.SpecifyKind(Wynajem.DataZwrotuPlanowana, DateTimeKind.Utc));
                        AddParam(cmdInsert, "@koszt", Wynajem.KosztCalkowity);

                        await cmdInsert.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch (PostgresException ex)
                {
                    await transaction.RollbackAsync();

                    if (ex.SqlState == "P0001")
                    {
                        ModelState.AddModelError("", $"B³¹d walidacji (Trigger): {ex.MessageText}");
                    }
   
                    else if (ex.SqlState == "P0002")
                    {
                        ModelState.AddModelError("", $"Konflikt rezerwacji: {ex.MessageText}");
                    }
        
                    else if (ex.SqlState == "23514")
                    {
                        if (ex.ConstraintName == "check_daty_wynajmu")
                            ModelState.AddModelError("Wynajem.DataZwrotuPlanowana", "Data zwrotu musi byæ póŸniejsza ni¿ data wypo¿yczenia (B³¹d Bazy).");
                        else
                            ModelState.AddModelError("", "Naruszenie regu³ poprawnoœci danych.");
                    }
            
                    else
                    {
                        ModelState.AddModelError("", $"B³¹d bazy danych (SQL: {ex.SqlState}): {ex.MessageText}");
                    }

                    await LoadSelectListsAsync();
                    return Page();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Wyst¹pi³ b³¹d podczas tworzenia wynajmu: " + ex.Message);
                    await LoadSelectListsAsync();
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

        private async Task LoadSelectListsAsync()
        {
            var connection = _context.Database.GetDbConnection();
            bool shouldClose = false;
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
                shouldClose = true;
            }

    
            var klienci = new List<object>();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT klient_id, imie, nazwisko FROM klienci ORDER BY nazwisko";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        klienci.Add(new
                        {
                            Id = reader.GetInt32(0),
                            Nazwa = $"{reader.GetString(1)} {reader.GetString(2)}"
                        });
                    }
                }
            }
            ViewData["KlientId"] = new SelectList(klienci, "Id", "Nazwa");

 
            var lokalizacje = new List<Lokalizacja>();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT lokalizacja_id, nazwa FROM lokalizacje ORDER BY nazwa";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        lokalizacje.Add(new Lokalizacja
                        {
                            LokalizacjaId = reader.GetInt32(0),
                            Nazwa = reader.GetString(1)
                        });
                    }
                }
            }
            ViewData["LokalizacjaOdbioruId"] = new SelectList(lokalizacje, "LokalizacjaId", "Nazwa");

            var pojazdy = new List<object>();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT p.pojazd_id, m.nazwa_modelu, p.numer_vin, m.cena_za_dobe 
                    FROM pojazdy p
                    JOIN modelepojazdow m ON p.model_id = m.model_id
                    WHERE p.status = 'Dostepny'
                    ORDER BY m.nazwa_modelu";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        pojazdy.Add(new
                        {
                            Id = reader.GetInt32(0),
                            Opis = $"{reader.GetString(1)} ({reader.GetString(2)}) - {reader.GetDecimal(3):C}/doba"
                        });
                    }
                }
            }
            ViewData["PojazdId"] = new SelectList(pojazdy, "Id", "Opis");

            if (shouldClose) await connection.CloseAsync();
        }
    }
}