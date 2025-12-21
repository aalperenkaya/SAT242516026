using System.ComponentModel.DataAnnotations;

namespace SAT242516026.Models.DbContexts;

public class Kullanici
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string KullaniciAdi { get; set; } = null!;

    [Required, MaxLength(200)]
    public string SifreHash { get; set; } = null!;

    [MaxLength(100)]
    public string? AdSoyad { get; set; }

    [MaxLength(120)]
    public string? Email { get; set; }

    public bool IsAdmin { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;



}
