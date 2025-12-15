using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;
using Npgsql;
using System.Text; 

namespace WypozyczalniaApp.Pages.Wynajmy
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public IndexModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        public IList<Wynajem> Wynajmy { get; set; } = new List<Wynajem>();

        [BindProperty(SupportsGet = true)]
        public string ViewType { get; set; } = "Active";

        [BindProperty(SupportsGet = true)]
        public int? IdSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? VINSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? KlientSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFromSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortColumn { get; set; } = "PlanowanyZwrot";

        [BindProperty(SupportsGet = true)]
        public string? SortDirection { get; set; } = "Asc";
 

        public async Task OnGetAsync()
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            var sqlBuilder = new StringBuilder();
            sqlBuilder.Append(@"
                    SELECT 
                        w.wynajem_id, w.data_wypozyczenia, w.data_zwrotu_planowana, w.data_zwrotu_rzeczywista, w.koszt_calkowity,
                        k.imie, k.nazwisko, k.email,
                        p.numer_vin,
                        m.nazwa_modelu,
                        pr.login
                    FROM wynajmy w
                    JOIN klienci k ON w.klient_id = k.klient_id
                    JOIN pojazdy p ON w.pojazd_id = p.pojazd_id
                    JOIN modelepojazdow m ON p.model_id = m.model_id
                    JOIN pracownicy pr ON w.pracownik_id = pr.pracownik_id
                    WHERE 1=1 ");

            var parameters = new List<NpgsqlParameter>();


    
            if (ViewType == "History")
            {
                sqlBuilder.Append(" AND w.data_zwrotu_rzeczywista IS NOT NULL");
            }
            else
            {
                sqlBuilder.Append(" AND w.data_zwrotu_rzeczywista IS NULL");
            }

         
            if (IdSearch.HasValue)
            {
                sqlBuilder.Append(" AND w.wynajem_id = @idSearch");
                parameters.Add(new NpgsqlParameter("@idSearch", IdSearch.Value));
            }

            if (!string.IsNullOrWhiteSpace(VINSearch))
            {
                sqlBuilder.Append(" AND p.numer_vin ILIKE @vinSearch");
                parameters.Add(new NpgsqlParameter("@vinSearch", $"%{VINSearch}%"));
            }

            if (!string.IsNullOrWhiteSpace(KlientSearch))
            {
                sqlBuilder.Append(" AND (k.imie ILIKE @klientSearch OR k.nazwisko ILIKE @klientSearch)");
                parameters.Add(new NpgsqlParameter("@klientSearch", $"%{KlientSearch}%"));
            }

            if (DateFromSearch.HasValue)
            {
                sqlBuilder.Append(" AND w.data_wypozyczenia >= @dateFromSearch");
         
                parameters.Add(new NpgsqlParameter("@dateFromSearch", DateFromSearch.Value.Date));
            }

      
            string sortField = SortColumn switch
            {
                "Id" => "w.wynajem_id",
                "Klient" => "k.nazwisko",
                "Pojazd" => "p.numer_vin",
                "Wypozyczenie" => "w.data_wypozyczenia",
                _ => "w.data_zwrotu_planowana" 
            };

            string sortDir = SortDirection?.ToUpper() == "DESC" ? "DESC" : "ASC";

            sqlBuilder.Append($" ORDER BY {sortField} {sortDir}");

            using (var command = connection.CreateCommand())
            {
                command.CommandText = sqlBuilder.ToString();
                command.Parameters.AddRange(parameters.ToArray());

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var wynajem = new Wynajem
                        {
                            WynajemId = reader.GetInt32(0),
                            DataWypozyczenia = reader.GetDateTime(1),
                            DataZwrotuPlanowana = reader.GetDateTime(2),
                            DataZwrotuRzeczywista = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                            KosztCalkowity = reader.IsDBNull(4) ? null : reader.GetDecimal(4),

                            Klient = new Klient
                            {
                                Imie = reader.GetString(5),
                                Nazwisko = reader.GetString(6),
                                Email = reader.GetString(7)
                            },
                            Pojazd = new Pojazd
                            {
                                NumerVin = reader.GetString(8),
                                Model = new ModelPojazdu { NazwaModelu = reader.GetString(9) }
                            },
                            Pracownik = new Pracownik { Login = reader.GetString(10) }
                        };

                        Wynajmy.Add(wynajem);
                    }
                }
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();
        }

        public async Task<IActionResult> OnPostFinishAsync(int id)
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    int pojazdId = 0;
                    decimal? koszt = null;
                    bool juzZakonczony = false;
                    using (var cmdCheck = connection.CreateCommand())
                    {
                        cmdCheck.Transaction = (DbTransaction)transaction;
                        cmdCheck.CommandText = "SELECT pojazd_id, koszt_calkowity, data_zwrotu_rzeczywista FROM wynajmy WHERE wynajem_id = @id";

                        var pId = cmdCheck.CreateParameter(); pId.ParameterName = "@id"; pId.Value = id; cmdCheck.Parameters.Add(pId);

                        using (var reader = await cmdCheck.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                pojazdId = reader.GetInt32(0);
                                koszt = reader.IsDBNull(1) ? null : reader.GetDecimal(1);
                                juzZakonczony = !reader.IsDBNull(2);
                            }
                            else
                            {
                                await transaction.RollbackAsync();
                                return NotFound();
                            }
                        }
                    }

                    if (juzZakonczony)
                    {
                        await transaction.RollbackAsync();
                        return NotFound();
                    }

                    var nowUtc = DateTime.UtcNow;

       
                    using (var cmdUpdateRent = connection.CreateCommand())
                    {
                        cmdUpdateRent.Transaction = (DbTransaction)transaction;
                        cmdUpdateRent.CommandText = "UPDATE wynajmy SET data_zwrotu_rzeczywista = @now WHERE wynajem_id = @id";

                        var pNow = cmdUpdateRent.CreateParameter(); pNow.ParameterName = "@now"; pNow.Value = nowUtc; cmdUpdateRent.Parameters.Add(pNow);
                        var pId = cmdUpdateRent.CreateParameter(); pId.ParameterName = "@id"; pId.Value = id; cmdUpdateRent.Parameters.Add(pId);

                        await cmdUpdateRent.ExecuteNonQueryAsync();
                    }

                    using (var cmdUpdateCar = connection.CreateCommand())
                    {
                        cmdUpdateCar.Transaction = (DbTransaction)transaction;
                        cmdUpdateCar.CommandText = "UPDATE pojazdy SET status = 'Dostepny' WHERE pojazd_id = @pid";

                        var pPid = cmdUpdateCar.CreateParameter(); pPid.ParameterName = "@pid"; pPid.Value = pojazdId; cmdUpdateCar.Parameters.Add(pPid);

                        await cmdUpdateCar.ExecuteNonQueryAsync();
                    }

                    using (var cmdInvoice = connection.CreateCommand())
                    {
                        cmdInvoice.Transaction = (DbTransaction)transaction;
                        cmdInvoice.CommandText = @"
                            INSERT INTO faktury (wynajem_id, data_wystawienia, kwota_brutto) 
                            VALUES (@wid, @date, @amount)";

                        var pWid = cmdInvoice.CreateParameter(); pWid.ParameterName = "@wid"; pWid.Value = id; cmdInvoice.Parameters.Add(pWid);
                        var pDate = cmdInvoice.CreateParameter(); pDate.ParameterName = "@date"; pDate.Value = nowUtc; cmdInvoice.Parameters.Add(pDate);
                        var pAmount = cmdInvoice.CreateParameter(); pAmount.ParameterName = "@amount"; pAmount.Value = koszt ?? 0; cmdInvoice.Parameters.Add(pAmount);

                        await cmdInvoice.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = $"Pomyœlnie zakoñczono wynajem #{id} i wygenerowano fakturê.";
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Wyst¹pi³ b³¹d podczas zamykania wynajmu.";
                }
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return RedirectToPage(new { ViewType });
        }
    }
}