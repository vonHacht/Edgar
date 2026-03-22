namespace Edgar.Models
{
    public sealed class FirmTradingDays
    {
        private Dictionary<int, List<FirmTradingDay>> _byYear = new Dictionary<int, List<FirmTradingDay>>();

        /* public FirmTradingDays(IEnumerable<FirmTradingDay> items)
        {
            _byYear = items
                .GroupBy(x => x.Date.Year)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.Date).ToList());
        } */

        public void Sort()
        {
            foreach (var list in _byYear.Values)
            {
                list.Sort((a, b) => a.Date.CompareTo(b.Date));
            }
        }

        public void Add(int year, FirmTradingDay day)
        {
            if (!_byYear.TryGetValue(year, out var list))
                _byYear[year] = list = new List<FirmTradingDay>();
            
            list.Add(day);
            list.Sort(static (a, b) => a.Date.CompareTo(b.Date));
        }

        public IReadOnlyList<FirmTradingDay> GetByYear(int year) =>
            _byYear.TryGetValue(year, out var result)
                ? result
                : Array.Empty<FirmTradingDay>();

        private List<FirmTradingDay> GetFirmHistoryUpTo(DateTime date, int permno)
        {
            var result = new List<FirmTradingDay>();

            int year = date.Year;

            // Collect current year + previous year (usually enough for 60/90 days)
            for (int y = year - 1; y <= year; y++)
            {
                if (_byYear.TryGetValue(y, out var days))
                {
                    result.AddRange(days.Where(x => x.Permno == permno && x.Date < date));
                }
            }

            return result
                .OrderBy(x => x.Date)
                .ToList();
        }

        private IReadOnlyList<FirmTradingDay> GetPriorTradingDays(DateTime date, int permno, int d)
        {
            if (d != 60 && d != 90)
                throw new NotSupportedException("Only 60-day and 90-day windows are supported.");

            var history = GetFirmHistoryUpTo(date, permno);

            var priorDays = history
                .TakeLast(d)
                .ToList();

            return priorDays.Count == d
                ? priorDays
                : Array.Empty<FirmTradingDay>();
        }

        private FirmTradingDay? GetTradingDay(DateTime date, int permno)
        {
            if (!_byYear.TryGetValue(date.Year, out var days))
                return null;

            return days.FirstOrDefault(x => x.Permno == permno && x.Date == date);
        }

        public decimal PriorReturn(DateTime date, int permno, int d = 60)
        {
            var priorDays = GetPriorTradingDays(date, permno, d);
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

        public decimal RealizedVariance(DateTime date, int permno, int d = 60)
        {
            var priorDays = GetPriorTradingDays(date, permno, d);
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

        public decimal RealizedVolatility(DateTime date, int permno, int d = 60)
        {
            var rv = RealizedVariance(date, permno, d);
            return rv > 0m ? (decimal)Math.Sqrt((double)rv) : 0m;
        }

        private static decimal DailyTurnover(FirmTradingDay day)
        {
            if (!day.Volume.HasValue || !day.SharesOut.HasValue || day.SharesOut.Value == 0)
                return 0m;

            return (decimal)day.Volume.Value / day.SharesOut.Value;
        }

        public decimal AverageTurnover(DateTime date, int permno, int d = 60)
        {
            var priorDays = GetPriorTradingDays(date, permno, d);
            if (priorDays.Count == 0)
                return 0m;

            decimal total = 0m;

            foreach (var day in priorDays)
            {
                total += DailyTurnover(day);
            }

            return total / priorDays.Count;
        }

        public decimal CumulativeTurnover(DateTime date, int permno, int d = 60)
        {
            var priorDays = GetPriorTradingDays(date, permno, d);
            if (priorDays.Count == 0)
                return 0m;

            decimal total = 0m;

            foreach (var day in priorDays)
            {
                total += DailyTurnover(day);
            }

            return total;
        }

        public decimal FilingDayReturn(DateTime date, int permno)
        {
            var day = GetTradingDay(date, permno);
            if (day is null)
                return 0m;

            return day.Ret.HasValue ? (decimal)day.Ret.Value : 0m;
        }
    }
}
