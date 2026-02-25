public sealed class CikPermnoMap
{
    // permno -> either string (single cik) or HashSet<string> (many ciks)
    private readonly Dictionary<int, object> _permnoToCiks = new();

    // cik -> either int (single permno) or HashSet<int> (many permnos)
    private readonly Dictionary<string, object> _cikToPermnos = new(StringComparer.Ordinal);

    public void Add(int permno, string cik)
    {
        if (permno <= 0)
            throw new ArgumentOutOfRangeException(nameof(permno));

        if (string.IsNullOrWhiteSpace(cik))
            throw new ArgumentException("CIK cannot be null/empty.", nameof(cik));

        cik = cik.Trim();

        AddPermnoToCik(permno, cik);
        AddCikToPermno(cik, permno);
    }

    private void AddPermnoToCik(int permno, string cik)
    {
        if (!_permnoToCiks.TryGetValue(permno, out var v))
        {
            _permnoToCiks[permno] = cik; // single
            return;
        }

        if (v is string s)
        {
            if (string.Equals(s, cik, StringComparison.Ordinal))
                return;

            // upgrade to set
            var set = new HashSet<string>(StringComparer.Ordinal) { s, cik };
            _permnoToCiks[permno] = set;
            return;
        }

        ((HashSet<string>)v).Add(cik);
    }

    private void AddCikToPermno(string cik, int permno)
    {
        if (!_cikToPermnos.TryGetValue(cik, out var v))
        {
            _cikToPermnos[cik] = permno; // single
            return;
        }

        if (v is int p)
        {
            if (p == permno)
                return;

            // upgrade to set
            var set = new HashSet<int> { p, permno };
            _cikToPermnos[cik] = set;
            return;
        }

        ((HashSet<int>)v).Add(permno);
    }

    // ---- Query APIs ----
    // These avoid allocations by returning enumerables.

    public IEnumerable<string> GetCiks(int permno)
    {
        if (!_permnoToCiks.TryGetValue(permno, out var v))
            yield break;

        if (v is string s)
        {
            yield return s;
            yield break;
        }

        foreach (var cik in (HashSet<string>)v)
            yield return cik;
    }

    public IEnumerable<int> GetPermnos(string cik)
    {
        if (string.IsNullOrWhiteSpace(cik))
            yield break;

        cik = cik.Trim();

        if (!_cikToPermnos.TryGetValue(cik, out var v))
            yield break;

        if (v is int p)
        {
            yield return p;
            yield break;
        }

        foreach (var permno in (HashSet<int>)v)
            yield return permno;
    }

    // Optional: fast existence checks
    public bool HasPermno(int permno) => _permnoToCiks.ContainsKey(permno);
    public bool HasCik(string cik) => !string.IsNullOrWhiteSpace(cik) && _cikToPermnos.ContainsKey(cik.Trim());
}
