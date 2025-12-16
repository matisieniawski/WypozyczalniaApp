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

// --- OSTATECZNE, STABILNE PARSOWANIE URI DLA RENDER ---
var databaseUrl = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(databaseUrl))
{
    string finalConnectionString;

    // Zapewnienie, ¿e prefiks jest poprawny dla klasy Uri, nawet jeœli Render u¿ywa 'postgres://'
    string cleanedUrl = databaseUrl.Replace("postgres://", "postgresql://");

    try
    {
        var uri = new Uri(cleanedUrl);

        // Parsowanie URI i budowanie ci¹gu w formacie s³ownikowym
        finalConnectionString =
            $"Host={uri.Host};" +
            $"Port={(uri.Port > 0 ? uri.Port : 5432)};" + // POPRAWIONE: Dodano nawiasy wokó³ warunku
            $"Database={uri.LocalPath.TrimStart('/')};" +
            $"Username={uri.UserInfo.Split(':')[0]};" +
            $"Password={uri.UserInfo.Split(':')[1]};" +
            $"SSL Mode=Prefer;Trust Server Certificate=true";

        // Zapisujemy skonwertowany ci¹g po³¹czenia
        builder.Services.AddDbContext<WypozyczalniaDbContext>(options =>
            options.UseNpgsql(finalConnectionString));
    }
    catch (Exception ex)
    {
        // Jeœli parsowanie zawiedzie, logujemy b³¹d (choæ w logach i tak bêdzie fail)
        // I u¿ywamy oryginalnej, wadliwej konfiguracji jako fallback.
        Console.WriteLine($"KRYTYCZNY B£¥D PARSOWANIA URI: {ex.Message}");
        builder.Services.AddDbContext<WypozyczalniaDbContext>(options =>
            options.UseNpgsql(databaseUrl));
    }
}
else
{
    // U¿ywamy starego kodu, jeœli zmienna nie jest ustawiona
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