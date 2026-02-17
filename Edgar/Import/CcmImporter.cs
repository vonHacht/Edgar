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
        private static readonly ILogger<Program> _logger = EdgarLogger.CreateLogger<Program>();

        private readonly string _ccmPath;
        private readonly CsvConfiguration _csvConfig;

        public CcmImporter(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _ccmPath = Path.Combine(settings.CompaniesDir, Filepaths.returnsCCMFileName);

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

            using var reader = new StreamReader(_ccmPath);
            var parser = new CsvParser(reader, _csvConfig);

            // Read header
            if (!parser.Read())
                return [];

            var header = parser.Record ?? Array.Empty<string>();

            int cikIdx = IndexOf(header, "cik");
            int permnoIdx = IndexOf(header, "LPERMNO");
            int permcoIdx = IndexOf(header, "LPERMCO");
            int yearIdx = IndexOf(header, "datadate");
            int companyNameIdx = IndexOf(header, "conm");
            int tickerIdx = IndexOf(header, "tic");

            if (cikIdx < 0 || permnoIdx < 0 || permcoIdx < 0 || yearIdx < 0 || companyNameIdx < 0 || tickerIdx < 0)
                throw new InvalidDataException("CCM CSV is missing required columns: cik, year, LPERMNO, LPERMCO, conm, tic");

            var results = new List<Ccm>();

            while (parser.Read())
            {
                var row = parser.Record;
                if (row is null || row.Length == 0)
                    continue;

                // Fast filters first
                if (!string.Equals(row[cikIdx], targetCik, StringComparison.Ordinal))
                    continue;

                if (!MatchesYear(row[yearIdx], targetYear))
                    continue;

                // Parse only when matched
                int? permno = TryParseInt(row[permnoIdx]);
                int? permco = TryParseInt(row[permcoIdx]);
                DateTime? tradingDay = DateTime.TryParse(row[yearIdx], CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : null;
                string companyName = row[companyNameIdx]?.Trim() ?? string.Empty;
                string ticker = row[tickerIdx]?.Trim() ?? string.Empty;

                results.Add(new Ccm { permno = permno, permco = permco, tradingDay = dt, CompanyName = companyName, Ticker = ticker });
            }

            return results;
        }

        private static int IndexOf(string[] header, string name)
        {
            for (int i = 0; i < header.Length; i++)
            {
                if (string.Equals(header[i], name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static int? TryParseInt(string? s)
            => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

        private static bool MatchesYear(string? dateText, string targetYear)
        {
            if (string.IsNullOrEmpty(dateText) || dateText.Length < 4)
                return false;

            // Compare first 4 chars (yyyy)
            return dateText.AsSpan(0, 4).SequenceEqual(targetYear);
        }

    }
}


