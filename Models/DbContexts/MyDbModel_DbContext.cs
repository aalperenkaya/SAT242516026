using Microsoft.EntityFrameworkCore;
using SAT242516026.Data;

namespace SAT242516026.Models.DbContexts
{
    public class MyDbModel_Context : DbContext
    {
        public MyDbModel_Context(DbContextOptions<MyDbModel_Context> options)
            : base(options)
        {
        }

        // AuthService burayı kullanıyor
        public DbSet<Kullanici> Kullanicilar { get; set; } = null!;

        public DbSet<Mukellef> Mukellef { get; set; } = null!;
        public DbSet<BeyannameTipi> BeyannameTipi { get; set; } = null!;
        public DbSet<Beyanname> Beyanname { get; set; } = null!;
        public DbSet<Tahakkuk> Tahakkuk { get; set; } = null!;
        public DbSet<Odeme> Odeme { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // dbo.Kullanicilar tablosu mapping
            modelBuilder.Entity<Kullanici>(e =>
            {
                e.ToTable("Kullanicilar", "dbo");

                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

                e.Property(x => x.KullaniciAdi)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(true);

                // BCrypt kaldırdıysan bu alan artık düz şifre tutuyor ama kolon adı aynı kalabilir
                e.Property(x => x.SifreHash)
                    .IsRequired()
                    .HasMaxLength(200)
                    .IsUnicode(true);

                e.Property(x => x.AdSoyad)
                    .HasMaxLength(100)
                    .IsUnicode(true);

                e.Property(x => x.Email)
                    .HasMaxLength(120)
                    .IsUnicode(true);

                e.Property(x => x.IsAdmin)
                    .HasDefaultValue(false);

                // Eğer Kullanici entity'nde CreatedAt yoksa bu satırı SİL
                // e.Property(x => x.CreatedAt);
            });

            modelBuilder.Entity<Mukellef>(e =>
            {
                e.ToTable("Mukellef", "dbo");

                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

                // sp_help: Ad nvarchar(300), VergiNo nvarchar(40), Tip nvarchar(40), Telefon nvarchar(60)
                e.Property(x => x.Ad)
                    .IsRequired()
                    .HasMaxLength(300)
                    .IsUnicode(true);

                e.Property(x => x.VergiNo)
                    .HasMaxLength(40)
                    .IsUnicode(true);

                e.Property(x => x.Tip)
                    .HasMaxLength(40)
                    .IsUnicode(true);

                e.Property(x => x.Telefon)
                    .HasMaxLength(60)
                    .IsUnicode(true);

                // ✅ SQL'de eklediğimiz kolon (entity'de de olmalı)
                e.Property(x => x.KullaniciId).IsRequired();
            });

            modelBuilder.Entity<BeyannameTipi>(e =>
            {
                e.ToTable("BeyannameTipi", "dbo");

                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

                e.Property(x => x.Ad)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(true);
            });

            modelBuilder.Entity<Beyanname>(e =>
            {
                e.ToTable("Beyanname", "dbo");

                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

                e.Property(x => x.MukellefId).IsRequired();
                e.Property(x => x.BeyannameTipiId).IsRequired();
                e.Property(x => x.Yil).IsRequired();

                // sp_help: Donem nvarchar(40), Durum nvarchar(100)
                e.Property(x => x.Donem)
                    .HasMaxLength(40)
                    .IsUnicode(true);

                e.Property(x => x.Durum)
                    .HasMaxLength(100)
                    .IsUnicode(true);

                e.Property(x => x.SonGondermeTarihi).HasColumnType("date");
                e.Property(x => x.GondermeTarihi).HasColumnType("date");

                e.HasOne(x => x.Mukellef)
                    .WithMany(m => m.Beyannameler)
                    .HasForeignKey(x => x.MukellefId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.BeyannameTipi)
                    .WithMany(t => t.Beyannameler)
                    .HasForeignKey(x => x.BeyannameTipiId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Tahakkuk>(e =>
            {
                e.ToTable("Tahakkuk", "dbo");

                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

                e.Property(x => x.BeyannameId).IsRequired();
                e.Property(x => x.Tutar).HasPrecision(18, 2).HasDefaultValue(0m);
                e.Property(x => x.Tarih).HasColumnType("date");

                e.HasOne(x => x.Beyanname)
                    .WithMany()
                    .HasForeignKey(x => x.BeyannameId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Odeme>(e =>
            {
                e.ToTable("Odeme", "dbo");
                e.ToTable(tb => tb.HasTrigger("TR_Odeme_UpdateBeyannameDurum"));

                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

                e.Property(x => x.TahakkukId).IsRequired();
                e.Property(x => x.Tutar).HasPrecision(18, 2).HasDefaultValue(0m);
                e.Property(x => x.OdemeTarihi).HasColumnType("date");

                e.HasOne(x => x.Tahakkuk)
                    .WithMany(t => t.Odemeler)
                    .HasForeignKey(x => x.TahakkukId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
