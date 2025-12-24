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

// (hocadan gelen attribute/extension/enums varsa kalsın, zararı yok)
using SAT242516026.Models.Attributes;
using SAT242516026.Models.Extensions;
using SAT242516026.Models.Enums;

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

builder.Services.AddLogging();

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
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Admin rolü zaten ClaimTypes.Role ile veriliyor
builder.Services.AddAuthorization();

// Register sayfası
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

// Form POST logout için lazım
app.UseAntiforgery();

#region AUTH ENDPOINTS

// REGISTER
app.MapPost("/auth/register", async (HttpContext http, MyDbModel_Context db) =>
{
    try
    {
        var form = await http.Request.ReadFormAsync();

        var kullaniciAdi = (form["kullaniciAdi"].ToString() ?? "").Trim();
        var sifre = (form["sifre"].ToString() ?? "");
        var adSoyad = (form["adSoyad"].ToString() ?? "").Trim();
        var email = (form["email"].ToString() ?? "").Trim();

        if (kullaniciAdi.Length < 3)
            return Results.Redirect("/kayit?err=kullanici_adi_kisa");

        if (string.IsNullOrWhiteSpace(sifre) || sifre.Length < 4)
            return Results.Redirect("/kayit?err=sifre_kisa");

        var exists = await db.Kullanicilar.AnyAsync(x => x.KullaniciAdi == kullaniciAdi);
        if (exists)
            return Results.Redirect("/kayit?err=kullanici_adi_alinmis");

        var user = new Kullanici
        {
            KullaniciAdi = kullaniciAdi,
            SifreHash = sifre,
            AdSoyad = string.IsNullOrWhiteSpace(adSoyad) ? null : adSoyad,
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
            IsAdmin = false
        };

        db.Kullanicilar.Add(user);
        await db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.KullaniciAdi),
            new Claim("IsAdmin", "0"),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        return Results.Redirect("/user/beyannamelerim");
    }
    catch (Exception ex)
    {
        return Results.Redirect("/kayit?err=" + Uri.EscapeDataString(ex.Message));
    }
})
.DisableAntiforgery();

// LOGIN
app.MapPost("/auth/login", async (HttpContext http, MyDbModel_Context db) =>
{
    try
    {
        var form = await http.Request.ReadFormAsync();

        var kullaniciAdi = (form["kullaniciAdi"].ToString() ?? "").Trim();
        var sifre = (form["sifre"].ToString() ?? "");

        var user = await db.Kullanicilar.FirstOrDefaultAsync(x => x.KullaniciAdi == kullaniciAdi);
        if (user == null || user.SifreHash != sifre)
            return Results.Redirect("/giris?err=bad_login");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.KullaniciAdi),
            new Claim("IsAdmin", user.IsAdmin ? "1" : "0"),
        };

        if (user.IsAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });

        return Results.Redirect("/");
    }
    catch (Exception ex)
    {
        return Results.Redirect("/giris?err=" + Uri.EscapeDataString(ex.Message));
    }
})
.DisableAntiforgery();

// LOGOUT (POST) ✅ form kullanırsan burası
app.MapPost("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/giris");
});

// LOGOUT (GET) ✅ link kullanırsan burası
app.MapGet("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/giris");
});

#endregion

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();





app.Run();
