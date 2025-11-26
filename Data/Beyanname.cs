using System;
using System.Collections.Generic;

namespace SAT242516026.Data;

public class Beyanname
{
    public int Id { get; set; }

    public int MukellefId { get; set; }
    public int BeyannameTipiId { get; set; }

    public int Yil { get; set; }
    public string? Donem { get; set; }            // nvarchar(20)
    public string? Durum { get; set; }            // nvarchar(50)
    public DateTime? SonGondermeTarihi { get; set; }  // SQL 'date' -> DateTime
    public DateTime? GondermeTarihi { get; set; }     // SQL 'date' -> DateTime

    public Mukellef Mukellef { get; set; } = null!;
    public BeyannameTipi BeyannameTipi { get; set; } = null!;
}

