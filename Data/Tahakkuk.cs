using System;
using System.Collections.Generic;

namespace SAT242516026.Data
{
    public class Tahakkuk
    {
        public int Id { get; set; }

        public int BeyannameId { get; set; }

        public decimal Tutar { get; set; }  // decimal(18,2) NOT NULL, default 0

        public DateTime? Tarih { get; set; } // date NULL

        public Beyanname Beyanname { get; set; } = null!;

        public ICollection<Odeme> Odemeler { get; set; } = new List<Odeme>();
    }
}
