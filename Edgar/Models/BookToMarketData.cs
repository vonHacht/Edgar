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
}
