namespace Edgar.Models
{
    public class Firm
    {
        public string? CIK { get; set; }

        public string? LatestCompanyName { get; set; }
        public bool NameChanged { get; set; }

        public string? Ticker { get; set; }

        public string? AllTickers { get; set; }

        public int NameVariants { get; set; }
        public string? AllNames { get; set; }

        public string? Gvkey { get; set; }

        public List<CrspDailyRecordBig> CRSPBig { get; set; } = new List<CrspDailyRecordBig>();
    
        public List<CrspDailyRecordSmall> CRSPSmall { get; set; } = new List<CrspDailyRecordSmall>();
    }
}
