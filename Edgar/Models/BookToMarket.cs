namespace Edgar.Models
{
    public class BookToMarket
    {
        public DateTime Date { get; init; }

        public required string Gvkey { get; init; } // GVKEY

        public double CommonEquity { get; set; } // ceq
        public double ShareholdersEquity { get; set; } // seq

        public double PrefferedStockRedemptionValue { get; set; } // pstkrv
        public double PrefferedStockLiquidatingValue { get; set; } // pstkl
        public double PrefferedStock { get; set; } // pstk

        public double DeferredTaxes { get; set; } // txditc

        public double TotalAssets { get; set; } // at
        public double TotalLiabilities { get; set; } // lt

        public double MarketCap { get; set; } // mkvalt
        public double SpecialItems { get; set; } // spi
        public double NetIncome { get; set; } // ni

        // New variables
        public int Sic { get; set; } // sic
        public double LongTermDebt { get; set; } // dltt
        public double PretaxIncome { get; set; } // pi
        public double LoanLossProvision { get; set; } // pll
        public double NetChargeOffs { get; set; } // nco
        public double NonPerformingAssets { get; set; } // npat
        public double Tier1CapitalRatio { get; set; } // capr1
        public DateTime? FinalDate { get; set; } // fdate
        public double LoanLossReservesI { get; set; } // llrci
        public double LoanLossReservesR { get; set; } // llrcr

        public double LossProvision { get; set; }

        public double LossProvisionRaw { get; set; }

        public double Size => MarketCap > 0.0 ? Math.Log(MarketCap) : double.NaN;

        public double BookEquity => _bookEquity();

        public double BM => _bookToMarket(); // Book-to-market ratio

        public double Leverage => _leverage();

        private double _bookEquity()
        {
            static bool HasValue(double v) => !double.IsNaN(v);
            static bool Pos(double v) => !double.IsNaN(v) && v > 0.0;

            double preferred =
                Pos(PrefferedStockRedemptionValue) ? PrefferedStockRedemptionValue :
                Pos(PrefferedStockLiquidatingValue) ? PrefferedStockLiquidatingValue :
                Pos(PrefferedStock) ? PrefferedStock :
                0.0;

            double baseEquity;

            if (Pos(ShareholdersEquity))
            {
                baseEquity = ShareholdersEquity;
            }
            else if (Pos(CommonEquity))
            {
                baseEquity = CommonEquity;
            }
            else if (Pos(TotalAssets) && HasValue(TotalLiabilities))
            {
                baseEquity = TotalAssets - TotalLiabilities;
            }
            else
            {
                baseEquity = 0.0;
            }

            double deferred = Pos(DeferredTaxes) ? DeferredTaxes : 0.0;

            return baseEquity + deferred - preferred;
        }

        private double _bookToMarket()
        {
            if (BookEquity > 0.0 && MarketCap > 0.0)
                return BookEquity / MarketCap;

            return 0.0;
        }

        private double _leverage()
        {
            static bool Pos(double v) => !double.IsNaN(v) && v > 0.0;
            static bool HasValue(double v) => !double.IsNaN(v);

            if (Pos(TotalAssets) && HasValue(TotalLiabilities))
                return TotalLiabilities / TotalAssets;

            return double.NaN;
        }
    }
}

