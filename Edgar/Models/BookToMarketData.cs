using Edgar.Models;

public class BookToMarketData : SortedDictionary<int, Dictionary<string, List<BookToMarket>>>
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

    public BookToMarket? Get(int year, string cik, DateTime filingDate)
    {
        if (string.IsNullOrWhiteSpace(cik))
            return null;

        if (TryGetValue(year, out var byCik) &&
            byCik.TryGetValue(cik.Trim(), out var list) &&
            list.Count > 0)
        {
            return list.MinBy(item => Math.Abs((item.Date - filingDate).Ticks));
        }

        return null;
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

    public void ComputeForwardLossProvision()
    {
        foreach (var (year, byCik) in this)
        {
            int nextYear = year + 1;

            if (!TryGetValue(nextYear, out var nextByCik))
                continue;

            foreach (var (cik, list) in byCik)
            {
                if (!nextByCik.TryGetValue(cik, out var nextList) || nextList.Count == 0)
                    continue;

                // You may want to align by date more precisely, but for now:
                // assume one observation per year and take the first
                var nextItem = nextList[0];

                foreach (var item in list)
                {
                    if (double.IsNaN(nextItem.SpecialItems) || item.TotalAssets <= 0)
                    {
                        item.LossProvisionRaw = double.NaN;
                        continue;
                    }

                    item.LossProvisionRaw = nextItem.SpecialItems / item.TotalAssets;

                    double loss = Math.Min(nextItem.SpecialItems, 0.0);
                    item.LossProvision = -loss / item.TotalAssets;
                }
            }
        }
    }
}
