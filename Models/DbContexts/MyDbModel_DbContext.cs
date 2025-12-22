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

            // =========================
            // KULLANICI
            // (Kullanicilar tablosunda trigger yoksa HasTrigger yazmana gerek yok)
            // =========================
            modelBuilder.Entity<Kullanici>(e =>
            {
                e.ToTable("Kullanicilar", "dbo");

                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

                e.Property(x => x.KullaniciAdi)
                    .IsRequired()
                    .HasMaxLength(50)
                    .IsUnicode(true);

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
            });

            // =========================
            // MUKELLEF ✅ trigger var
            // =========================
            modelBuilder.Entity<Mukellef>(e =>
            {
                e.ToTable("Mukellef", "dbo");
                e.ToTable(tb => tb.HasTrigger("Trg_Mukellef_IUD"));

                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

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

                e.Property(x => x.KullaniciId).IsRequired();
            });

            // =========================
            // BEYANNAME TIPI ✅ trigger var
            // =========================
            modelBuilder.Entity<BeyannameTipi>(e =>
            {
                e.ToTable("BeyannameTipi", "dbo");
                e.ToTable(tb => tb.HasTrigger("Trg_BeyannameTipi_IUD"));

                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

                e.Property(x => x.Ad)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(true);
            });

            // =========================
            // BEYANNAME ✅ trigger var
            // =========================
            modelBuilder.Entity<Beyanname>(e =>
            {
                e.ToTable("Beyanname", "dbo");
                e.ToTable(tb => tb.HasTrigger("Trg_Beyanname_IUD"));

                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();

                e.Property(x => x.MukellefId).IsRequired();
                e.Property(x => x.BeyannameTipiId).IsRequired();
                e.Property(x => x.Yil).IsRequired();

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

            // =========================
            // TAHHAKKUK ✅ trigger var
            // =========================
            modelBuilder.Entity<Tahakkuk>(e =>
            {
                e.ToTable("Tahakkuk", "dbo");
                e.ToTable(tb => tb.HasTrigger("Trg_Tahakkuk_IUD"));

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

            // =========================
            // ODEME ✅ 2 trigger var
            // =========================
            modelBuilder.Entity<Odeme>(e =>
            {
                e.ToTable("Odeme", "dbo");

                // ✅ Odeme tablosunda 2 trigger var, ikisini de aynı ToTable içinde tanıt
                e.ToTable(tb =>
                {
                    tb.HasTrigger("TR_Odeme_UpdateBeyannameDurum");
                    tb.HasTrigger("Trg_Odeme_IUD");
                });

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
