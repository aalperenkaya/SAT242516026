namespace SAT242516026.Data;

public class Tahakkuk
{
    public int Id { get; set; }
    public int BeyannameId { get; set; }
    public Beyanname? Beyanname { get; set; }

    public decimal Tutar { get; set; }
    public DateTime? Tarih { get; set; }

    public ICollection<Odeme> Odemeler { get; set; } = new List<Odeme>();
}

