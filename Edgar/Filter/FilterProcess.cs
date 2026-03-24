using Edgar.Models;

namespace Edgar.Filter
{
    public static class FilterProcess
    {
        public static string Process(Filing filing,
            ExtractedSections sections,
            List<FirmTradingDay> ftd,
            BookToMarket btm)
        {
            // CRSP market capitalization data available
            // Stock price > $3 on day before filing
            // Returns and trading volume available for event window (days 0–3)
            DateTime filingDate = filing.DateFiled.Date;
            int dayBeforeFiling = 0;
            List<int> daysAfterFiling = new List<int>(4);

            for (int i = 0; i < ftd.Count; i++)
            {
                var td = ftd[i].Date.Date;

                if (td <= filingDate)
                {
                    dayBeforeFiling = i;
                }
                else
                {
                    daysAfterFiling.Add(i);

                    if (daysAfterFiling.Count == 4)
                        break;
                }
            }

            if (ftd[dayBeforeFiling].MarketCap == null)
                return "CRSP market capitalization data not available";
            if (ftd[dayBeforeFiling].Close < 3)
                return "Stock price below $3 on day before filing";
            foreach (int i in daysAfterFiling)
            {
                if (ftd[i].Ret == null || ftd[i].Volume == null)
                    return $"Returns and trading volume not available for day {i - dayBeforeFiling} after filing";
            }

            // Firm must be listed on a major U.S. exchange: NYSE, AMEX or NASDAQ
            ExchangeCodes exchange = ftd[0].ExchangeCodes;

            switch (exchange)
            {
                case ExchangeCodes.NYSE:
                case ExchangeCodes.AMEX:
                case ExchangeCodes.NASDAQ:
                    break;
                default:
                    return "Firm not listed on a major U.S. exchange NYSE, AMEX or NASDAQ";
            }

            // COMPUSTAT book-to-market data available
            // Book-value > 0
            if (btm.BookEquity <= 0)
                return "COMPUSTAT book-to-market data not available or less then 0";

            return "";
        }
    }
}
