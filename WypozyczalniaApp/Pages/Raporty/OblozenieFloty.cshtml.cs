using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Globalization;
using System.Linq;

namespace WypozyczalniaApp.Pages.Raporty
{
    [Authorize(Policy = "RequireManagerRole")]
    public class OblozenieFlotyModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public OblozenieFlotyModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        public class FleetUtilization
        {
            public string Status { get; set; } = string.Empty;
            public int TotalDays { get; set; }
        }

        public IList<FleetUtilization> UtilizationData { get; set; } = new List<FleetUtilization>();


        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        public int TotalPojazdow { get; set; }
        public int TotalWynajete { get; set; }
        public int TotalSerwis { get; set; }
        public int TotalDostepne { get; set; }


        public async Task OnGetAsync()
        {
   
            EndDate = EndDate ?? DateTime.Today.AddDays(1);
            StartDate = StartDate ?? EndDate.Value.AddDays(-30);


            var startUtc = DateTime.SpecifyKind(StartDate.Value.Date, DateTimeKind.Utc);
            var endUtc = DateTime.SpecifyKind(EndDate.Value.Date, DateTimeKind.Utc);

   
            if (endUtc.TimeOfDay.TotalSeconds == 0)
            {
                endUtc = endUtc.AddDays(1).AddSeconds(-1);
            }

            StartDate = startUtc;
            EndDate = endUtc;


            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT status, COUNT(pojazd_id) FROM pojazdy GROUP BY status";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var status = reader.GetString(0);
                            var count = Convert.ToInt32(reader.GetInt64(1)); 

                            if (status == "Dostepny") TotalDostepne = count;
                            if (status == "Wynajety") TotalWynajete = count;
                            if (status == "Serwis") TotalSerwis = count;
                        }
                    }
                    TotalPojazdow = TotalDostepne + TotalWynajete + TotalSerwis;
                }


                using (var command = connection.CreateCommand())
                {

                    command.CommandText = @"
                        SELECT
                            COALESCE(SUM(
                                DATE_PART('day', LEAST(data_zwrotu_planowana, @end) - GREATEST(data_wypozyczenia, @start))
                            ), 0)
                        FROM wynajmy
                        WHERE (data_wypozyczenia, data_zwrotu_planowana) OVERLAPS (@start::timestamp, @end::timestamp)";

                    var pStart = command.CreateParameter(); pStart.ParameterName = "@start"; pStart.Value = StartDate.Value; command.Parameters.Add(pStart);
                    var pEnd = command.CreateParameter(); pEnd.ParameterName = "@end"; pEnd.Value = EndDate.Value; command.Parameters.Add(pEnd);

                    var result = await command.ExecuteScalarAsync();

                    var rentedDays = result != null && result != DBNull.Value ? Convert.ToDouble(result) : 0;

                    UtilizationData.Add(new FleetUtilization { Status = "Wynajête Dni", TotalDays = (int)Math.Round(rentedDays) });
                }

                int totalSerwisEntries = 0;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT COUNT(DISTINCT pojazd_id)
                        FROM serwisowanie s
                        WHERE s.data_serwisu BETWEEN @start AND @end";

                    var pStart = command.CreateParameter(); pStart.ParameterName = "@start"; pStart.Value = StartDate.Value; command.Parameters.Add(pStart);
                    var pEnd = command.CreateParameter(); pEnd.ParameterName = "@end"; pEnd.Value = EndDate.Value; command.Parameters.Add(pEnd);

                    var result = await command.ExecuteScalarAsync();
                    totalSerwisEntries = Convert.ToInt32(result);
                }

                UtilizationData.Add(new FleetUtilization { Status = "Dni Serwisu", TotalDays = totalSerwisEntries });

                // Dni dostêpne to reszta
                var totalPeriodDays = (int)(EndDate.Value.Date - StartDate.Value.Date).TotalDays;
                var totalCapacity = TotalPojazdow * totalPeriodDays;
                var wynajeteDni = UtilizationData.FirstOrDefault(d => d.Status == "Wynajête Dni")?.TotalDays ?? 0;
                var serwisDni = UtilizationData.FirstOrDefault(d => d.Status == "Dni Serwisu")?.TotalDays ?? 0;

                var availableDays = Math.Max(0, totalCapacity - wynajeteDni - serwisDni);

                UtilizationData.Add(new FleetUtilization { Status = "Dostêpne Dni", TotalDays = availableDays });

            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}