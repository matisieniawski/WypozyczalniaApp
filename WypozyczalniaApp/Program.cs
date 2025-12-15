using WypozyczalniaApp.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(int.Parse(port));
    });
}

// --- KLUCZOWA ZMIANA: Konwersja formatu URL z Render na format oczekiwany przez Npgsql ---
var databaseUrl = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

// Sprawdzamy, czy string po³¹czenia u¿ywa formatu URL (postgres://)
if (!string.IsNullOrEmpty(databaseUrl) && databaseUrl.StartsWith("postgres://"))
{
    // KLUCZOWA POPRAWKA: Render u¿ywa 'postgres://', a standardowo powinno byæ 'postgresql://'.
    // Zamieniamy 'postgres://' na 'Host=', aby Npgsql móg³ poprawnie sparsowaæ resztê ci¹gu jako URI.
    databaseUrl = databaseUrl.Replace("postgres://", "Host=");

    // Tworzymy NpgsqlConnectionStringBuilder bezpoœrednio z ci¹gu URL (co jest mo¿liwe, jeœli poprawiliœmy prefix)
    var connBuilder = new NpgsqlConnectionStringBuilder(databaseUrl);

    // Dodajemy wymagane ustawienia SSL dla Render.com
    connBuilder.SslMode = SslMode.Prefer;
    connBuilder.TrustServerCertificate = true;

    // Zapisujemy nowy, ju¿ skonwertowany ci¹g po³¹czenia do konfiguracji EF Core
    builder.Services.AddDbContext<WypozyczalniaDbContext>(options =>
        options.UseNpgsql(connBuilder.ToString()));
}
else
{
    // U¿ywamy starego kodu, jeœli zmienna nie jest ustawiona lub jest w tradycyjnym formacie
    builder.Services.AddDbContext<WypozyczalniaDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
    );
}
// -----------------------------------------------------------------------------------------


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireManagerRole",
        policy => policy.RequireRole("Manager", "Administrator"));

    options.AddPolicy("RequireAdminRole",
        policy => policy.RequireRole("Administrator"));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToFolder("/Account");
});

builder.Services.AddControllers();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{

}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapControllers();
app.Run();