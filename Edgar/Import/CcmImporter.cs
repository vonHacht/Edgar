using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using Edgar.Config;
using Edgar.Logging;
using Edgar.Models;

using Microsoft.Extensions.Logging;

namespace Edgar.Import
{
    public sealed class CcmImporter
    {
        private static readonly ILogger<Program> _logger =
            EdgarLogger.CreateLogger<Program>();

        private readonly string _ccmPath;
        private readonly CsvConfiguration _csvConfig;

        // Cache: Year -> all CIKs present that year
        private readonly Dictionary<string, HashSet<string>> _cikPresenceByYear = new();

        public CcmImporter(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _ccmPath = settings.CcmFilename;

            _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                PrepareHeaderForMatch = args => args.Header.Replace("\uFEFF", "").Trim(),
                MissingFieldFound = null,
                HeaderValidated = null
            };
        }

        public List<Ccm> ReadByCik(string cik, string year)
        {
            if (string.IsNullOrWhiteSpace(cik) || string.IsNullOrWhiteSpace(year))
                return [];

            if (!File.Exists(_ccmPath))
                throw new FileNotFoundException($"CCM path not found: {_ccmPath}");

            var targetCik = cik.Trim();
            var targetYear = year.Trim();

            // Build presence cache if first time for this year
            if (!_cikPresenceByYear.TryGetValue(targetYear, out var cikSet))
            {
                cikSet = BuildCikPresenceForYear(targetYear);
                _cikPresenceByYear[targetYear] = cikSet;

                _logger.LogInformation(
                    "Built CCM presence cache for year {Year}, CIK count={Count}",
                    targetYear, cikSet.Count);
            }

            // If CIK not present → return immediately
            if (!cikSet.Contains(targetCik))
                return [];

            // Otherwise read rows normally
            return ReadRowsForCik(targetCik, targetYear);
        }

        private HashSet<string> BuildCikPresenceForYear(string targetYear)
        {
            using var reader = new StreamReader(_ccmPath);
            var parser = new CsvParser(reader, _csvConfig);

            if (!parser.Read())
                return new HashSet<string>(StringComparer.Ordinal);

            var header = parser.Record ?? Array.Empty<string>();

            int cikIdx = IndexOf(header, "cik");
            int yearIdx = IndexOf(header, "datadate");

            if (cikIdx < 0 || yearIdx < 0)
                throw new InvalidDataException("CCM CSV missing cik/datadate");

            var set = new HashSet<string>(StringComparer.Ordinal);

            while (parser.Read())
            {
                var row = parser.Record;
                if (row is null || row.Length == 0)
                    continue;

                if (!MatchesYear(row[yearIdx], targetYear))
                    continue;

                var cik = row[cikIdx]?.Trim();
                if (!string.IsNullOrEmpty(cik))
                    set.Add(cik);
            }

            return set;
        }

        private List<Ccm> ReadRowsForCik(string targetCik, string targetYear)
        {
            using var reader = new StreamReader(_ccmPath);
            var parser = new CsvParser(reader, _csvConfig);

            if (!parser.Read())
                return [];

            var header = parser.Record ?? Array.Empty<string>();

            int cikIdx = IndexOf(header, "cik");
            int permnoIdx = IndexOf(header, "LPERMNO");
            int permcoIdx = IndexOf(header, "LPERMCO");
            int yearIdx = IndexOf(header, "datadate");
            int companyNameIdx = IndexOf(header, "conm");
            int tickerIdx = IndexOf(header, "tic");

            if (cikIdx < 0 || permnoIdx < 0 || permcoIdx < 0 ||
                yearIdx < 0 || companyNameIdx < 0 || tickerIdx < 0)
                throw new InvalidDataException("CCM CSV missing required columns");

            var results = new List<Ccm>();

            while (parser.Read())
            {
                var row = parser.Record;
                if (row is null || row.Length == 0)
                    continue;

                if (!string.Equals(row[cikIdx], targetCik, StringComparison.Ordinal))
                    continue;

                if (!MatchesYear(row[yearIdx], targetYear))
                    continue;

                int? permno = TryParseInt(row[permnoIdx]);
                int? permco = TryParseInt(row[permcoIdx]);
                DateTime? tradingDay = DateTime.TryParse(
                    row[yearIdx],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var dt) ? dt : null;

                results.Add(new Ccm
                {
                    permno = permno,
                    permco = permco,
                    tradingDay = tradingDay,
                    CompanyName = row[companyNameIdx]?.Trim() ?? "",
                    Ticker = row[tickerIdx]?.Trim() ?? ""
                });
            }

            return results;
        }

        private static int IndexOf(string[] header, string name)
        {
            for (int i = 0; i < header.Length; i++)
                if (string.Equals(header[i], name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        private static int? TryParseInt(string? s)
            => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

        private static bool MatchesYear(string? dateText, string targetYear)
            => !string.IsNullOrEmpty(dateText)
               && dateText.Length >= 4
               && dateText.AsSpan(0, 4).SequenceEqual(targetYear);
    }
}


