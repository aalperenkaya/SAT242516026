namespace SAT242516026.Data;

public class Beyanname
{
    public int Id { get; set; }

    public int MukellefId { get; set; }
    public Mukellef? Mukellef { get; set; }   // lazım: “Beyanname.Mukellef yok” hatasını bitirir

    public int BeyannameTipiId { get; set; }
    public BeyannameTipi? BeyannameTipi { get; set; }

    public int Yil { get; set; }
    public string? Donem { get; set; }
    public string? Durum { get; set; }

    public DateTime? GondermeTarihi { get; set; }
    public DateTime? SonGondermeTarihi { get; set; }

    public ICollection<Tahakkuk> Tahakkuklar { get; set; } = new List<Tahakkuk>();
}
