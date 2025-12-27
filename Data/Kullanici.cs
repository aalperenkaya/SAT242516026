using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SAT242516026.Data
{
    [Table("Kullanicilar")] // tablo adın buysa doğru. Değilse burayı DB'deki gerçek tablo adına çevir.
    public class Kullanici
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string KullaniciAdi { get; set; } = null!;

        [Required, MaxLength(100)]
        public string AdSoyad { get; set; } = null!;

        [MaxLength(120)]
        public string? Email { get; set; }

        // DB kolonu: SifreHash
        [Required, MaxLength(50)]
        [Column("SifreHash")]
        public string SifreHash { get; set; } = "";

        public bool IsAdmin { get; set; } = false;

        public ICollection<Mukellef> Mukellefler { get; set; } = new List<Mukellef>();
        public ICollection<Beyanname> Beyannameler { get; set; } = new List<Beyanname>();
    }
}
