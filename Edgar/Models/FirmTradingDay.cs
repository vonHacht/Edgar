namespace Edgar.Models
{
    public sealed class FirmTradingDay
    {
        // Keys
        public DateTime Date { get; init; }

        // Prices (CRSP: PRC can be negative = midpoint; store raw + normalized)
        public decimal? ClosePrcRaw { get; init; }     // CRSP PRC as-is
        public decimal? Close { get; init; }           // abs(PRC)
        public bool CloseIsMidpoint { get; init; }     // PRC < 0

        public decimal? Open { get; init; }
        public decimal? Bid { get; init; }
        public decimal? Ask { get; init; }
        public decimal? BidLow { get; init; }
        public decimal? AskHigh { get; init; }

        // Activity / size
        public long? Volume { get; init; }
        public int? NumberOfTrades { get; init; }
        public long? SharesOut { get; init; } // usually in shares (check units in your extract)

        // Returns
        public double? Ret { get; init; } // RET
        public double? RetExDiv { get; init; } // RETX

        // Delisting info (if applicable)
        public int? DelistCode { get; init; }          // DLSTCD
        public double? DelistRet { get; init; }        // DLRET
        public double? DelistRetExDiv { get; init; }   // DLRETX
        public double? DelistPrice { get; init; }     // DLPRC
    }

}
