using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;

namespace WypozyczalniaApp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PojazdyApiController : ControllerBase
    {
        private readonly WypozyczalniaDbContext _context;

        public PojazdyApiController(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        [HttpGet("details/{pojazdId}")]
        public async Task<IActionResult> GetPojazdDetails(int pojazdId)
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            DateTime? ostatniSerwis = null;
            DateTime? ostatniWynajem = null;

            using (var cmdSerwis = connection.CreateCommand())
            {
                cmdSerwis.CommandText = @"
                    SELECT data_serwisu 
                    FROM serwisowanie 
                    WHERE pojazd_id = @id 
                    ORDER BY data_serwisu DESC 
                    LIMIT 1";

                var pId = cmdSerwis.CreateParameter();
                pId.ParameterName = "@id";
                pId.Value = pojazdId;
                cmdSerwis.Parameters.Add(pId);

                var result = await cmdSerwis.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    ostatniSerwis = (DateTime)result;
                }
            }

            using (var cmdWynajem = connection.CreateCommand())
            {
                cmdWynajem.CommandText = @"
                    SELECT data_zwrotu_rzeczywista 
                    FROM wynajmy 
                    WHERE pojazd_id = @id 
                    AND data_zwrotu_rzeczywista IS NOT NULL
                    ORDER BY data_zwrotu_rzeczywista DESC 
                    LIMIT 1";

                var pId = cmdWynajem.CreateParameter();
                pId.ParameterName = "@id";
                pId.Value = pojazdId;
                cmdWynajem.Parameters.Add(pId);

                var result = await cmdWynajem.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                {
                    ostatniWynajem = (DateTime)result;
                }
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            return Ok(new
            {
                ostatniSerwis = ostatniSerwis,
                ostatniWynajem = ostatniWynajem
            });
        }
    }
}