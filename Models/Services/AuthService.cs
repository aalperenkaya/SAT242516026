using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SAT242516026.Models.DbContexts;

namespace SAT242516026.Models.Services;

public class AuthService
{
    private readonly MyDbModel_Context _db;
    private readonly IHttpContextAccessor _http;

    public AuthService(MyDbModel_Context db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task<(bool ok, string? error)> RegisterAsync(string kullaniciAdi, string sifre, string? adSoyad, string? email)
    {
        kullaniciAdi = (kullaniciAdi ?? "").Trim();

        if (kullaniciAdi.Length < 3) return (false, "Kullanıcı adı çok kısa.");
        if (string.IsNullOrWhiteSpace(sifre) || sifre.Length < 4) return (false, "Şifre çok kısa.");

        var exists = await _db.Kullanicilar.AnyAsync(x => x.KullaniciAdi == kullaniciAdi);
        if (exists) return (false, "Bu kullanıcı adı alınmış.");

        var user = new Kullanici
        {
            KullaniciAdi = kullaniciAdi,
            SifreHash = sifre, // düz şifre
            AdSoyad = adSoyad,
            Email = email,
            IsAdmin = false
        };

        _db.Kullanicilar.Add(user);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool ok, string? error)> LoginAsync(string kullaniciAdi, string sifre)
    {
        kullaniciAdi = (kullaniciAdi ?? "").Trim();

        var user = await _db.Kullanicilar.FirstOrDefaultAsync(x => x.KullaniciAdi == kullaniciAdi);
        if (user is null) return (false, "Kullanıcı bulunamadı.");

        if ((user.SifreHash ?? "") != (sifre ?? ""))
            return (false, "Şifre yanlış.");

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

        await _http.HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });

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
}
