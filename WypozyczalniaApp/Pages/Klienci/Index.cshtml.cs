using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using System.Data;
using System.Data.Common;
using Npgsql;
using System.Text;
using System.Linq;

namespace WypozyczalniaApp.Pages.Klienci
{
    [Authorize(Policy = "RequireManagerRole")]
    public class IndexModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        public IndexModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        public IList<Klient> Klienci { get; set; } = new List<Klient>();

        [BindProperty(SupportsGet = true)]
        public int? IdSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ImieSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? NazwiskoSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? EmailSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortColumn { get; set; } = "Nazwisko";

        [BindProperty(SupportsGet = true)]
        public string? SortDirection { get; set; } = "Asc";
    
        [BindProperty(SupportsGet = true)]
        public int? SelectedKlientId { get; set; }

        public class WypozyczoneAuto
        {
            public string NumerRejestracyjny { get; set; } = string.Empty;
            public int PojazdId { get; set; }
            public string MarkaModel { get; set; } = string.Empty;
            public DateTime DataWynajmu { get; set; }
            public DateTime DataZwrotu { get; set; }
        }

        public IList<WypozyczoneAuto> CurrentRentals { get; set; } = new List<WypozyczoneAuto>();
        public Klient? SelectedKlient { get; set; }


        public async Task OnGetAsync()
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();


            var queryBuilder = new System.Text.StringBuilder();
            queryBuilder.Append("SELECT klient_id, imie, nazwisko, email, numer_telefonu FROM klienci WHERE 1=1");

            var parameters = new List<NpgsqlParameter>();

            if (IdSearch.HasValue)
            {
                queryBuilder.Append(" AND klient_id = @idSearch");
                parameters.Add(new NpgsqlParameter("@idSearch", IdSearch.Value));
            }

            if (!string.IsNullOrWhiteSpace(ImieSearch))
            {
                queryBuilder.Append(" AND imie ILIKE @imieSearch");
                parameters.Add(new NpgsqlParameter("@imieSearch", $"%{ImieSearch}%"));
            }

            if (!string.IsNullOrWhiteSpace(NazwiskoSearch))
            {
                queryBuilder.Append(" AND nazwisko ILIKE @nazwiskoSearch");
                parameters.Add(new NpgsqlParameter("@nazwiskoSearch", $"%{NazwiskoSearch}%"));
            }

            if (!string.IsNullOrWhiteSpace(EmailSearch))
            {
                queryBuilder.Append(" AND email ILIKE @emailSearch");
                parameters.Add(new NpgsqlParameter("@emailSearch", $"%{EmailSearch}%"));
            }

            string sortColumn = SortColumn switch
            {
                "Id" => "klient_id",
                "Imie" => "imie",
                "Email" => "email",
                "NumerTelefonu" => "numer_telefonu",
                _ => "nazwisko"
            };

            string sortDirection = SortDirection?.ToUpper() == "DESC" ? "DESC" : "ASC";

            queryBuilder.Append($" ORDER BY {sortColumn} {sortDirection}");


            using (var command = connection.CreateCommand())
            {
                command.CommandText = queryBuilder.ToString();
                command.Parameters.AddRange(parameters.ToArray());

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var klient = new Klient
                        {
                            KlientId = reader.GetInt32(0),
                            Imie = reader.GetString(1),
                            Nazwisko = reader.GetString(2),
                            Email = reader.GetString(3),
                            NumerTelefonu = reader.IsDBNull(4) ? null : reader.GetString(4)
                        };
                        Klienci.Add(klient);
                    }
                }
            }

            if (SelectedKlientId.HasValue)
            {
                SelectedKlient = Klienci.FirstOrDefault(k => k.KlientId == SelectedKlientId.Value);
            }

            if (connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }

        public async Task<IActionResult> OnPostExportCsv()
        {
            await OnGetAsync();

            if (!Klienci.Any())
            {
                TempData["ErrorMessage"] = "Brak klientów spe³niaj¹cych kryteria do wyeksportowania.";
                return RedirectToPage(new { SortColumn, SortDirection, IdSearch, ImieSearch, NazwiskoSearch, EmailSearch });
            }

            var builder = new StringBuilder();


            builder.AppendLine("Klient ID;Imiê;Nazwisko;Email;Numer Telefonu");

            foreach (var klient in Klienci)
            {
                builder.AppendLine($"{klient.KlientId};{klient.Imie};{klient.Nazwisko};{klient.Email};{klient.NumerTelefonu}");
            }

            return File(Encoding.UTF8.GetBytes(builder.ToString()),
                        "text/csv",
                        $"Klienci_Eksport_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}