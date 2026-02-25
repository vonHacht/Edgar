namespace Edgar.Models
{
    public class BookToMarket
    {

        public double CommonEquity { get; set; } // ceq
        public double ShareholdersEquity { get; set; } // seq

        public double PrefferedStockRedemptionValue { get; set; } // pstkrv

        public double PrefferedStockLiquidatingValue { get; set; } // pstkl

        public double PrefferedStock { get; set; } // pstk

        public double DeferredTaxes { get; set; } // txditc

        public double TotalAssets { get; set; } // at

        public double TotalLiabilities { get; set; } // lt

        public double BookEquity { get { return _bookEquity(); } }

        private double _bookEquity()
        {
            static bool IsMissing(double v) => double.IsNaN(v);
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
    }
}

