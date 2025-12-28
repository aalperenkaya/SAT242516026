using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SAT242516026.Models.MyDbModels;

namespace SAT242516026.Models.Services;

public class AuthService
{
    private readonly IMyDbModel_Provider _provider;
    private readonly IHttpContextAccessor _http;

    private const string SP_AUTH_REGISTER = "sp_Auth_Register";
    private const string SP_AUTH_LOGIN = "sp_Auth_Login";

    public AuthService(IMyDbModel_Provider provider, IHttpContextAccessor http)
    {
        _provider = provider;
        _http = http;
    }

    // SP dönüş modeli
    public class AuthUserRow
    {
        public int Id { get; set; }
        public string KullaniciAdi { get; set; } = "";
        public bool IsAdmin { get; set; }
    }

    public async Task<(bool ok, string? error)> RegisterAsync(string kullaniciAdi, string sifre, string? adSoyad, string? email)
    {
        kullaniciAdi = (kullaniciAdi ?? "").Trim();

        if (kullaniciAdi.Length < 3) return (false, "Kullanıcı adı çok kısa.");
        if (string.IsNullOrWhiteSpace(sifre) || sifre.Length < 4) return (false, "Şifre çok kısa.");

        var json = JsonSerializer.Serialize(new
        {
            KullaniciAdi = kullaniciAdi,
            Sifre = sifre,
            AdSoyad = string.IsNullOrWhiteSpace(adSoyad) ? null : adSoyad?.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email?.Trim()
        });

        var rows = await _provider.SetItems<AuthUserRow>(
            SP_AUTH_REGISTER,
            ("@jsonvalues", json)
        );

        var user = rows.FirstOrDefault();
        if (user is null || user.Id <= 0) return (false, "Kayıt başarısız (kullanıcı adı alınmış olabilir).");

        // İstersen kayıt sonrası otomatik login:
        await SignInAsync(user);

        return (true, null);
    }

    public async Task<(bool ok, string? error)> LoginAsync(string kullaniciAdi, string sifre)
    {
        kullaniciAdi = (kullaniciAdi ?? "").Trim();

        if (kullaniciAdi.Length < 3) return (false, "Kullanıcı adı hatalı.");
        if (string.IsNullOrWhiteSpace(sifre)) return (false, "Şifre boş.");

        var json = JsonSerializer.Serialize(new
        {
            KullaniciAdi = kullaniciAdi,
            Sifre = sifre
        });

        var rows = await _provider.SetItems<AuthUserRow>(
            SP_AUTH_LOGIN,
            ("@jsonvalues", json)
        );

        var user = rows.FirstOrDefault();
        if (user is null || user.Id <= 0) return (false, "Kullanıcı adı/şifre yanlış.");

        await SignInAsync(user);
        return (true, null);
    }

    public async Task LogoutAsync()
        => await _http.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    public int? GetUserId()
    {
        var id = _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var parsed) ? parsed : null;
    }

    public bool IsAdmin()
        => _http.HttpContext?.User?.IsInRole("Admin") == true;

    private async Task SignInAsync(AuthUserRow user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.KullaniciAdi),
            new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User"),
            new Claim("IsAdmin", user.IsAdmin ? "1" : "0"),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await _http.HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }
}
