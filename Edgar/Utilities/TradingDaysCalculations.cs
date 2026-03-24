using Edgar.Models;

namespace Edgar.Utilities
{
    public static class TradingDaysCalculations
    {
        private static List<FirmTradingDay> GetFirmHistoryUpTo(List<FirmTradingDay> TradingDays, DateTime date)
        {
            var result = new List<FirmTradingDay>();

            int year = date.Year;

            for (int y = year - 1; y <= year; y++)
            {
                result.AddRange(TradingDays.Where(x => x.Date < date));
            }

            return result
                .OrderBy(x => x.Date)
                .ToList();
        }

        private static IReadOnlyList<FirmTradingDay> GetPriorTradingDays(List<FirmTradingDay> TradingDays, DateTime Date, int d)
        {
            if (d != 60 && d != 90)
                throw new NotSupportedException("Only 60-day and 90-day windows are supported.");

            var history = GetFirmHistoryUpTo(TradingDays, Date);

            var priorDays = history.TakeLast(d).ToList();

            return priorDays.Count == d
                ? priorDays
                : Array.Empty<FirmTradingDay>();
        }

        public static decimal PriorReturn(List<FirmTradingDay> TradingDays, DateTime date, int d = 60)
        {
            var priorDays = GetPriorTradingDays(TradingDays, date, d);
            if (priorDays.Count == 0)
                return 0m;

            decimal compounded = 1m;

            foreach (var day in priorDays)
            {
                var r = (decimal)(day.Ret ?? 0.0);
                compounded *= (1m + r);
            }

            return compounded - 1m;
        }

        public static decimal RealizedVariance(List<FirmTradingDay> TradingDays, DateTime date, int d = 60)
        {
            var priorDays = GetPriorTradingDays(TradingDays, date, d);
            if (priorDays.Count == 0)
                return 0m;

            decimal rv = 0m;

            foreach (var day in priorDays)
            {
                var r = (decimal)(day.Ret ?? 0.0);
                rv += r * r;
            }

            return rv;
        }

        public static decimal RealizedVolatility(List<FirmTradingDay> TradingDays, DateTime date, int d = 60)
        {
            var rv = RealizedVariance(TradingDays, date, d);
            return rv > 0m ? (decimal)Math.Sqrt((double)rv) : 0m;
        }

        private static decimal DailyTurnover(FirmTradingDay day)
        {
            if (!day.Volume.HasValue || !day.SharesOut.HasValue || day.SharesOut.Value == 0)
                return 0m;

            return (decimal)day.Volume.Value / day.SharesOut.Value;
        }

        public static decimal AverageTurnover(List<FirmTradingDay> TradingDays, DateTime date, int d = 60)
        {
            var priorDays = GetPriorTradingDays(TradingDays, date, d);
            if (priorDays.Count == 0)
                return 0m;

            decimal total = 0m;

            foreach (var day in priorDays)
                total += DailyTurnover(day);

            return total / priorDays.Count;
        }

        public static decimal CumulativeTurnover(List<FirmTradingDay> TradingDays, DateTime date, int d = 60)
        {
            var priorDays = GetPriorTradingDays(TradingDays, date, d);
            if (priorDays.Count == 0)
                return 0m;

            decimal total = 0m;

            foreach (var day in priorDays)
                total += DailyTurnover(day);

            return total;
        }

        private static FirmTradingDay? GetClosestDay(List<FirmTradingDay> tradingDays, DateTime date)
        {
            if (tradingDays == null || tradingDays.Count == 0)
                return null;

            int lo = 0;
            int hi = tradingDays.Count - 1;

            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                var cmp = tradingDays[mid].Date.CompareTo(date);

                if (cmp == 0)
                    return tradingDays[mid];

                if (cmp < 0)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            // After loop:
            // hi = closest smaller
            // lo = closest larger

            if (lo >= tradingDays.Count)
                return tradingDays[hi];

            if (hi < 0)
                return tradingDays[lo];

            var before = tradingDays[hi];
            var after = tradingDays[lo];

            var diffBefore = Math.Abs((before.Date - date).Ticks);
            var diffAfter = Math.Abs((after.Date - date).Ticks);

            return diffBefore <= diffAfter ? before : after;
        }

        public static decimal FilingDayReturn(List<FirmTradingDay> TradingDays, DateTime date)
        {
            var day = GetClosestDay(TradingDays, date);
            if (day is null)
                return 0m;

            return day.Ret.HasValue ? (decimal)day.Ret.Value : 0m;
        }

        public static decimal Return4Days(List<FirmTradingDay> tradingDays, DateTime filingDate)
        {
            if (tradingDays == null || tradingDays.Count == 0)
                return 0m;

            int startIndex = -1;

            for (int i = 0; i < tradingDays.Count; i++)
            {
                if (tradingDays[i].Date >= filingDate)
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex == -1 || startIndex + 4 > tradingDays.Count)
                return 0m;

            decimal compounded = 1m;

            for (int i = 0; i < 4; i++)
            {
                var day = tradingDays[startIndex + i];
                var r = (decimal)(day.Ret ?? 0.0);
                compounded *= (1m + r);
            }

            return compounded - 1m;
        }
    }
