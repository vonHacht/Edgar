namespace Edgar.Models
{
    public class CrspDailyRecord
    {
        public int Permno { get; set; }
        public DateTime Date { get; set; }

        public string? Ticker { get; set; }
        public string? Comnam { get; set; }

        /// <summary>8-digit CUSIP in CRSP daily files (often called CUSIP or NCUSIP).</summary>
        public string? Cusip8 { get; set; }

        public double? Prc { get; set; }       // PRC
        public double? Ret { get; set; }       // RET
        public double? Vol { get; set; }       // VOL
        public double? Shrout { get; set; }    // SHROUT (in thousands)
    }
}
