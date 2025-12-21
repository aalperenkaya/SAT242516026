using SAT242516026.Data;

public class Mukellef
{
    public int Id { get; set; }

    public string Ad { get; set; } = null!;        
    public string? VergiNo { get; set; }        
    public string? Tip { get; set; }               
    public string? Telefon { get; set; }
    public int KullaniciId { get; set; }



    public ICollection<Beyanname> Beyannameler { get; set; } = new List<Beyanname>();
}
