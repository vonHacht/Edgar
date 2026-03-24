using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using Edgar.Config;
using Edgar.Models;

namespace Edgar.Companies
{
    public sealed class CrspImporter
    {
        private readonly string _crspPath;
        private readonly CsvConfiguration _csvConfig;

        // Column indices
        private int _permnoIdx, _dateIdx, _prcIdx, _openIdx, _bidIdx, _askIdx, _bidloIdx, _askhiIdx, _exchdIdx;
        private int _volIdx, _numtrdIdx, _shroutIdx, _retIdx, _retxIdx, _dlstcdIdx, _dlretIdx, _dlretxIdx, _dlprcIdx;
        private int _shrcdIdx, _vwretdIdx, _vwretxIdx;

        public CrspImporter(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _crspPath = settings.CrspFilename
                ?? throw new ArgumentException("CrspFilename is null.", nameof(settings));

            EnsureFileExists(_crspPath);

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

        public FirmTradingDays ReadAllCrsp()
        {
            var data = new FirmTradingDays();

            using var reader = new StreamReader(_crspPath);
            using var parser = new CsvParser(reader, _csvConfig);

            // consume header
            if (!parser.Read())
                return data;

            while (parser.Read())
            {
                var row = parser.Record;
                if (row is null || row.Length == 0)
                    continue;

                // PERMNO
                if (!TryParseInt(GetField(row, _permnoIdx), out var permno))
                    continue;

                // date
                if (!TryParseDate(GetField(row, _dateIdx), out var dt))
                    continue;

                var year = dt.Year;

                var day = ParseTradingDay(row, dt);
                data.Add(year, permno, day);
            }

            data.SortAll();
            return data;
        }

        // -------------------- internals --------------------

        private void LoadIndices()
        {
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

            _exchdIdx = IndexOf(header, "EXCHCD");

            _shrcdIdx = IndexOf(header, "shrcd");

            _vwretdIdx = IndexOf(header, "vwretd");
            _vwretxIdx = IndexOf(header, "vwretx");

            if (_permnoIdx < 0 || _dateIdx < 0)
                throw new InvalidDataException("CRSP CSV is missing required columns: PERMNO, date.");
        }

        private static void EnsureFileExists(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"CRSP file not found: {path}", path);
        }

        private FirmTradingDay ParseTradingDay(string[] row, DateTime dt)
        {
            var prc = ReadDoubleOrNull(row, _prcIdx);

            return new FirmTradingDay
            {
                Date = dt,

                Permno = ReadIntOrNull(row, _permnoIdx) ?? 0,

                // PRC: negative means midpoint (CRSP convention)
                ClosePrcRaw = prc.HasValue ? (decimal)prc.Value : null,
                Close = prc.HasValue ? (decimal)Math.Abs(prc.Value) : null,
                CloseIsMidpoint = prc.HasValue && prc.Value < 0,

                Open = ToNullableDecimal(ReadDoubleOrNull(row, _openIdx)),
                Bid = ToNullableDecimal(ReadDoubleOrNull(row, _bidIdx)),
                Ask = ToNullableDecimal(ReadDoubleOrNull(row, _askIdx)),
                BidLow = ToNullableDecimal(ReadDoubleOrNull(row, _bidloIdx)),
                AskHigh = ToNullableDecimal(ReadDoubleOrNull(row, _askhiIdx)),

                Volume = ReadLongOrNull(row, _volIdx),
                NumberOfTrades = ReadIntOrNull(row, _numtrdIdx),

                SharesOut = ReadLongOrNull(row, _shroutIdx),

                Ret = ReadDoubleOrNull(row, _retIdx),
                RetExDiv = ReadDoubleOrNull(row, _retxIdx),

                DelistCode = ReadIntOrNull(row, _dlstcdIdx),
                DelistRet = ReadDoubleOrNull(row, _dlretIdx),
                DelistRetExDiv = ReadDoubleOrNull(row, _dlretxIdx),
                DelistPrice = ReadDoubleOrNull(row, _dlprcIdx),

                ExchangeCodes = (ExchangeCodes)(ReadIntOrNull(row, _exchdIdx) ?? 0),

                ShareCode = ReadIntOrNull(row, _shrcdIdx),

                ValueWeightedReturnExcludingDividents = ReadDoubleOrNull(row, _vwretxIdx),
                ValueWeightedReturnIncludingDividends = ReadDoubleOrNull(row, _vwretdIdx)
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

        private static int? ReadIntOrNull(string[] row, int idx)
        {
            var s = GetField(row, idx);
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
        }

        private static long? ReadLongOrNull(string[] row, int idx)
        {
            var s = GetField(row, idx);
            return long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
        }

        private static double? ReadDoubleOrNull(string[] row, int idx)
        {
            var s = GetField(row, idx);
            return double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v) ? v : null;
        }

        private static bool TryParseDate(string? s, out DateTime dt)
            => DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);

        private static decimal? ToNullableDecimal(double? v)
            => v.HasValue ? (decimal)v.Value : null;
    }
}

