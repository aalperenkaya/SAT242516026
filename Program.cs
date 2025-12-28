using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using SAT242516026.Components;
using SAT242516026.Logging;
using SAT242516026.Models.DbContexts;
using SAT242516026.Models.MyDbModels;
using SAT242516026.Models.Services;
using SAT242516026.Models.UnitOfWorks;

var builder = WebApplication.CreateBuilder(args);

#region LOGGER
Directory.CreateDirectory("Logs");

var compositeLoggerProvider = new SAT242516026.Logging.CompositeLoggerProvider()
    .AddProvider(new SAT242516026.Logging.AsyncFileLoggerProvider("Logs/app-log.txt"))
    .AddProvider(new SAT242516026.Logging.AsyncDbLoggerProvider(() =>
        new Microsoft.Data.SqlClient.SqlConnection(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
    ));

builder.Logging.ClearProviders();
builder.Logging.AddProvider(compositeLoggerProvider);

builder.Services.AddSingleton(new SAT242516026.Logging.LogService(
    filePath: "Logs/app-log.txt",
    connectionFactory: () => new Microsoft.Data.SqlClient.SqlConnection(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
));
#endregion

#region DB CONTEXT (SADECE CONNECTION İÇİN)
builder.Services.AddDbContext<MyDbModel_Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

#region ✅ MYDBMODEL DI (HOCA PATTERN)
builder.Services.AddScoped(typeof(IMyDbModel<>), typeof(MyDbModel<>));
builder.Services.AddScoped<IMyDbModel_Provider, MyDbModel_Provider>();
builder.Services.AddScoped<IMyDbModel_UnitOfWork, MyDbModel_UnitOfWork<MyDbModel_Context>>();
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

builder.Services.AddAuthorization();
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

app.UseAntiforgery();

#region AUTH ENDPOINTS (SP + JSON) ✅

const string SP_AUTH_REGISTER = "sp_Auth_Register";
const string SP_AUTH_LOGIN = "sp_Auth_Login";

async Task SignInAsync(HttpContext http, AuthUserRow user)
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.KullaniciAdi),
        new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User"),
        new Claim("IsAdmin", user.IsAdmin ? "1" : "0"),
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

    await http.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity),
        new AuthenticationProperties { IsPersistent = true });
}

app.MapPost("/auth/register", async (HttpContext http, IMyDbModel_Provider provider) =>
{
    var form = await http.Request.ReadFormAsync();

    var kullaniciAdi = (form["kullaniciAdi"].ToString() ?? "").Trim();
    var sifre = (form["sifre"].ToString() ?? "");
    var adSoyad = (form["adSoyad"].ToString() ?? "").Trim();
    var email = (form["email"].ToString() ?? "").Trim();

    if (kullaniciAdi.Length < 3) return Results.Redirect("/kayit?err=kullanici_adi_kisa");
    if (string.IsNullOrWhiteSpace(sifre) || sifre.Length < 4) return Results.Redirect("/kayit?err=sifre_kisa");

    var json = JsonSerializer.Serialize(new
    {
        KullaniciAdi = kullaniciAdi,
        Sifre = sifre, // düz şifre (senin isteğin)
        AdSoyad = string.IsNullOrWhiteSpace(adSoyad) ? null : adSoyad,
        Email = string.IsNullOrWhiteSpace(email) ? null : email
    });

    var rows = await provider.SetItems<AuthUserRow>(
        SP_AUTH_REGISTER,
        ("@jsonvalues", json)
    );

    var user = rows.FirstOrDefault();
    if (user is null || user.Id <= 0)
        return Results.Redirect("/kayit?err=kayit_basarisiz");

    await SignInAsync(http, user);
    return Results.Redirect("/");
}).DisableAntiforgery();

app.MapPost("/auth/login", async (HttpContext http, IMyDbModel_Provider provider) =>
{
    var form = await http.Request.ReadFormAsync();

    var kullaniciAdi = (form["kullaniciAdi"].ToString() ?? "").Trim();
    var sifre = (form["sifre"].ToString() ?? "");

    var json = JsonSerializer.Serialize(new
    {
        KullaniciAdi = kullaniciAdi,
        Sifre = sifre
    });

    var rows = await provider.SetItems<AuthUserRow>(
        SP_AUTH_LOGIN,
        ("@jsonvalues", json)
    );

    var user = rows.FirstOrDefault();
    if (user is null || user.Id <= 0)
        return Results.Redirect("/giris?err=bad_login");

    await SignInAsync(http, user);
    return Results.Redirect("/");
}).DisableAntiforgery();

app.MapGet("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/giris");
});

#endregion

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();


// ✅ TOP-LEVEL KURAL: TYPE'lar EN ALTA
public sealed class AuthUserRow
{
    public int Id { get; set; }
    public string KullaniciAdi { get; set; } = "";
    public bool IsAdmin { get; set; }
}
