using SAT242516026.Data;

public class Mukellef
{
    public int Id { get; set; }

    public string Ad { get; set; } = null!;        // nvarchar(150) NOT NULL
    public string? VergiNo { get; set; }           // nvarchar(20)
    public string? Tip { get; set; }               // nvarchar(20)
    public string? Telefon { get; set; }           // nvarchar(30)

    public ICollection<Beyanname> Beyannameler { get; set; } = new List<Beyanname>();
}
