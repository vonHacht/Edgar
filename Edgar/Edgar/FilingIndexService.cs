using System.Globalization;

using Edgar.Models;

namespace Edgar.Edgar
{
    /// <summary>
    /// Retrieves filing metadata from SEC "company submissions" JSON and filters 10-K filings.
    /// Endpoint: https://data.sec.gov/submissions/CIK##########.json
    /// </summary>
    public class FilingIndexService
    {
        private readonly EdgarClient _client;

        public FilingIndexService(EdgarClient client)
        {
            _client = client;
        }

        public static string BuildSubmissionsUrl(string cik10)
            => $"https://data.sec.gov/submissions/CIK{cik10}.json";

        public async Task<List<Filing>> Get10KFilingsForYearAsync(
            int year,
            bool includeAmendments = false,
            CancellationToken ct = default)
        {
            var allowedForms = includeAmendments
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "10-K", "10-K/A" }
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "10-K" };

            var all = new List<Filing>();

            for (var q = 1; q <= 4; q++)
            {
                var url = BuildMasterIndexUrl(year, q);
                var text = await _client.GetStringAsync(url, ct);

                foreach (var filing in ParseMasterIdx(text))
                {
                    if (!allowedForms.Contains(filing.FormType))
                    {
                        continue;
                    }

                    if (filing.DateFiled.Year != year)
                    {
                        continue;
                    }

                    all.Add(filing);
                }
            }

            // Typical research choice: one 10-K per CIK per year (keep the latest filing date in that year)
            // return PickOnePerCikPerYear(all);
            // ... but if you want to keep all filings that are "far enough apart" (e.g. to keep some amendments but not near-duplicates), use this instead:
            return PickAllRightful10Ks(all, minDaysBetweenFilings: 180);
        }

        public static string BuildMasterIndexUrl(int year, int quarter)
            => $"https://www.sec.gov/Archives/edgar/full-index/{year}/QTR{quarter}/master.idx";

        private static IEnumerable<Filing> ParseMasterIdx(string masterIdxText)
        {
            // master.idx format (pipe-delimited after header):
            // CIK|Company Name|Form Type|Date Filed|Filename
            // Filename example: edgar/data/1000045/0001193125-10-001234.txt

            using var sr = new StringReader(masterIdxText);

            string? line;
            var dataStarted = false;

            while ((line = sr.ReadLine()) != null)
            {
                if (!dataStarted)
                {
                    // Data starts after a line containing "-----"
                    if (line.StartsWith("-----", StringComparison.Ordinal))
                    {
                        dataStarted = true;
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split('|');

                if (parts.Length < 5)
                {
                    continue;
                }

                var cikRaw = parts[0].Trim();
                var company = parts[1].Trim();
                var form = parts[2].Trim();
                var dateStr = parts[3].Trim();
                var filename = parts[4].Trim(); // relative under /Archives/

                if (!int.TryParse(cikRaw, out var cikInt))
                {
                    continue;
                }

                if (!DateTime.TryParseExact(
                        dateStr,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var filed))
                {
                    continue;
                }

                // master.idx gives us the submission .txt filename. We can infer accession number from it.
                // Example filename: edgar/data/{cik}/{accession-with-dashes}.txt
                //var accessionWithExt = Path.GetFileName(filename); // 0001193125-10-001234.txt
                //var accession = Path.GetFileNameWithoutExtension(accessionWithExt); // 0001193125-10-001234

                yield return new Filing
                {
                    CIK = cikInt.ToString("D10"),
                    CompanyName = company,
                    FormType = form,
                    DateFiled = filed,
                    Filename = filename,
                };
            }
        }

        private static List<Filing> PickOnePerCikPerYear(List<Filing> filings)
        {
            return filings
                .GroupBy(f => (f.CIK, Year: f.DateFiled.Year))
                .Select(g => g.OrderByDescending(x => x.DateFiled).First())
                .OrderBy(f => f.DateFiled)
                .ToList();
        }

        private static List<Filing> PickAllRightful10Ks(
    List<Filing> filings,
    int minDaysBetweenFilings = 180)
        {
            if (filings == null || filings.Count == 0)
            {
                return new List<Filing>();
            }

            return filings
                // avoid exact duplicates (sometimes master.idx can repeat rows)
                .GroupBy(f => (f.CIK, f.Filename), StringTupleComparer.Instance)
                .Select(g => g.OrderByDescending(x => x.DateFiled).First())
                .GroupBy(f => f.CIK)
                .SelectMany(g =>
                {
                    var ordered = g.OrderBy(x => x.DateFiled).ToList();
                    var kept = new List<Filing>(capacity: ordered.Count);

                    Filing? lastKept = null;

                    foreach (var f in ordered)
                    {
                        if (lastKept == null)
                        {
                            kept.Add(f);
                            lastKept = f;
                            continue;
                        }

                        var days = (f.DateFiled - lastKept.DateFiled).TotalDays;

                        // keep only if it's far enough from the last kept filing
                        if (days >= minDaysBetweenFilings)
                        {
                            kept.Add(f);
                            lastKept = f;
                        }
                        else
                        {
                            // Too close: likely amendment/dup/near-duplicate.
                            // If you want to prefer the *later* one within the cluster, swap logic here.
                        }
                    }

                    return kept;
                })
                .OrderBy(f => f.DateFiled)
                .ToList();
        }

        // Helper comparer so GroupBy on (string, string) uses OrdinalIgnoreCase for CIK/Filename if you want.
        private sealed class StringTupleComparer : IEqualityComparer<(string CIK, string Filename)>
        {
            public static readonly StringTupleComparer Instance = new();

            public bool Equals((string CIK, string Filename) x, (string CIK, string Filename) y)
                => string.Equals(x.CIK, y.CIK, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(x.Filename, y.Filename, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((string CIK, string Filename) obj)
                => HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.CIK ?? ""),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Filename ?? "")
                );
        }

    }
}

