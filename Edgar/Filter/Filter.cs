using Edgar.Models;

namespace Edgar.Filter
{
    public class Filter
    {
        public static bool NonMatchingCIK(Firm firm)
        {
            return firm.FirmTradingDays.Count == 0;
        }

        public static bool Delisted(Firm firm)
        {
            bool delisted = false;

            firm.FirmTradingDays.ForEach(tradingDay =>
            {
                if (tradingDay.DelistCode != null)
                {
                    delisted = true;
                }
            });

            return delisted;
        }

    }
}
