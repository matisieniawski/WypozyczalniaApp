using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using System.IO;

namespace WypozyczalniaApp.Pages.Faktury
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public IndexModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        public IList<Faktura> Faktury { get; set; } = new List<Faktura>();
        public SelectList AvailableStatuses { get; set; } = new SelectList(new List<string> { "Oplacona", "Nieoplacona", "Anulowana" });


        [BindProperty(SupportsGet = true)]
        public string SortColumn { get; set; } = "DataWystawienia";

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; } = "Desc";

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string ClientSearch { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string DateRange { get; set; } = string.Empty;


        public async Task OnGetAsync()
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                
                string sqlBase = @"
                    SELECT 
                        f.faktura_id, f.data_wystawienia, f.kwota_brutto, f.status_platnosci, f.wynajem_id,
                        k.imie, k.nazwisko, k.email,
                        w.data_wypozyczenia, w.data_zwrotu_planowana, w.data_zwrotu_rzeczywista,
                        po.numer_rejestracyjny, m.nazwa_modelu, pr.nazwa AS nazwa_producenta, po.numer_vin
                    FROM faktury f
                    JOIN wynajmy w ON f.wynajem_id = w.wynajem_id
                    JOIN klienci k ON w.klient_id = k.klient_id
                    JOIN pojazdy po ON w.pojazd_id = po.pojazd_id
                    JOIN modelepojazdow m ON po.model_id = m.model_id
                    JOIN producenci pr ON m.producent_id = pr.producent_id";

                var sqlWhere = new StringBuilder(" WHERE 1=1 ");


                var parameters = new List<NpgsqlParameter>();

                if (!string.IsNullOrEmpty(StatusFilter))
                {
                    sqlWhere.Append(" AND f.status_platnosci = @statusFilter");
                    var p = command.CreateParameter(); p.ParameterName = "@statusFilter"; p.Value = StatusFilter; command.Parameters.Add(p);
                }

                if (!string.IsNullOrEmpty(ClientSearch))
                {
                    sqlWhere.Append(" AND (k.imie ILIKE @clientSearch OR k.nazwisko ILIKE @clientSearch)");
                    var p = command.CreateParameter(); p.ParameterName = "@clientSearch"; p.Value = $"%{ClientSearch}%"; command.Parameters.Add(p);
                }

                
                string sortField = SortColumn switch
                {
                    "Kwota" => "f.kwota_brutto",
                    "Klient" => "k.nazwisko",
                    "Pojazd" => "po.numer_vin", 
                    "Status" => "f.status_platnosci", 
                    _ => "f.data_wystawienia"
                };

                string sortDir = SortDirection == "Asc" ? "ASC" : "DESC";

                command.CommandText = sqlBase + sqlWhere.ToString() + $" ORDER BY {sortField} {sortDir}";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Faktury.Add(new Faktura
                        {
                            FakturaId = reader.GetInt32(0),
                            DataWystawienia = reader.GetDateTime(1),
                            KwotaBrutto = reader.GetDecimal(2),
                            StatusPlatnosci = reader.GetString(3),
                            WynajemId = reader.GetInt32(4),

                            Wynajem = new Wynajem
                            {
                                
                                Klient = new Klient { Imie = reader.GetString(5), Nazwisko = reader.GetString(6), Email = reader.GetString(7) },
                                
                                DataWypozyczenia = reader.GetDateTime(8),
                                DataZwrotuPlanowana = reader.GetDateTime(9),
                                DataZwrotuRzeczywista = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                                
                                Pojazd = new Pojazd
                                {
                                    NumerRejestracyjny = reader.IsDBNull(11) ? null : reader.GetString(11),
                                    NumerVin = reader.GetString(14), 
                                    Model = new ModelPojazdu
                                    {
                                        Producent = new Producent { Nazwa = reader.GetString(13) },
                                        NazwaModelu = reader.GetString(12)
                                    }
                                }
                            }
                        });
                    }
                }
            }

            if (connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }

     
        public async Task<IActionResult> OnPostMarkPaidAsync(int fakturaId)
        {

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE faktury SET status_platnosci = 'Oplacona' WHERE faktura_id = @id AND status_platnosci != 'Oplacona'";
                    var pId = command.CreateParameter();
                    pId.ParameterName = "@id";
                    pId.Value = fakturaId;
                    command.Parameters.Add(pId);
                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        TempData["SuccessMessage"] = $"Faktura #{fakturaId} zosta³a oznaczona jako op³acona.";
                    }
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "B³¹d: Nie uda³o siê zaktualizowaæ statusu p³atnoœci.";
            }
            finally
            {
                if (connection.State == ConnectionState.Open) await connection.CloseAsync();
            }

            return RedirectToPage(new { SortColumn, SortDirection, StatusFilter, ClientSearch, DateRange });
        }


        public async Task<IActionResult> OnPostExportCsv()
        {
  
            await OnGetAsync();

            if (!Faktury.Any())
            {
                TempData["ErrorMessage"] = "Brak faktur do wyeksportowania.";
            
                return RedirectToPage(new { SortColumn, SortDirection, StatusFilter, ClientSearch, DateRange });
            }

            var builder = new StringBuilder();

           
            builder.AppendLine("Faktura ID;Data Wystawienia;Kwota Brutto;Status;Klient;Email Klienta;Pojazd (Model);Rejestracja;VIN");

            foreach (var faktura in Faktury)
            {
               
                builder.AppendLine(
                    $"{faktura.FakturaId};" +
                    $"{faktura.DataWystawienia.ToShortDateString()};" +
                    $"{faktura.KwotaBrutto.ToString().Replace(',', '.')};" +
                    $"{faktura.StatusPlatnosci};" +
                    $"{faktura.Wynajem.Klient.Nazwisko} {faktura.Wynajem.Klient.Imie};" +
                    $"{faktura.Wynajem.Klient.Email};" +
                    $"{faktura.Wynajem.Pojazd.Model.Producent.Nazwa} {faktura.Wynajem.Pojazd.Model.NazwaModelu};" +
                    $"{faktura.Wynajem.Pojazd.NumerRejestracyjny};" +
                    $"{faktura.Wynajem.Pojazd.NumerVin}"
                );
            }

      
            return File(Encoding.UTF8.GetBytes(builder.ToString()),
                        "text/csv",
                        $"Faktury_Eksport_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
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