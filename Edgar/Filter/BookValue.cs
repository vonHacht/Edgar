using Edgar.Models;

namespace Edgar.Filter
{
    public partial class Filter
    {
        public static double BookValue(BookToMarket b)
        {
            static double Nz(double? v) => v ?? 0.0;
            static bool Pos(double? v) => v.HasValue && v.Value > 0.0;

            // Preferred stock: pick first positive in priority order
            double preferred =
                Pos(b.pstkrv) ? b.pstkrv!.Value :
                Pos(b.pstkl) ? b.pstkl!.Value :
                Pos(b.pstk) ? b.pstk!.Value :
                0.0;

            // Base book equity (before adding txditc and subtracting preferred)
            double baseEquity;

            if (Pos(b.seq))
            {
                baseEquity = b.seq!.Value;
            }
            else if (Pos(b.ceq))
            {
                // If you intended: CEQ + PSTK (since you later subtract preferred, this matches your original structure)
                baseEquity = b.ceq!.Value + Nz(b.pstk);
            }
            else if (Pos(b.at) && Pos(b.lt))
            {
                baseEquity = Nz(b.at) - Nz(b.lt);
            }
            else
            {
                baseEquity = 0.0;
            }

            // Add deferred taxes (txditc), then subtract preferred stock
            return baseEquity + Nz(b.txditc) - preferred;
        }
        public static bool BookValueAboveZero(BookToMarket b) => BookValue(b) > 0.0;
    }
}

