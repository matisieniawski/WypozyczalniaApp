using System.Data;
using System.Data.Common;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;

namespace WypozyczalniaApp.Pages.Pracownicy
{
    [Authorize(Policy = "RequireManagerRole")]
    public class IndexModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public IndexModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        public class PracownikView : Pracownik
        {
            public string RoleList { get; set; } = string.Empty;
        }

        public IList<PracownikView> Pracownicy { get; set; } = new List<PracownikView>();


        [BindProperty(SupportsGet = true)]
        public string? SortColumn { get; set; } = "Nazwisko";

        [BindProperty(SupportsGet = true)]
        public string? SortDirection { get; set; } = "Asc";

        public async Task OnGetAsync()
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            string sortColumn = SortColumn switch
            {
                "Login" => "p.login",
                "Role" => "RoleList",
                _ => "p.nazwisko"
            };

            string sortDirection = SortDirection?.ToUpper() == "DESC" ? "DESC" : "ASC";


            using (var command = connection.CreateCommand())
            {

                command.CommandText = $@"
                    SELECT 
                        p.pracownik_id, p.imie, p.nazwisko, p.login, 
                        STRING_AGG(r.nazwa_roli, ', ') AS RoleList 
                    FROM pracownicy p
                    LEFT JOIN pracownicyrole pr ON p.pracownik_id = pr.pracownik_id
                    LEFT JOIN role r ON pr.rola_id = r.rola_id
                    GROUP BY p.pracownik_id, p.imie, p.nazwisko, p.login
                    ORDER BY {sortColumn} {sortDirection}";


                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Pracownicy.Add(new PracownikView
                        {
                            PracownikId = reader.GetInt32(0),
                            Imie = reader.GetString(1),
                            Nazwisko = reader.GetString(2),
                            Login = reader.GetString(3),
                            RoleList = reader.IsDBNull(4) ? "Brak" : reader.GetString(4)
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