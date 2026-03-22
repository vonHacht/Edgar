namespace Edgar.Models
{
    public sealed class FirmTradingDays
    {
        // year -> permno -> sorted list of days
        private readonly Dictionary<int, Dictionary<int, List<FirmTradingDay>>> _data
            = new();

        public bool ContainsKey(int year)
        {
            if (!_data.TryGetValue(year, out var byPermno) || byPermno.Count == 0)
            {
                return false;
            }

            return true;
        }

        public void Add(int year, int permno, FirmTradingDay day)
        {
            if (!_data.TryGetValue(year, out var byPermno))
                _data[year] = byPermno = new Dictionary<int, List<FirmTradingDay>>();

            if (!byPermno.TryGetValue(permno, out var list))
                byPermno[permno] = list = new List<FirmTradingDay>();

            list.Add(day); // no sorting here
        }

        public void SortAll()
        {
            foreach (var byPermno in _data.Values)
                foreach (var list in byPermno.Values)
                    list.Sort(static (a, b) => a.Date.CompareTo(b.Date));
        }

        public IReadOnlyList<FirmTradingDay> GetDays(int year, int permno)
        {
            if (_data.TryGetValue(year, out var byPermno) &&
                byPermno.TryGetValue(permno, out var list))
                return list;

            return Array.Empty<FirmTradingDay>();
        }

        public FirmTradingDay? GetDay(int year, int permno, DateTime date)
        {
            if (!_data.TryGetValue(year, out var byPermno))
                return null;

            if (!byPermno.TryGetValue(permno, out var list) || list.Count == 0)
                return null;

            // binary search (same as your CrspData)
            int lo = 0;
            int hi = list.Count - 1;

            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                var cmp = list[mid].Date.CompareTo(date);

                if (cmp == 0)
                    return list[mid];
                if (cmp < 0)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            return null;
        }

        private List<FirmTradingDay> GetFirmHistoryUpTo(DateTime date, int permno)
        {
            var result = new List<FirmTradingDay>();

            int year = date.Year;

            for (int y = year - 1; y <= year; y++)
            {
                if (_data.TryGetValue(y, out var byPermno) &&
                    byPermno.TryGetValue(permno, out var list))
                {
                    result.AddRange(list.Where(x => x.Date < date));
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

            var priorDays = history.TakeLast(d).ToList();

            return priorDays.Count == d
                ? priorDays
                : Array.Empty<FirmTradingDay>();
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
                total += DailyTurnover(day);

            return total / priorDays.Count;
        }

        public decimal CumulativeTurnover(DateTime date, int permno, int d = 60)
        {
            var priorDays = GetPriorTradingDays(date, permno, d);
            if (priorDays.Count == 0)
                return 0m;

            decimal total = 0m;

            foreach (var day in priorDays)
                total += DailyTurnover(day);

            return total;
        }

        public decimal FilingDayReturn(DateTime date, int permno)
        {
            var day = GetDay(date.Year, permno, date);
            if (day is null)
                return 0m;

            return day.Ret.HasValue ? (decimal)day.Ret.Value : 0m;
        }
    }
}
