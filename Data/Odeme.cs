using System;

namespace SAT242516026.Data
{
    public class Odeme
    {
        public int Id { get; set; }

        public int TahakkukId { get; set; }

        public decimal Tutar { get; set; }   // decimal(18,2) NOT NULL, default 0

        public DateTime? OdemeTarihi { get; set; }   // date NULL

        public Tahakkuk Tahakkuk { get; set; } = null!;
    }
}
