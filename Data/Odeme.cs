namespace SAT242516026.Data;

public class Odeme
{
    public int Id { get; set; }
    public int TahakkukId { get; set; }
    public Tahakkuk? Tahakkuk { get; set; }

    public decimal Tutar { get; set; }
    public DateTime? OdemeTarihi { get; set; }
}
