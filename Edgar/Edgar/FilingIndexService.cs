using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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

        public async Task<List<Filing>> Get10KFilingsAsync(
            Firm firm,
            int startYear,
            int endYear,
            bool includeAmendments = false,
            CancellationToken ct = default)
        {
            if (firm == null) throw new ArgumentNullException(nameof(firm));
            if (string.IsNullOrWhiteSpace(firm.Cik10)) throw new ArgumentException("Firm.Cik10 is required.");

            var submissionsUrl = BuildSubmissionsUrl(firm.Cik10);
            var json = await _client.GetStringAsync(submissionsUrl, ct);

            using var doc = JsonDocument.Parse(json);

            var filings = ParseRecentFilings(doc, firm.Cik10);

            // Filter forms
            var allowedForms = includeAmendments
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "10-K", "10-K/A" }
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "10-K" };

            filings = filings
                .Where(f => allowedForms.Contains(f.Form))
                .Where(f => f.FilingDate.Year >= startYear && f.FilingDate.Year <= endYear)
                .ToList();

            // Select one filing per year (most recent filing date in that year)
            return PickOnePerYear(filings);
        }

        public static string BuildSubmissionsUrl(string cik10)
            => $"https://data.sec.gov/submissions/CIK{cik10}.json";

        private static List<Filing> ParseRecentFilings(JsonDocument doc, string cik10)
        {
            // Path: root.filings.recent
            // Arrays: form[], accessionNumber[], filingDate[], primaryDocument[], (optional) reportDate[]
            var root = doc.RootElement;

            if (!root.TryGetProperty("filings", out var filingsEl) ||
                !filingsEl.TryGetProperty("recent", out var recentEl))
            {
                return new List<Filing>();
            }

            var forms = seeArray(recentEl, "form");
            var accs = seeArray(recentEl, "accessionNumber");
            var dates = seeArray(recentEl, "filingDate");
            var primaryDocs = seeArray(recentEl, "primaryDocument");

            // Optional
            var reportDates = tryArray(recentEl, "reportDate");

            var n = new[] { forms.Count, accs.Count, dates.Count, primaryDocs.Count }.Min();

            var result = new List<Filing>(capacity: n);

            for (int i = 0; i < n; i++)
            {
                var form = forms[i];
                var acc = accs[i];
                var dateStr = dates[i];
                var primary = primaryDocs[i];

                if (string.IsNullOrWhiteSpace(form) ||
                    string.IsNullOrWhiteSpace(acc) ||
                    string.IsNullOrWhiteSpace(dateStr) ||
                    string.IsNullOrWhiteSpace(primary))
                {
                    continue;
                }

                if (!DateTime.TryParse(dateStr, out var filingDate))
                    continue;

                DateTime? periodOfReport = null;
                if (reportDates != null && i < reportDates.Count && DateTime.TryParse(reportDates[i], out var rep))
                    periodOfReport = rep;

                result.Add(new Filing
                {
                    Cik10 = cik10,
                    Form = form,
                    AccessionNumber = acc,
                    FilingDate = filingDate,
                    PrimaryDocument = primary,
                    PeriodOfReport = periodOfReport
                });
            }

            return result;

            static List<string> seeArray(JsonElement parent, string name)
            {
                if (!parent.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
                    return new List<string>();

                return el.EnumerateArray()
                         .Select(x => seeString(x))
                         .ToList();
            }

            static List<string>? tryArray(JsonElement parent, string name)
            {
                if (!parent.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
                    return null;

                return el.EnumerateArray()
                         .Select(x => seeString(x))
                         .ToList();
            }

            static string seeString(JsonElement el)
            {
                if (el.ValueKind == JsonValueKind.String)
                    return el.GetString() ?? string.Empty;

                // Sometimes fields can be null in SEC JSON; treat as empty string.
                return string.Empty;
            }
        }

        private static List<Filing> PickOnePerYear(List<Filing> filings)
        {
            return filings
                .GroupBy(f => f.FilingDate.Year)
                .Select(g => g.OrderByDescending(f => f.FilingDate).First())
                .OrderBy(f => f.FilingDate)
                .ToList();
        }
    }
}
