using System.Collections.Generic;

namespace SAT242516026.Data;

public class BeyannameTipi
{
    public int Id { get; set; }

    public string Ad { get; set; } = null!;

    // İlişki: Bir tip birçok beyannameye bağlanabilir
    public ICollection<Beyanname> Beyannameler { get; set; } = new List<Beyanname>();
}
