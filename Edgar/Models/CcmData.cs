using System.Diagnostics.CodeAnalysis;

using Edgar.Models;

public sealed class CcmData : Dictionary<int, Dictionary<string, HashSet<Ccm>>>
{
    private Dictionary<int, string> CikToPermnoMapping = new Dictionary<int, string>();

    public void Add(int year, string cik, Ccm ccm)
    {
        ArgumentNullException.ThrowIfNull(ccm);
        if (string.IsNullOrWhiteSpace(cik))
            throw new ArgumentException("CIK cannot be null/empty.", nameof(cik));

        cik = cik.Trim();

        if (!TryGetValue(year, out var byCik))
        {
            byCik = new Dictionary<string, HashSet<Ccm>>(StringComparer.Ordinal);
            this[year] = byCik;
        }

        if (!byCik.TryGetValue(cik, out var set))
        {
            set = new HashSet<Ccm>();
            byCik[cik] = set;
        }

        set.Add(ccm);
        if (CikToPermnoMapping.ContainsKey(ccm.permno ?? 0))
            return;
        CikToPermnoMapping.Add(ccm.permno ?? 0, cik);
    }

    public bool TryGet(int year, string cik, [NotNullWhen(true)] out HashSet<Ccm>? set)
    {
        set = null;
        if (string.IsNullOrWhiteSpace(cik))
            return false;

        cik = cik.Trim();

        return TryGetValue(year, out var byCik)
            && byCik.TryGetValue(cik, out set);
    }

    public IReadOnlyCollection<Ccm> Get(int year, int permno)
    {
        if (!CikToPermnoMapping.TryGetValue(permno, out var cik))
            return Array.Empty<Ccm>();

        return GetOrEmpty(year, cik);
    }

    public IReadOnlyCollection<Ccm> GetOrEmpty(int year, string cik)
        => TryGet(year, cik, out var set) ? set : Array.Empty<Ccm>();

    public IEnumerable<(int Year, string Cik, Ccm Ccm)> All()
    {
        foreach (var (year, byCik) in this)
            foreach (var (cik, set) in byCik)
                foreach (var ccm in set)
                    yield return (year, cik, ccm);
    }
}
