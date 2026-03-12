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

        public double LossProvision => _lossProvision();

        public double Size => Math.Log(MarketCap);

        public double BookEquity { get { return _bookEquity(); } }

        public double BM { get { return _bookToMarket(); } } // Book-to-market ratio

        public double Leverage => _leverage();

        private double _bookEquity()
        {
            // static bool IsMissing(double v) => double.IsNaN(v);
            static bool HasValue(double v) => !double.IsNaN(v);
            static bool Pos(double v) => !double.IsNaN(v) && v > 0.0;

            // Preferred stock priority: pstkrv -> pstkl -> pstk
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
                // Use CEQ as-is; preferred is handled consistently below
                baseEquity = CommonEquity;
            }
            else if (Pos(TotalAssets) && HasValue(TotalLiabilities))
            {
                baseEquity = TotalAssets - TotalLiabilities; // lt may be 0
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
            {
                return BookEquity / MarketCap;
            }
            else
            {
                return 0.0; // or NaN, depending on how you want to handle this case
            }
        }

        private double _leverage()
        {
            static bool Pos(double v) => !double.IsNaN(v) && v > 0.0;
            static bool HasValue(double v) => !double.IsNaN(v);

            if (Pos(TotalAssets) && HasValue(TotalLiabilities))
            {
                return TotalLiabilities / TotalAssets;
            }

            return double.NaN;
        }

        private double _lossProvision()
        {
            static bool Pos(double v) => !double.IsNaN(v) && v > 0.0;
            static bool HasValue(double v) => !double.IsNaN(v);

            if (HasValue(SpecialItems) && Pos(TotalAssets))
            {
                // Only capture losses (negative special items)
                double loss = Math.Min(SpecialItems, 0.0);
                return -loss / TotalAssets;
            }

            return double.NaN;
        }

        public double LossProvisionRaw =>
            (double.IsNaN(SpecialItems) || TotalAssets <= 0)
            ? double.NaN
            : SpecialItems / TotalAssets;
    }
}

