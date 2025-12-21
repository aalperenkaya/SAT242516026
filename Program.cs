using System.Globalization;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using SAT242516026.Components;
using SAT242516026.Logging;
using SAT242516026.Models.DbContexts;
using SAT242516026.Models.Services;

var builder = WebApplication.CreateBuilder(args);

#region LOGGER (FILE + DB)
Directory.CreateDirectory("Logs");

var logFilePath = Path.Combine("Logs", "app-log.txt");

var compositeLoggerProvider = new CompositeLoggerProvider()
    .AddProvider(new AsyncFileLoggerProvider(logFilePath))
    .AddProvider(new AsyncDbLoggerProvider(() =>
        new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

builder.Logging.ClearProviders();
builder.Logging.AddProvider(compositeLoggerProvider);

builder.Services.AddSingleton(new LogService(
    filePath: logFilePath,
    connectionFactory: () => new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))
));
#endregion

#region BLAZOR
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
#endregion

#region LOCALIZATION
builder.Services.AddLocalization(options =>
    options.ResourcesPath = Path.Combine("Models", "Localization"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(typeof(LocalizerService<>));

var supportedCultures = new[] { "tr", "en", "de" };

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("tr");
    options.SupportedCultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();
    options.SupportedUICultures = supportedCultures.Select(c => new CultureInfo(c)).ToList();

    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Insert(1, new CookieRequestCultureProvider());
    options.RequestCultureProviders.Insert(2, new AcceptLanguageHeaderRequestCultureProvider());
});
#endregion

#region DB
builder.Services.AddDbContext<MyDbModel_Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

#region AUTH (COOKIE + CLAIMS)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/giris";
        options.AccessDeniedPath = "/yetkisiz";

        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;

        options.Cookie.Name = "SAT242516026.Auth";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.HttpOnly = true;

        // bazen dev ortamında lazım oluyor
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();

// Register sayfası bunu kullanıyor
builder.Services.AddScoped<AuthService>();
#endregion

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(locOptions.Value);

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ✅ Blazor formlarında lazım olacak (NavMenu logout POST gibi)
app.UseAntiforgery();

#region AUTH ENDPOINTS (LOGIN/LOGOUT)

// ✅ LOGIN (Form POST) - antiforgery KAPALI, düz şifre kontrol
app.MapPost("/auth/login", async (HttpContext http, MyDbModel_Context db) =>
{
    try
    {
        // Form alanlarını buradan çekiyoruz (binder triplerini azaltır)
        var form = await http.Request.ReadFormAsync();

        var kullaniciAdi = (form["kullaniciAdi"].ToString() ?? "").Trim();
        var sifre = (form["sifre"].ToString() ?? "");

        if (string.IsNullOrWhiteSpace(kullaniciAdi))
            return Results.Redirect("/giris?err=kullanici_adi_bos");

        var user = await db.Kullanicilar.FirstOrDefaultAsync(x => x.KullaniciAdi == kullaniciAdi);
        if (user is null)
            return Results.Redirect("/giris?err=user_not_found");

        // ✅ BCrypt yok: düz şifre karşılaştırma
        if ((user.SifreHash ?? "") != (sifre ?? ""))
            return Results.Redirect("/giris?err=bad_password");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.KullaniciAdi),
            new Claim("IsAdmin", user.IsAdmin ? "1" : "0"),
        };

        if (user.IsAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });

        return Results.Redirect("/");
    }
    catch (Exception ex)
    {
        return Results.Redirect("/giris?err=server_error_" + Uri.EscapeDataString(ex.Message));
    }
})
.DisableAntiforgery(); // ✅ login formunda token aramasın

// ✅ LOGOUT (POST) - NavMenu bunu kullanacak (güvenli)
app.MapPost("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/giris");
});
// Not: Buna DisableAntiforgery eklemiyorum çünkü NavMenu formunda <AntiforgeryToken /> var.
// Yani güvenli şekilde token ile çalışacak.

// ✅ LOGOUT (GET) - elinde link varsa diye (opsiyonel ama pratik)
app.MapGet("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/giris");
});

#endregion

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
