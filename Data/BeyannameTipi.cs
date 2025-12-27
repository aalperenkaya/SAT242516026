namespace SAT242516026.Data;

public class BeyannameTipi
{
    public int Id { get; set; }
    public string Ad { get; set; } = null!;

    public ICollection<Beyanname> Beyannameler { get; set; } = new List<Beyanname>();
}
