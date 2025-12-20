using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;

using SAT242516026.Components;
using SAT242516026.Models.DbContexts;
using SAT242516026.Models.Services;
using SAT242516026.Logging;

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

// Logs ekranı için
builder.Services.AddSingleton(new LogService(
    filePath: logFilePath,
    connectionFactory: () => new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))
));
#endregion

#region BLAZOR
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
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
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
