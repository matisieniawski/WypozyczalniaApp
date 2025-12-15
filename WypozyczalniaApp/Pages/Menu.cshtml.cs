using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.Json; 


namespace WypozyczalniaApp.Pages
{

    public class MonthlyRevenue
    {
        public int MonthNumber { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
    }

    [Authorize]
    public class MenuModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public MenuModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        public int DostepnePojazdy { get; set; }
        public int WynajetePojazdy { get; set; }
        public int WSerwisiePojazdy { get; set; }
        public List<Pojazd> ListaPojazdow { get; set; } = new List<Pojazd>();

        public int TotalKlientow { get; set; }
        public decimal TotalPrzychodu { get; set; }
        public int TotalPojazdowDzialajacych { get; set; }

        public List<MonthlyRevenue> MonthlyRevenues { get; set; } = new List<MonthlyRevenue>();



        public async Task OnGetAsync()
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {
                await LoadFlotaStatsAsync(connection);
                await LoadFinancialStatsAsync(connection);
                await LoadOperationalStatsAsync(connection);
                await LoadMonthlyRevenueAsync(connection);

                ViewData["MonthlyRevenueJson"] = JsonSerializer.Serialize(MonthlyRevenues);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private async Task LoadFlotaStatsAsync(DbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT p.pojazd_id, p.numer_vin, p.status, 
                           m.nazwa_modelu, pr.nazwa
                    FROM pojazdy p
                    JOIN modelepojazdow m ON p.model_id = m.model_id
                    JOIN producenci pr ON m.producent_id = pr.producent_id";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var pojazd = new Pojazd
                        {
                            PojazdId = reader.GetInt32(0),
                            NumerVin = reader.GetString(1),
                            Status = reader.GetString(2),
                            Model = new ModelPojazdu
                            {
                                NazwaModelu = reader.GetString(3),
                                Producent = new Producent { Nazwa = reader.GetString(4) }
                            }
                        };
                        ListaPojazdow.Add(pojazd);
                    }
                }
            }

            DostepnePojazdy = ListaPojazdow.Count(p => p.Status == "Dostepny");
            WynajetePojazdy = ListaPojazdow.Count(p => p.Status == "Wynajety");
            WSerwisiePojazdy = ListaPojazdow.Count(p => p.Status == "Serwis");
        }

        private async Task LoadMonthlyRevenueAsync(DbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT 
                        EXTRACT(MONTH FROM data_wystawienia) AS month_num,
                        TO_CHAR(data_wystawienia, 'Month') AS month_name,
                        SUM(kwota_brutto) AS total_revenue
                    FROM faktury
                    WHERE status_platnosci = 'Oplacona' AND data_wystawienia >= DATE_TRUNC('year', NOW())
                    GROUP BY 1, 2
                    ORDER BY month_num";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        MonthlyRevenues.Add(new MonthlyRevenue
                        {
                            MonthNumber = Convert.ToInt32(reader.GetDecimal(0)),
                            MonthName = reader.GetString(1).Trim(),
                            TotalRevenue = reader.GetDecimal(2)
                        });
                    }
                }
            }
        }

        private async Task LoadFinancialStatsAsync(DbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT SUM(kwota_brutto) FROM faktury WHERE status_platnosci = 'Oplacona'";
                var result = await command.ExecuteScalarAsync();
                TotalPrzychodu = result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0m;
            }
        }

        private async Task LoadOperationalStatsAsync(DbConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(klient_id) FROM klienci";
                var result = await command.ExecuteScalarAsync();
                TotalKlientow = Convert.ToInt32(result);
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(pojazd_id) FROM pojazdy WHERE status IN ('Dostepny', 'Wynajety')";
                var result = await command.ExecuteScalarAsync();
                TotalPojazdowDzialajacych = Convert.ToInt32(result);
            }
        }

    }
}