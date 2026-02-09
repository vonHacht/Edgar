namespace Edgar.Models
{
    public class CrspDailyRecordBig
    {
        public int PERMNO { get; set; }
        public DateTime date { get; set; }

        public string? TICKER { get; set; }
        public string? COMNAM { get; set; }
        public string? SHRCLS { get; set; }
        public string? TSYMBOL { get; set; }
        public string? TRDSTAT { get; set; }
        public string? SECSTAT { get; set; }

        public int? PERMCO { get; set; }
        public string? CUSIP { get; set; }

        public int? DLSTCD { get; set; }
        public DateTime? NEXTDT { get; set; }

        public string? SHRFLG { get; set; }
        public int? ACPERM { get; set; }
        public int? ACCOMP { get; set; }
        public DateTime? SHRENDDT { get; set; }
        public int? NWPERM { get; set; }

        public double? DLRETX { get; set; }
        public double? DLPRC { get; set; }
        public double? DLRET { get; set; }

        public double? BIDLO { get; set; }
        public double? ASKHI { get; set; }
        public double? PRC { get; set; }
        public double? VOL { get; set; }
        public double? RET { get; set; }

        public double? BID { get; set; }
        public double? ASK { get; set; }
        public double? SHROUT { get; set; }
        public double? OPENPRC { get; set; }
        public int? NUMTRD { get; set; }

        public double? RETX { get; set; }
        public double? vwretd { get; set; }
        public double? vwretx { get; set; }
        public double? ewretd { get; set; }
        public double? ewretx { get; set; }
        public double? sprtrn { get; set; }
    }
}


