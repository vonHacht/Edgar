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
    }
}
