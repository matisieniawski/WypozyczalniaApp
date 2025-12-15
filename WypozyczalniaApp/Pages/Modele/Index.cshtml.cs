using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace WypozyczalniaApp.Pages.Modele
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public IndexModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        public IList<ModelPojazdu> Modele { get; set; } = new List<ModelPojazdu>();

        [BindProperty(SupportsGet = true)]
        public string SortColumn { get; set; } = "Nazwa"; 

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; } = "Asc";

        [BindProperty(SupportsGet = true)]
        public string ProducerFilter { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string ModelFilter { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public decimal? MinPriceFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MaxPriceFilter { get; set; }



        public SelectList AvailableProducers { get; set; } = default!;
        public SelectList AvailableModels { get; set; } = default!;

        public List<ModelPojazdu> AllModelsData { get; set; } = new List<ModelPojazdu>();


        public async Task OnGetAsync()
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();


            await LoadFilterListsAsync(connection);

            using (var command = connection.CreateCommand())
            {

                string sqlBase = @"
                    SELECT m.model_id, m.nazwa_modelu, m.cena_za_dobe, m.producent_id, pr.nazwa
                    FROM modelepojazdow m
                    JOIN producenci pr ON m.producent_id = pr.producent_id";

                var sqlWhere = new StringBuilder(" WHERE 1=1 ");

                if (!string.IsNullOrEmpty(ProducerFilter))
                {
                    sqlWhere.Append(" AND pr.nazwa = @producerFilter");
                    AddParam(command, "@producerFilter", ProducerFilter);
                }

                if (!string.IsNullOrEmpty(ModelFilter))
                {
                    sqlWhere.Append(" AND m.nazwa_modelu = @modelFilter");
                    AddParam(command, "@modelFilter", ModelFilter);
                }

                if (MinPriceFilter.HasValue)
                {
                    sqlWhere.Append(" AND m.cena_za_dobe >= @minPrice");
                    AddParam(command, "@minPrice", MinPriceFilter.Value);
                }

                if (MaxPriceFilter.HasValue)
                {
                    sqlWhere.Append(" AND m.cena_za_dobe <= @maxPrice");
                    AddParam(command, "@maxPrice", MaxPriceFilter.Value);
                }


                string sortField = SortColumn switch
                {
                    "Producent" => "pr.nazwa",
                    "Cena" => "m.cena_za_dobe",
                    _ => "m.nazwa_modelu"
                };

                string sortDir = SortDirection == "Asc" ? "ASC" : "DESC";

                string sqlOrderBy = $" ORDER BY {sortField} {sortDir}";


                command.CommandText = sqlBase + sqlWhere.ToString() + sqlOrderBy;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var model = new ModelPojazdu
                        {
                            ModelId = reader.GetInt32(0),
                            NazwaModelu = reader.GetString(1),
                            CenaZaDobe = reader.GetDecimal(2),
                            ProducentId = reader.GetInt32(3),

                            Producent = new Producent
                            {
                                Nazwa = reader.GetString(4)
                            }
                        };
                        Modele.Add(model);
                    }
                }
            }

            if (connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
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
                cmd.CommandText = "SELECT DISTINCT producent_id, nazwa FROM producenci ORDER BY nazwa";
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        producenci.Add(new Producent { ProducentId = reader.GetInt32(0), Nazwa = reader.GetString(1) });
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

            if (shouldClose) await connection.CloseAsync();
        }
    }
}