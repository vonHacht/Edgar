namespace Edgar.Models
{
    public class CcmCssRecord
    {
        // ---- CCM link fields ----
        public string GVKEY { get; set; } = default!;
        public string? LINKPRIM { get; set; }
        public string LIID { get; set; } = default!;
        public string? LINKTYPE { get; set; }

        public int? LPERMNO { get; set; }
        public int? LPERMCO { get; set; }

        public DateTime? LINKDT { get; set; }

        // Can be "E" (open-ended) or a date like "2018-12-31"
        public string? LINKENDDT { get; set; }

        // ---- Security / company identifiers ----
        public string? iid { get; set; }
        public DateTime? datadate { get; set; }

        public string? tic { get; set; }

        // Leading zeros possible
        public string? cusip { get; set; }

        public string? conm { get; set; }

        public string? curcddv { get; set; }
        public string? curcdd { get; set; }

        public string? cik { get; set; }   // keep string (leading zeros)
        public string? fic { get; set; }
        public string? conml { get; set; }
        public string? costat { get; set; }
        public string? dlrsn { get; set; }
        public string? ein { get; set; }   // keep string (often leading zeros)

        public int? fyrc { get; set; }
        public string? idbflag { get; set; }
        public string? incorp { get; set; }
        public string? loc { get; set; }

        public int? naics { get; set; }
        public int? sic { get; set; }

        public string? prican { get; set; }
        public string? prirow { get; set; }
        public string? priusa { get; set; }

        public string? stko { get; set; }

        public DateTime? dldte { get; set; }
        public DateTime? ipodate { get; set; }

        // ---- Prices / trading ----
        public double? prccd { get; set; }  // close
        public double? prcod { get; set; }  // open
        public double? prchd { get; set; }  // high
        public double? prcld { get; set; }  // low

        public double? ajexdi { get; set; }
        public double? trfd { get; set; }

        public double? cshoc { get; set; }   // shares outstanding
        public double? cshtrd { get; set; }  // shares traded (volume)

        public double? dvi { get; set; }
        public double? eps { get; set; }
        public double? epsmo { get; set; }

        public double? prcstd { get; set; }

        public int? exchg { get; set; }
        public string? secstat { get; set; }
        public string? tpci { get; set; }

        // ---- Corporate actions / distributions ----
        public double? capgn { get; set; }
        public double? cheqv { get; set; }

        public double? div { get; set; }
        public double? divd { get; set; }
        public string? divdpaydateind { get; set; }

        public double? divsp { get; set; }
        public string? dvrated { get; set; }
        public string? paydateind { get; set; }

        public DateTime? anncdate { get; set; }
        public DateTime? capgnpaydate { get; set; }
        public DateTime? cheqvpaydate { get; set; }
        public DateTime? divdpaydate { get; set; }
        public DateTime? divsppaydate { get; set; }

        public DateTime? paydate { get; set; }
        public DateTime? recorddate { get; set; }
    }
}

