using Edgar.Models;

public class BookToMarketData : Dictionary<int, Dictionary<string, List<BookToMarket>>>
{

    public void Add(int year, string cik, BookToMarket item)
    {
        if (item == null) return;
        if (string.IsNullOrWhiteSpace(cik)) return;

        // Ensure year bucket exists
        if (!TryGetValue(year, out var byCik))
        {
            byCik = new Dictionary<string, List<BookToMarket>>(StringComparer.Ordinal);
            this[year] = byCik;
        }

        // Ensure cik bucket exists
        if (!byCik.TryGetValue(cik, out var list))
        {
            list = new List<BookToMarket>();
            byCik[cik] = list;
        }

        // Add the item
        list.Add(item);
    }

    public IReadOnlyList<BookToMarket> Get(int year, string cik)
    {
        if (string.IsNullOrWhiteSpace(cik))
            return Array.Empty<BookToMarket>();

        if (TryGetValue(year, out var byCik) &&
            byCik.TryGetValue(cik.Trim(), out var list))
            return list;

        return Array.Empty<BookToMarket>();
    }

    public bool Remove(int year, string cik, BookToMarket item)
    {
        if (item == null || string.IsNullOrWhiteSpace(cik))
            return false;

        cik = cik.Trim();

        if (!TryGetValue(year, out var byCik))
            return false;

        if (!byCik.TryGetValue(cik, out var list))
            return false;

        bool removed = list.Remove(item);

        if (removed && list.Count == 0)
        {
            byCik.Remove(cik);
            if (byCik.Count == 0)
                Remove(year);
        }

        return removed;
    }

    public bool RemoveFirm(int year, string cik)
    {
        if (string.IsNullOrWhiteSpace(cik))
            return false;

        cik = cik.Trim();

        if (!TryGetValue(year, out var byCik))
            return false;

        bool removed = byCik.Remove(cik);

        if (removed && byCik.Count == 0)
            Remove(year);

        return removed;
    }

    public bool RemoveYear(int year)
    {
        return Remove(year);
    }

    public bool HaveCik(int year, string cik)
    {
        if (string.IsNullOrWhiteSpace(cik))
            return false;

        cik = cik.Trim();

        return TryGetValue(year, out var byCik) &&
               byCik.TryGetValue(cik, out var list) &&
               list.Count > 0;
    }
}
