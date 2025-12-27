using Microsoft.EntityFrameworkCore;

namespace SAT242516026.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
    public DbSet<Mukellef> Mukellefler => Set<Mukellef>();
    public DbSet<Beyanname> Beyannameler => Set<Beyanname>();
    public DbSet<BeyannameTipi> BeyannameTipleri => Set<BeyannameTipi>();
    public DbSet<Tahakkuk> Tahakkuklar => Set<Tahakkuk>();
    public DbSet<Odeme> Odemeler => Set<Odeme>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ✅ Trigger varsa EF INSERT sırasında OUTPUT patlatır → bunu kapat
        modelBuilder.Entity<Kullanici>()
            .ToTable(tb => tb.UseSqlOutputClause(false));

        modelBuilder.Entity<Mukellef>()
            .HasOne(x => x.Kullanici)
            .WithMany(x => x.Mukellefler)
            .HasForeignKey(x => x.KullaniciId);

        modelBuilder.Entity<Beyanname>()
            .HasOne(x => x.Mukellef)
            .WithMany(x => x.Beyannameler)
            .HasForeignKey(x => x.MukellefId);

        modelBuilder.Entity<Beyanname>()
            .HasOne(x => x.BeyannameTipi)
            .WithMany(x => x.Beyannameler)
            .HasForeignKey(x => x.BeyannameTipiId);

        modelBuilder.Entity<Tahakkuk>()
            .HasOne(x => x.Beyanname)
            .WithMany(x => x.Tahakkuklar)
            .HasForeignKey(x => x.BeyannameId);

        modelBuilder.Entity<Odeme>()
            .HasOne(x => x.Tahakkuk)
            .WithMany(x => x.Odemeler)
            .HasForeignKey(x => x.TahakkukId);
    }
}
