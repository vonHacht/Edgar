namespace Edgar.Models
{
    public class CcmCssRecord 
    {
        public string GVKEY { get; set; } = default!;
        public string LIID { get; set; } = default!;

        public int? LPERMNO { get; set; }
        public int? LPERMCO { get; set; }

        public DateTime? LINKDT { get; set; }

        // NOTE: Can be "E" (open-ended) or a date like "2018-12-31"
        public string? LINKENDDT { get; set; }

        public string? iid { get; set; }
        public DateTime? datadate { get; set; }

        public string? tic { get; set; }
        public string? cusip { get; set; }
        public string? conm { get; set; }
        public string? navm { get; set; }

        public double? prccm { get; set; }
        public double? prchm { get; set; }
        public double? prclm { get; set; }

        public double? trfm { get; set; }
        public double? trt1m { get; set; }

        public double? rawpm { get; set; }
        public double? rawxm { get; set; }

        public string? sph100 { get; set; }
        public string? sphcusip { get; set; }
        public string? sphname { get; set; }
        public string? sphtic { get; set; }

        public double? cshoq { get; set; }
        public double? adrrm { get; set; }

        public int? cmth { get; set; }
        public double? cshom { get; set; }

        public int? cyear { get; set; }
        public string? mkvalincl { get; set; }

        public int? exchg { get; set; }
        public string? secstat { get; set; }

        // Leading zeros possible
        public string? cik { get; set; }

        public string? fic { get; set; }
        public string? conml { get; set; }
        public string? costat { get; set; }
        public string? county { get; set; }

        public string? dlrsn { get; set; }
        public string? ein { get; set; }

        public int? fyrc { get; set; }

        public int? ggroup { get; set; }
        public int? gind { get; set; }
        public int? gsector { get; set; }
        public int? gsubind { get; set; }

        public string? idbflag { get; set; }
        public string? phone { get; set; }

        public DateTime? dldte { get; set; }
        public DateTime? ipodate { get; set; }

        public int? curr_sp500_flag { get; set; }
    }
}

