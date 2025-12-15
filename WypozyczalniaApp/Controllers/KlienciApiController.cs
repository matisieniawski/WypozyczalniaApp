using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;

namespace WypozyczalniaApp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class KlienciApiController : ControllerBase
    {
        private readonly WypozyczalniaDbContext _context;

        public KlienciApiController(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        public class RentalDetail
        {
            public string NumerRejestracyjny { get; set; } = string.Empty;
            public int PojazdId { get; set; }
            public string MarkaModel { get; set; } = string.Empty;
            public DateTime DataWynajmu { get; set; }
            public DateTime DataZwrotuPlanowana { get; set; }
        }

        [HttpGet("rentals/{klientId}")]
        public async Task<IActionResult> GetActiveRentals(int klientId)
        {
            var rentals = new List<RentalDetail>();
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT 
                        p.pojazd_id,              -- 0
                        p.numer_rejestracyjny,    -- 1
                        pr.nazwa,                 -- 2
                        m.nazwa_modelu,           -- 3
                        w.data_wypozyczenia,      -- 4
                        w.data_zwrotu_planowana    -- 5
                    FROM wynajmy w
                    JOIN pojazdy p ON w.pojazd_id = p.pojazd_id
                    JOIN modelepojazdow m ON p.model_id = m.model_id
                    JOIN producenci pr ON m.producent_id = pr.producent_id
                    WHERE w.klient_id = @klientId 
                      AND w.data_zwrotu_rzeczywista IS NULL
                    ORDER BY w.data_wypozyczenia DESC";

                var pId = command.CreateParameter();
                pId.ParameterName = "@klientId";
                pId.Value = klientId;
                command.Parameters.Add(pId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        rentals.Add(new RentalDetail
                        {
                            PojazdId = reader.GetInt32(0),
                            NumerRejestracyjny = reader.IsDBNull(1) ? "Brak" : reader.GetString(1),
                            MarkaModel = $"{reader.GetString(2)} {reader.GetString(3)}",
                            DataWynajmu = reader.GetDateTime(4),
                            DataZwrotuPlanowana = reader.GetDateTime(5)
                        });
                    }
                }
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return Ok(rentals);
        }
    }
}