using System.Globalization;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using SAT242516026.Components;
using SAT242516026.Data;
using SAT242516026.Logging;
using SAT242516026.Models.DbContexts;
using SAT242516026.Models.Extensions;
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

#region DB CONTEXTS
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<MyDbModel_Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

#region ✅ MYDBMODEL DI
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

#region MIGRATION
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyDbModel_Context>();
    db.Database.Migrate();
}
#endregion

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

#region AUTH ENDPOINTS

app.MapPost("/auth/register", async (HttpContext http, MyDbModel_Context db) =>
{
    var form = await http.Request.ReadFormAsync();

    var kullaniciAdi = (form["kullaniciAdi"].ToString() ?? "").Trim();
    var sifre = (form["sifre"].ToString() ?? "");
    var adSoyad = (form["adSoyad"].ToString() ?? "").Trim();
    var email = (form["email"].ToString() ?? "").Trim();

    if (kullaniciAdi.Length < 3) return Results.Redirect("/kayit?err=kullanici_adi_kisa");
    if (string.IsNullOrWhiteSpace(sifre) || sifre.Length < 4) return Results.Redirect("/kayit?err=sifre_kisa");

    var exists = await db.Kullanicilar.AnyAsync(x => x.KullaniciAdi == kullaniciAdi);
    if (exists) return Results.Redirect("/kayit?err=kullanici_adi_alinmis");

    var user = new Kullanici
    {
        KullaniciAdi = kullaniciAdi,
        SifreHash = sifre, // düz şifre (senin isteğin)
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
        new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User"),
        new Claim("IsAdmin", user.IsAdmin ? "1" : "0"),
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

    await http.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity),
        new AuthenticationProperties { IsPersistent = true });

    return Results.Redirect("/");
}).DisableAntiforgery();

app.MapPost("/auth/login", async (HttpContext http, MyDbModel_Context db) =>
{
    var form = await http.Request.ReadFormAsync();

    var kullaniciAdi = (form["kullaniciAdi"].ToString() ?? "").Trim();
    var sifre = (form["sifre"].ToString() ?? "");

    var user = await db.Kullanicilar.FirstOrDefaultAsync(x => x.KullaniciAdi == kullaniciAdi);
    if (user is null || user.SifreHash != sifre)
        return Results.Redirect("/giris?err=bad_login");

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
