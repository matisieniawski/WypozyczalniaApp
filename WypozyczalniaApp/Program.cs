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

// --- KLUCZOWA ZMIANA: Stabilne Parsowanie Internal Database URL (URI) z Render ---
var databaseUrl = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

if (!string.IsNullOrEmpty(databaseUrl))
{
    string finalConnectionString;

    // 1. Sprawdzamy, czy mamy do czynienia z formatem URI (postgres://)
    if (databaseUrl.StartsWith("postgres://"))
    {
        // KLUCZOWA POPRAWKA: Stabilne parsowanie URI i budowanie ci¹gu s³ownikowego
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');

        finalConnectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.PathAndQuery.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Prefer;Trust Server Certificate=true";
    }
    else
    {
        // Jest to ju¿ tradycyjny format s³ownikowy lub lokalny
        finalConnectionString = databaseUrl;
    }

    // Zapisujemy skonwertowany ci¹g po³¹czenia
    builder.Services.AddDbContext<WypozyczalniaDbContext>(options =>
        options.UseNpgsql(finalConnectionString));
}
else
{
    // U¿ywamy starego kodu, jeœli zmienna nie jest ustawiona (tylko w celach awaryjnych)
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