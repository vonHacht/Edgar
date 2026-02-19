using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using Edgar.Config;
using Edgar.Logging;
using Edgar.Models;

using Microsoft.Extensions.Logging;

namespace Edgar.Companies
{
    public sealed class CrspImporter
    {
        private static readonly ILogger<CrspImporter> _logger =
            EdgarLogger.CreateLogger<CrspImporter>();

        private readonly string _crspPath;
        private readonly CsvConfiguration _csvConfig;

        // Cache header indices once
        private volatile bool _indicesLoaded;
        private readonly object _indicesLock = new();

        private int _permnoIdx, _dateIdx, _prcIdx, _openIdx, _bidIdx, _askIdx, _bidloIdx, _askhiIdx;
        private int _volIdx, _numtrdIdx, _shroutIdx, _retIdx, _retxIdx, _dlstcdIdx, _dlretIdx, _dlretxIdx, _dlprcIdx;

        public CrspImporter(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _crspPath = settings.CrspFilename;

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
        }

        public HashSet<int> ReadPermnoFromCrsp()
        {
            EnsureFileExists();
            EnsureIndicesLoaded();

            using var reader = new StreamReader(_crspPath);
            using var parser = new CsvParser(reader, _csvConfig);

            // header
            if (!parser.Read())
                return [];

            var permnos = new HashSet<int>();

            while (parser.Read())
            {
                var row = parser.Record;
                if (row is null || row.Length == 0)
                    continue;

                if (!TryParseInt(GetField(row, _permnoIdx), out var permno))
                    continue;

                permnos.Add(permno);
            }

            return permnos;
        }

        // ✅ Best method for your pipeline: read once per YEAR and group by permno
        public Dictionary<int, List<FirmTradingDay>> ReadYearGroupedByPermno(string year)
        {
            if (string.IsNullOrWhiteSpace(year) || year.Length != 4)
                return new();

            EnsureFileExists();
            EnsureIndicesLoaded();

            var targetYear = year.AsSpan();
            var dict = new Dictionary<int, List<FirmTradingDay>>(capacity: 8192);

            using var reader = new StreamReader(_crspPath);
            using var parser = new CsvParser(reader, _csvConfig);

            // header
            if (!parser.Read())
                return dict;

            while (parser.Read())
            {
                var row = parser.Record;
                if (row is null || row.Length == 0)
                    continue;

                // date filter first (cheap string check)
                var dateText = GetField(row, _dateIdx);
                if (string.IsNullOrEmpty(dateText) || dateText.Length < 4 ||
                    !dateText.AsSpan(0, 4).SequenceEqual(targetYear))
                    continue;

                // permno parse
                if (!TryParseInt(GetField(row, _permnoIdx), out var permno))
                    continue;

                if (!TryParseDate(dateText, out var dt))
                    continue;

                var tradingDay = ParseTradingDay(row, dt);

                if (!dict.TryGetValue(permno, out var list))
                {
                    list = new List<FirmTradingDay>(capacity: 260);
                    dict[permno] = list;
                }

                list.Add(tradingDay);
            }

            // Optional: ensure each list is sorted by date
            foreach (var kvp in dict)
                kvp.Value.Sort(static (a, b) => a.Date.CompareTo(b.Date));

            return dict;
        }

        // Convenience method (still one full-file scan). Keep if you need it.
        public List<FirmTradingDay> ReadByPermno(int permno, string year)
        {
            if (string.IsNullOrWhiteSpace(year) || year.Length != 4)
                return [];

            EnsureFileExists();
            EnsureIndicesLoaded();

            var targetYear = year.AsSpan();
            var results = new List<FirmTradingDay>();

            using var reader = new StreamReader(_crspPath);
            using var parser = new CsvParser(reader, _csvConfig);

            // header
            if (!parser.Read())
                return [];

            while (parser.Read())
            {
                var row = parser.Record;
                if (row is null || row.Length == 0)
                    continue;

                // permno
                if (!TryParseInt(GetField(row, _permnoIdx), out var rowPermno) || rowPermno != permno)
                    continue;

                // year
                var dateText = GetField(row, _dateIdx);
                if (string.IsNullOrEmpty(dateText) || dateText.Length < 4 ||
                    !dateText.AsSpan(0, 4).SequenceEqual(targetYear))
                    continue;

                if (!TryParseDate(dateText, out var dt))
                    continue;

                results.Add(ParseTradingDay(row, dt));
            }

            results.Sort(static (a, b) => a.Date.CompareTo(b.Date));
            return results;
        }

