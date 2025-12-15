using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WypozyczalniaApp.Data;
using WypozyczalniaApp.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Data;
using System.Data.Common;

namespace WypozyczalniaApp.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly WypozyczalniaDbContext _context;

        [BindProperty]
        public string Login { get; set; } = string.Empty;

        [BindProperty]
        public string Haslo { get; set; } = string.Empty;

        public LoginModel(WypozyczalniaDbContext context)
        {
            _context = context;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(Haslo))
            {
                ModelState.AddModelError("", "Podaj login i has³o.");
                return Page();
            }

            Pracownik? pracownik = null;
            var rolePracownika = new List<string>();

            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var cmdUser = connection.CreateCommand())
            {
                cmdUser.CommandText = "SELECT pracownik_id, login, haslo_hash FROM pracownicy WHERE login = @login";

                var pLogin = cmdUser.CreateParameter();
                pLogin.ParameterName = "@login";
                pLogin.Value = Login;
                cmdUser.Parameters.Add(pLogin);

                using (var reader = await cmdUser.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        pracownik = new Pracownik
                        {
                            PracownikId = reader.GetInt32(0),
                            Login = reader.GetString(1),
                            HasloHash = reader.GetString(2)
                        };
                    }
                }
            }

            if (pracownik == null || pracownik.HasloHash != Haslo)
            {
                if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                ModelState.AddModelError("", "Nieprawid³owy login lub has³o.");
                return Page();
            }

            using (var cmdRoles = connection.CreateCommand())
            {
                cmdRoles.CommandText = @"
                    SELECT r.nazwa_roli 
                    FROM role r
                    JOIN pracownicyrole pr ON r.rola_id = pr.rola_id
                    WHERE pr.pracownik_id = @id";

                var pId = cmdRoles.CreateParameter();
                pId.ParameterName = "@id";
                pId.Value = pracownik.PracownikId;
                cmdRoles.Parameters.Add(pId);

                using (var reader = await cmdRoles.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        rolePracownika.Add(reader.GetString(0));
                    }
                }
            }

            if (connection.State == ConnectionState.Open) await connection.CloseAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, pracownik.Login),
                new Claim(ClaimTypes.NameIdentifier, pracownik.PracownikId.ToString())
            };

            foreach (var rola in rolePracownika)
            {
                claims.Add(new Claim(ClaimTypes.Role, rola));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return LocalRedirect("/Menu");
        }
    }
}