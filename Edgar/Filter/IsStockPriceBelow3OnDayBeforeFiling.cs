using Edgar.Models;

namespace Edgar.Filter
{
    public partial class Filter
    {
        /// <summary>
        /// True when price on the most recent trading day BEFORE filing date is below $3.
        /// </summary>
        public static bool IsStockPriceBelow3OnDayBeforeFiling(DateTime filingDate, List<FirmTradingDay> tradingDays)
        {
            var prev = tradingDays
                .Where(d => d.Date < filingDate)
                .OrderByDescending(d => d.Date)
                .FirstOrDefault();

            // If there's no prior trading day, treat as failing the rule.
            if (prev is null) return true;

            return prev.Close < 3m;
        }
    }
}