        // -------------------- internals --------------------

        private void EnsureFileExists()
        {
            if (!File.Exists(_crspPath))
                throw new FileNotFoundException($"CRSP file not found: {_crspPath}");
        }

        private void EnsureIndicesLoaded()
        {
            if (_indicesLoaded)
                return;

            lock (_indicesLock)
            {
                if (_indicesLoaded)
                    return;

                using var reader = new StreamReader(_crspPath);
                using var parser = new CsvParser(reader, _csvConfig);

                if (!parser.Read())
                    throw new InvalidDataException("CRSP CSV appears empty (no header).");

                var header = parser.Record ?? Array.Empty<string>();

                _permnoIdx = IndexOf(header, "PERMNO");
                _dateIdx = IndexOf(header, "date");

                _prcIdx = IndexOf(header, "PRC");
                _openIdx = IndexOf(header, "OPENPRC");
                _bidIdx = IndexOf(header, "BID");
                _askIdx = IndexOf(header, "ASK");
                _bidloIdx = IndexOf(header, "BIDLO");
                _askhiIdx = IndexOf(header, "ASKHI");
                _volIdx = IndexOf(header, "VOL");
                _numtrdIdx = IndexOf(header, "NUMTRD");
                _shroutIdx = IndexOf(header, "SHROUT");
                _retIdx = IndexOf(header, "RET");
                _retxIdx = IndexOf(header, "RETX");
                _dlstcdIdx = IndexOf(header, "DLSTCD");
                _dlretIdx = IndexOf(header, "DLRET");
                _dlretxIdx = IndexOf(header, "DLRETX");
                _dlprcIdx = IndexOf(header, "DLPRC");

                if (_permnoIdx < 0 || _dateIdx < 0)
                    throw new InvalidDataException("CRSP CSV is missing required columns: PERMNO, date.");

                _indicesLoaded = true;
            }
        }

        private FirmTradingDay ParseTradingDay(string[] row, DateTime dt)
        {
            var prc = TryParseDoubleNullable(row, _prcIdx);

            return new FirmTradingDay
            {
                Date = dt,

                ClosePrcRaw = prc.HasValue ? (decimal)prc.Value : null,
                Close = prc.HasValue ? (decimal)Math.Abs(prc.Value) : null,
                CloseIsMidpoint = prc.HasValue && prc.Value < 0,

                Open = ToNullableDecimal(TryParseDoubleNullable(row, _openIdx)),
                Bid = ToNullableDecimal(TryParseDoubleNullable(row, _bidIdx)),
                Ask = ToNullableDecimal(TryParseDoubleNullable(row, _askIdx)),
                BidLow = ToNullableDecimal(TryParseDoubleNullable(row, _bidloIdx)),
                AskHigh = ToNullableDecimal(TryParseDoubleNullable(row, _askhiIdx)),

                Volume = TryParseLongNullable(row, _volIdx),
                NumberOfTrades = TryParseIntNullable(row, _numtrdIdx),

                SharesOut = TryParseLongNullable(row, _shroutIdx),

                Ret = TryParseDoubleNullable(row, _retIdx),
                RetExDiv = TryParseDoubleNullable(row, _retxIdx),

                DelistCode = TryParseIntNullable(row, _dlstcdIdx),
                DelistRet = TryParseDoubleNullable(row, _dlretIdx),
                DelistRetExDiv = TryParseDoubleNullable(row, _dlretxIdx),
                DelistPrice = TryParseDoubleNullable(row, _dlprcIdx)
            };
        }

        private static string? GetField(string[] row, int idx)
            => (idx >= 0 && idx < row.Length) ? row[idx] : null;

        private static int IndexOf(string[] header, string name)
        {
            for (int i = 0; i < header.Length; i++)
                if (string.Equals(header[i], name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        private static bool TryParseInt(string? s, out int v)
            => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);

        private static int? TryParseIntNullable(string[] row, int idx)
            => int.TryParse(GetField(row, idx), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

        private static long? TryParseLongNullable(string[] row, int idx)
            => long.TryParse(GetField(row, idx), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

        private static double? TryParseDoubleNullable(string[] row, int idx)
            => double.TryParse(GetField(row, idx), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v) ? v : null;

        private static bool TryParseDate(string? s, out DateTime dt)
            => DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);

        private static decimal? ToNullableDecimal(double? v)
            => v.HasValue ? (decimal)v.Value : null;
    }
}

