using Edgar.Models;

public sealed class CrspData : Dictionary<int, Dictionary<int, List<FirmTradingDay>>>
{
    public void AddDay(int year, int permno, FirmTradingDay day)
    {
        if (!TryGetValue(year, out var byPermno))
            this[year] = byPermno = new Dictionary<int, List<FirmTradingDay>>();

        if (!byPermno.TryGetValue(permno, out var list))
            byPermno[permno] = list = new List<FirmTradingDay>();

        list.Add(day);
    }

    public FirmTradingDay? GetDay(int year, int permno, DateTime date)
    {
        if (!TryGetValue(year, out var byPermno))
            return null;

        if (!byPermno.TryGetValue(permno, out var list) || list.Count == 0)
            return null;

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

    public IReadOnlyList<FirmTradingDay> GetDays(int year, int permno)
    {
        if (TryGetValue(year, out var byPermno) &&
            byPermno.TryGetValue(permno, out var list))
            return list;

        return Array.Empty<FirmTradingDay>();
    }

    public void SortAll()
    {
        foreach (var byPermno in Values)
            foreach (var list in byPermno.Values)
                list.Sort(static (a, b) => a.Date.CompareTo(b.Date));
    }

    public IEnumerable<(int Year, int Permno, FirmTradingDay Day)> AllDays()
    {
        foreach (var (year, byPermno) in this)
            foreach (var (permno, list) in byPermno)
                foreach (var day in list)
                    yield return (year, permno, day);
    }
}
