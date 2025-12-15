using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.Common;

namespace WypozyczalniaApp.Pages.Lokalizacje
{
    [Authorize(Policy = "RequireManagerRole")]
    public class IndexModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public IndexModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        public IList<Lokalizacja> Lokalizacje { get; set; } = new List<Lokalizacja>();

        public async Task OnGetAsync()
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT lokalizacja_id, nazwa, adres FROM lokalizacje ORDER BY nazwa";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var lokalizacja = new Lokalizacja
                        {
                            LokalizacjaId = reader.GetInt32(0),
                            Nazwa = reader.GetString(1),
                            Adres = reader.GetString(2)
                        };

                        Lokalizacje.Add(lokalizacja);
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