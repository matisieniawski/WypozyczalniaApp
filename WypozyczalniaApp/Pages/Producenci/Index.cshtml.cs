using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;

namespace WypozyczalniaApp.Pages.Producenci
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public IndexModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        public class ProducentView : Producent
        {
            public int LiczbaAut { get; set; }
        }

        public IList<ProducentView> Producenci { get; set; } = new List<ProducentView>();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string SortColumn { get; set; } = "Nazwa";

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; } = "Asc";


        public async Task OnGetAsync()
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                string sqlBase = @"
                    SELECT 
                        pr.producent_id, pr.nazwa, 
                        COUNT(p.pojazd_id) AS LiczbaAut
                    FROM producenci pr
                    LEFT JOIN modelepojazdow m ON pr.producent_id = m.producent_id
                    LEFT JOIN pojazdy p ON m.model_id = p.model_id";

                var sqlWhere = new StringBuilder(" WHERE 1=1 ");

                if (!string.IsNullOrEmpty(SearchTerm))
                {
                    sqlWhere.Append(" AND pr.nazwa ILIKE @searchTerm");
                    var param = command.CreateParameter();
                    param.ParameterName = "@searchTerm";
                    param.Value = $"%{SearchTerm}%";
                    command.Parameters.Add(param);
                }


                string sqlGroupBy = " GROUP BY pr.producent_id, pr.nazwa";

                string sortField = SortColumn switch
                {
                    "Auta" => "LiczbaAut",
                    _ => "pr.nazwa"
                };

                string sortDir = SortDirection == "Asc" ? "ASC" : "DESC";

                string sqlOrderBy = $" ORDER BY {sortField} {sortDir}";

                command.CommandText = sqlBase + sqlWhere.ToString() + sqlGroupBy + sqlOrderBy;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Producenci.Add(new ProducentView
                        {
                            ProducentId = reader.GetInt32(0),
                            Nazwa = reader.GetString(1),
                            LiczbaAut = Convert.ToInt32(reader.GetInt64(2))
                        });
                    }
                }
            }

            if (connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }
}