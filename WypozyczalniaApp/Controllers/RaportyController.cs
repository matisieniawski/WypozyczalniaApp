using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;

namespace WypozyczalniaApp.Controllers
{
    [Authorize(Policy = "RequireManagerRole")]
    [Route("api/[controller]")]
    [ApiController]
    public class RaportyController : ControllerBase
    {
        private readonly WypozyczalniaDbContext _context;

        public RaportyController(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        [HttpGet("popularne_modele")]
        public async Task<IActionResult> GetPopularneModele()
        {
            var wyniki = new List<ModelPopularnosc>();
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT model, liczba_wynajmow FROM widok_popularnosc_modeli ORDER BY liczba_wynajmow DESC";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var rekord = new ModelPopularnosc
                        {
                            Model = reader.GetString(0),
                            LiczbaWynajmow = Convert.ToInt32(reader.GetValue(1))
                        };

                        wyniki.Add(rekord);
                    }
                }
            }

            if (connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }

            if (!wyniki.Any())
            {
                return Ok(new List<ModelPopularnosc>());
            }

            return Ok(wyniki);
        }

      
        [HttpGet("miesieczne_przychody")]
        public async Task<IActionResult> GetMonthlyRevenue()
        {
            var revenues = new List<MonthlyRevenue>();
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

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
                        revenues.Add(new MonthlyRevenue
                        {
                            MonthNumber = Convert.ToInt32(reader.GetDecimal(0)),
                            MonthName = reader.GetString(1).Trim(),
                            TotalRevenue = reader.GetDecimal(2)
                        });
                    }
                }
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return Ok(revenues);
        }
    }
}