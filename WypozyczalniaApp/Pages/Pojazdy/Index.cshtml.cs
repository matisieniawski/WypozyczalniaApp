using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using System.IO;

namespace WypozyczalniaApp.Pages.Pojazdy
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public IndexModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        public IList<Pojazd> Pojazd { get; set; } = new List<Pojazd>();

        [BindProperty(SupportsGet = true)]
        public string SortColumn { get; set; } = "Status";

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; } = "Desc";

        [BindProperty(SupportsGet = true)]
        public string ModelFilter { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string ProducerFilter { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string VINFilter { get; set; } = string.Empty;

        public SelectList AvailableModels { get; set; } = default!;
        public SelectList AvailableProducers { get; set; } = default!;
        public SelectList AvailableStatuses { get; set; } = default!;

        public List<ModelPojazdu> AllModelsData { get; set; } = new List<ModelPojazdu>();


        public async Task OnGetAsync()
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();


            await LoadFilterListsAsync(connection);

            using (var command = connection.CreateCommand())
            {
 
                string sqlBase = @"
                    SELECT 
                        p.pojazd_id, p.numer_vin, p.status, p.model_id, p.numer_rejestracyjny, p.data_dodania,
                        m.nazwa_modelu, m.cena_za_dobe, m.producent_id, pr.nazwa
                    FROM pojazdy p
                    JOIN modelepojazdow m ON p.model_id = m.model_id
                    JOIN producenci pr ON m.producent_id = pr.producent_id";

                var sqlWhere = new StringBuilder(" WHERE 1=1 ");

                if (!string.IsNullOrEmpty(ModelFilter))
                {
                    sqlWhere.Append(" AND m.nazwa_modelu = @modelFilter");
                    AddParam(command, "@modelFilter", ModelFilter);
                }

                if (!string.IsNullOrEmpty(ProducerFilter))
                {
                    sqlWhere.Append(" AND pr.nazwa = @producerFilter");
                    AddParam(command, "@producerFilter", ProducerFilter);
                }

                if (!string.IsNullOrEmpty(StatusFilter))
                {
                    sqlWhere.Append(" AND p.status = @statusFilter");
                    AddParam(command, "@statusFilter", StatusFilter);
                }

                if (!string.IsNullOrEmpty(VINFilter))
                {
                    sqlWhere.Append(" AND (p.numer_vin ILIKE @vinFilter OR p.numer_rejestracyjny ILIKE @vinFilter)");
                    AddParam(command, "@vinFilter", $"%{VINFilter}%");
                }

                string sortField = SortColumn switch
                {
                    "Model" => "m.nazwa_modelu",
                    "Producent" => "pr.nazwa",
                    "VIN" => "p.numer_vin",
                    "DataDodania" => "p.data_dodania",
                    _ => "p.status"
                };

                string sortDir = SortDirection == "Asc" ? "ASC" : "DESC";

                string sqlOrderBy = $" ORDER BY {sortField} {sortDir}";


                command.CommandText = sqlBase + sqlWhere.ToString() + sqlOrderBy;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int vinIndex = 1;
                        int statusIndex = 2;
                        int regIndex = 4;
                        int dataDodaniaIndex = 5;
                        int modelNameIndex = 6;
                        int cenaDobaIndex = 7; 
                        int producentNameIndex = 9;

                        var nowyPojazd = new Pojazd
                        {
                            PojazdId = reader.GetInt32(0),
                            NumerVin = reader.GetString(vinIndex),
                            Status = reader.GetString(statusIndex),

                            NumerRejestracyjny = reader.IsDBNull(regIndex) ? null : reader.GetString(regIndex),
                            DataDodania = reader.GetDateTime(dataDodaniaIndex),

                            Model = new ModelPojazdu
                            {
                                NazwaModelu = reader.GetString(modelNameIndex),
                                CenaZaDobe = reader.GetDecimal(cenaDobaIndex),
                                Producent = new Producent
                                {
                                    Nazwa = reader.GetString(producentNameIndex)
                                }
                            }
                        };
                        Pojazd.Add(nowyPojazd);
                    }
                }
            }

            if (connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }

        public async Task<IActionResult> OnPostExportCsv()
        {
            await OnGetAsync();

            if (!Pojazd.Any())
            {
                TempData["ErrorMessage"] = "Brak pojazdów spe³niaj¹cych kryteria do wyeksportowania.";
                return RedirectToPage(new { SortColumn, SortDirection, ModelFilter, ProducerFilter, StatusFilter, VINFilter });
            }

            var builder = new StringBuilder();

            builder.AppendLine("Pojazd ID;VIN;Rejestracja;Model;Producent;Status;Data Dodania;Cena Dobowa");

            foreach (var pojazd in Pojazd)
            {
                builder.AppendLine($"{pojazd.PojazdId};{pojazd.NumerVin};{pojazd.NumerRejestracyjny};{pojazd.Model.NazwaModelu};{pojazd.Model.Producent.Nazwa};{pojazd.Status};{pojazd.DataDodania.ToShortDateString()};{pojazd.Model.CenaZaDobe}");
            }

            return File(Encoding.UTF8.GetBytes(builder.ToString()),
                        "text/csv",
                        $"Flota_Eksport_{DateTime.Now:yyyyMMdd}.csv");
        }


        private void AddParam(DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }

        private async Task LoadFilterListsAsync(DbConnection connection)
        {
            bool shouldClose = connection.State != ConnectionState.Open;
            if (shouldClose) await connection.OpenAsync();


            var producenci = new List<Producent>();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT DISTINCT nazwa FROM producenci ORDER BY nazwa";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        producenci.Add(new Producent { Nazwa = reader.GetString(0) });
                    }
                }
            }
            AvailableProducers = new SelectList(producenci, "Nazwa", "Nazwa", ProducerFilter);


            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT m.nazwa_modelu, pr.nazwa AS producent_nazwa
                    FROM modelepojazdow m
                    JOIN producenci pr ON m.producent_id = pr.producent_id
                    ORDER BY m.nazwa_modelu";

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        AllModelsData.Add(new ModelPojazdu
                        {
                            NazwaModelu = reader.GetString(0),
                            Producent = new Producent { Nazwa = reader.GetString(1) }
                        });
                    }
                }
            }

            var modele = AllModelsData.Select(m => m.NazwaModelu).Distinct().ToList();
            AvailableModels = new SelectList(modele, ModelFilter);

            AvailableStatuses = new SelectList(new List<string> { "Dostepny", "Wynajety", "Serwis" }, StatusFilter);

            if (shouldClose) await connection.CloseAsync();
        }
    }
}