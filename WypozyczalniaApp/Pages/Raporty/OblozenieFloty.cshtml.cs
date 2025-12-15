using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Globalization;

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
            EndDate = EndDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);
            StartDate = StartDate ?? EndDate.Value.AddDays(-30);

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();


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

                var pStart = command.CreateParameter(); pStart.ParameterName = "@start"; pStart.Value = StartDate.Value.ToUniversalTime(); command.Parameters.Add(pStart);
                var pEnd = command.CreateParameter(); pEnd.ParameterName = "@end"; pEnd.Value = EndDate.Value.ToUniversalTime(); command.Parameters.Add(pEnd);

                command.CommandText = @"
                    SELECT 
                        SUM(
                            EXTRACT(DAY FROM (
                                LEAST(data_zwrotu_planowana, @end) - GREATEST(data_wypozyczenia, @start)
                            ))
                        )
                    FROM wynajmy
                    WHERE (data_wypozyczenia, data_zwrotu_planowana) OVERLAPS (@start::timestamp, @end::timestamp)
                    AND data_zwrotu_rzeczywista IS NULL";

                var wynajeteDni = await command.ExecuteScalarAsync();

                if (wynajeteDni != null && wynajeteDni != DBNull.Value)
                {
                    UtilizationData.Add(new FleetUtilization { Status = "Wynajête Dni", TotalDays = Convert.ToInt32(wynajeteDni) });
                }

                command.CommandText = @"
                    SELECT 
                        COUNT(s.serwis_id)
                    FROM serwisowanie s
                    WHERE s.data_serwisu BETWEEN @start AND @end";

                var serwisDni = await command.ExecuteScalarAsync();

                if (serwisDni != null && serwisDni != DBNull.Value)
                {
                    UtilizationData.Add(new FleetUtilization { Status = "Dni Serwisu", TotalDays = Convert.ToInt32(serwisDni) });
                }

                var totalDaysInPeriod = (int)(EndDate.Value - StartDate.Value).TotalDays;


            }

            if (connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }
}