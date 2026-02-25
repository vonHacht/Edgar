using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using Edgar.Config;

namespace Edgar.Import
{
    public sealed class CcmImporter
    {
        private readonly string _ccmPath;
        private readonly CsvConfiguration _csvConfig;

        // Column indices
        private int _cikIdx, _permnoIdx, _permcoIdx, _companyNameIdx, _tickerIdx, _datadateIdx;

        public CcmImporter(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _ccmPath = settings.CcmFilename
                ?? throw new ArgumentException("CcmFilename is null.", nameof(settings));

            EnsureFileExists(_ccmPath);

            _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                PrepareHeaderForMatch = args => args.Header.Replace("\uFEFF", "").Trim(),
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null
            };

            LoadIndices();
        }

        public CikPermnoMap ReadAllYearsUniqueCcms()
        {
            var results = new CikPermnoMap();

            using var reader = new StreamReader(_ccmPath);
            using var parser = new CsvParser(reader, _csvConfig);

            // consume header
            if (!parser.Read())
                return results;

            while (parser.Read())
            {
                var row = parser.Record;
                if (row is null || row.Length == 0)
                    continue;

                // cik
                var cik = (GetField(row, _cikIdx) ?? string.Empty).Trim();
                if (cik.Length == 0)
                    continue;

                // permno / permco (at least one)
                var permno = ReadIntOrNull(row, _permnoIdx);

                results.Add(permno ?? 0, cik);
            }

            return results;
        }

        private void LoadIndices()
        {
            using var reader = new StreamReader(_ccmPath);
            using var parser = new CsvParser(reader, _csvConfig);

            if (!parser.Read())
                throw new InvalidDataException("CCM CSV appears empty (no header).");

            var header = parser.Record ?? Array.Empty<string>();

            _cikIdx = IndexOf(header, "cik");
            _permnoIdx = IndexOf(header, "LPERMNO");
            _permcoIdx = IndexOf(header, "LPERMCO");
            _companyNameIdx = IndexOf(header, "conm");
            _tickerIdx = IndexOf(header, "tic");
            _datadateIdx = IndexOf(header, "datadate");

            if (_cikIdx < 0 || _datadateIdx < 0 || _permnoIdx < 0 || _permcoIdx < 0 || _companyNameIdx < 0 || _tickerIdx < 0)
                throw new InvalidDataException("CCM CSV is missing required columns: cik, datadate, LPERMNO, LPERMCO, conm, tic");
        }

        private static void EnsureFileExists(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"CCM path not found: {path}", path);
        }

        private static int IndexOf(string[] header, string name)
        {
            for (int i = 0; i < header.Length; i++)
                if (string.Equals(header[i], name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        private static string? GetField(string[] row, int idx)
            => (idx >= 0 && idx < row.Length) ? row[idx] : null;

        private static int? ReadIntOrNull(string[] row, int idx)
        {
            var s = GetField(row, idx);
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
        }
    }
}


