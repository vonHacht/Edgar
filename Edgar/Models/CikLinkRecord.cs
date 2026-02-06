namespace Edgar.Models
{
    public class CikLinkRecord
    {
        public string Cik10 { get; set; } = string.Empty;
        public string? Gvkey { get; set; }
        public int? Permno { get; set; }

        public DateTime? LinkStart { get; set; }
        public DateTime? LinkEnd { get; set; }

        public string? Cusip8 { get; set; }
        public string? Cusip9 { get; set; }
    }
}
