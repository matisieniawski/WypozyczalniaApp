using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using System.Data;
using System.Data.Common;
using System.Text;

namespace WypozyczalniaApp.Pages.Serwis
{
    [Authorize(Policy = "RequireManagerRole")]
    public class IndexModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public IndexModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        public IList<Pojazd> PojazdyFloty { get; set; } = new List<Pojazd>();

        [BindProperty(SupportsGet = true)]
        public string VINSearch { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string NameSearch { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int? IdSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string SortColumn { get; set; } = "Status";

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; } = "Desc";


        public async Task OnGetAsync()
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            string sortField = SortColumn switch
            {
                "Id" => "p.pojazd_id",
                "Model" => "m.nazwa_modelu",
                "VIN" => "p.numer_vin",
                "OstatniSerwis" => "p.data_ostatniego_serwisu",
                _ => "p.status"
            };
            string sortDir = SortDirection?.ToUpper() == "DESC" ? "DESC" : "ASC";

            using (var command = connection.CreateCommand())
            {
                var sqlBuilder = new StringBuilder();
                sqlBuilder.Append(@"
                    SELECT p.pojazd_id, p.numer_vin, p.status, p.numer_rejestracyjny, p.data_ostatniego_serwisu,
                           m.nazwa_modelu, pr.nazwa
                    FROM pojazdy p
                    JOIN modelepojazdow m ON p.model_id = m.model_id
                    JOIN producenci pr ON m.producent_id = pr.producent_id
                    WHERE 1=1 ");


                if (!string.IsNullOrEmpty(StatusFilter))
                {
                    sqlBuilder.Append(" AND p.status = @statusFilter");
                    AddParam(command, "@statusFilter", StatusFilter);
                }

 
                if (IdSearch.HasValue)
                {
                    sqlBuilder.Append(" AND p.pojazd_id = @idSearch");
                    AddParam(command, "@idSearch", IdSearch.Value);
                }

                if (!string.IsNullOrEmpty(VINSearch))
                {
                    sqlBuilder.Append(" AND p.numer_vin ILIKE @vinSearch");
                    AddParam(command, "@vinSearch", $"%{VINSearch}%");
                }
                if (!string.IsNullOrEmpty(NameSearch))
                {
                    sqlBuilder.Append(" AND (m.nazwa_modelu ILIKE @nameSearch OR pr.nazwa ILIKE @nameSearch)");
                    AddParam(command, "@nameSearch", $"%{NameSearch}%");
                }

                sqlBuilder.Append($" ORDER BY {sortField} {sortDir}");

                command.CommandText = sqlBuilder.ToString();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var pojazd = new Pojazd
                        {
                            PojazdId = reader.GetInt32(0),
                            NumerVin = reader.GetString(1),
                            Status = reader.GetString(2),
                            NumerRejestracyjny = reader.IsDBNull(3) ? null : reader.GetString(3),
                            DataOstatniegoSerwisu = reader.IsDBNull(4) ? null : reader.GetDateTime(4),

                            Model = new ModelPojazdu
                            {
                                NazwaModelu = reader.GetString(5),
                                Producent = new Producent
                                {
                                    Nazwa = reader.GetString(6)
                                }
                            }
                        };
                        PojazdyFloty.Add(pojazd);
                    }
                }
            }

            if (connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }

        public async Task<IActionResult> OnPostChangeStatusAsync(int id, string newStatus)
        {
            if (newStatus != "Serwis" && newStatus != "Dostepny")
            {
                return RedirectToPage();
            }

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    var nowUtc = DateTime.UtcNow;

                    using (var cmdUpdate = connection.CreateCommand())
                    {
                        cmdUpdate.Transaction = (DbTransaction)transaction;

                        string sqlUpdateCar = "UPDATE pojazdy SET status = @status";

                        if (newStatus == "Dostepny")
                        {
                            sqlUpdateCar += ", data_ostatniego_serwisu = @lastServiceDate";
                            AddParam(cmdUpdate, "@lastServiceDate", nowUtc);
                        }

                        sqlUpdateCar += " WHERE pojazd_id = @id";
                        cmdUpdate.CommandText = sqlUpdateCar;

                        AddParam(cmdUpdate, "@status", newStatus);
                        AddParam(cmdUpdate, "@id", id);

                        int rows = await cmdUpdate.ExecuteNonQueryAsync();
                        if (rows == 0)
                        {
                            await transaction.RollbackAsync();
                            TempData["ErrorMessage"] = "B³¹d: Pojazd nie zosta³ znaleziony.";
                            return RedirectToPage();
                        }
                    }

                    if (newStatus == "Serwis")
                    {
                        using (var cmdInsert = connection.CreateCommand())
                        {
                            cmdInsert.Transaction = (DbTransaction)transaction;
                            cmdInsert.CommandText = @"
                                INSERT INTO serwisowanie (pojazd_id, data_serwisu, opis) 
                                VALUES (@pid, @data, @opis)";

                            AddParam(cmdInsert, "@pid", id);
                            AddParam(cmdInsert, "@data", nowUtc);
                            AddParam(cmdInsert, "@opis", $"Wys³ano do serwisu przez {User.Identity!.Name}");

                            await cmdInsert.ExecuteNonQueryAsync();
                        }
                    }

                    await transaction.CommitAsync();

                    if (newStatus == "Dostepny")
                        TempData["SuccessMessage"] = $"Serwis pojazdu #{id} zakoñczono. Pojazd jest Dostêpny.";
                    else
                        TempData["SuccessMessage"] = $"Pojazd #{id} wys³ano do Serwisu.";
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Wyst¹pi³ b³¹d krytyczny podczas zmiany statusu serwisu.";
                    throw;
                }
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();


            return RedirectToPage(new { IdSearch, VINSearch, NameSearch, StatusFilter, SortColumn, SortDirection });
        }

        private void AddParam(DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }
    }
}