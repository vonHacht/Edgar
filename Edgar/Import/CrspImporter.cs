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
        private static readonly ILogger<Program> _logger = EdgarLogger.CreateLogger<Program>();

        private readonly string _crspPath;
        private readonly CsvConfiguration _csvConfig;

        public CrspImporter(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _crspPath = Path.Combine(settings.CompaniesDir, Filepaths.returnsCRSPFileName);

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

        public List<FirmTradingDay> ReadByPermno(int permno, string year)
        {
            if (string.IsNullOrWhiteSpace(year) || year.Length != 4)
                return [];

            if (!File.Exists(_crspPath))
                throw new FileNotFoundException($"CRSP file not found: {_crspPath}");

            var targetYear = year.AsSpan(); // compare yyyy via span

            using var reader = new StreamReader(_crspPath);
            using var parser = new CsvParser(reader, _csvConfig);

            // Read header
            if (!parser.Read())
                return [];

            var header = parser.Record ?? Array.Empty<string>();

            int permnoIdx = IndexOf(header, "PERMNO");
            int dateIdx = IndexOf(header, "date");      // adjust if your column is named differently
            int prcIdx = IndexOf(header, "PRC");
            int openIdx = IndexOf(header, "OPENPRC");
            int bidIdx = IndexOf(header, "BID");
            int askIdx = IndexOf(header, "ASK");
            int bidloIdx = IndexOf(header, "BIDLO");
            int askhiIdx = IndexOf(header, "ASKHI");
            int volIdx = IndexOf(header, "VOL");
            int numtrdIdx = IndexOf(header, "NUMTRD");
            int shroutIdx = IndexOf(header, "SHROUT");
            int retIdx = IndexOf(header, "RET");
            int retxIdx = IndexOf(header, "RETX");
            int dlstcdIdx = IndexOf(header, "DLSTCD");
            int dlretIdx = IndexOf(header, "DLRET");
            int dlretxIdx = IndexOf(header, "DLRETX");
            int dlprcIdx = IndexOf(header, "DLPRC");

            if (permnoIdx < 0 || dateIdx < 0)
                throw new InvalidDataException("CRSP CSV is missing required columns: PERMNO, date.");

            var results = new List<FirmTradingDay>();

            while (parser.Read())
            {
                var row = parser.Record;
                if (row is null || row.Length == 0)
                    continue;

                // Filter 1: PERMNO (cheap int parse)
                if (!TryParseInt(row[permnoIdx], out var rowPermno) || rowPermno != permno)
                    continue;

                // Filter 2: year (avoid DateTime/ToString; compare first 4 chars of yyyy-MM-dd)
                var dateText = row[dateIdx];
                if (string.IsNullOrEmpty(dateText) || dateText.Length < 4 ||
                    !dateText.AsSpan(0, 4).SequenceEqual(targetYear))
                    continue;

                // Parse date only for matches
                if (!TryParseDate(dateText, out var dt))
                    continue;

                var prc = TryParseDouble(row, prcIdx);

                results.Add(new FirmTradingDay
                {
                    Date = DateOnly.FromDateTime(dt),

                    ClosePrcRaw = prc.HasValue ? (decimal)prc.Value : null,
                    Close = prc.HasValue ? (decimal)Math.Abs(prc.Value) : null,
                    CloseIsMidpoint = prc.HasValue && prc.Value < 0,

                    Open = ToNullableDecimal(TryParseDouble(row, openIdx)),
                    Bid = ToNullableDecimal(TryParseDouble(row, bidIdx)),
                    Ask = ToNullableDecimal(TryParseDouble(row, askIdx)),
                    BidLow = ToNullableDecimal(TryParseDouble(row, bidloIdx)),
                    AskHigh = ToNullableDecimal(TryParseDouble(row, askhiIdx)),

                    Volume = TryParseLongNullable(row, volIdx),
                    NumberOfTrades = TryParseIntNullable(row, numtrdIdx),

                    SharesOut = TryParseLongNullable(row, shroutIdx),

                    Ret = TryParseDoubleNullable(row, retIdx),
                    RetExDiv = TryParseDoubleNullable(row, retxIdx),

                    DelistCode = TryParseIntNullable(row, dlstcdIdx),
                    DelistRet = TryParseDoubleNullable(row, dlretIdx),
                    DelistRetExDiv = TryParseDoubleNullable(row, dlretxIdx),
                    DelistPrice = TryParseDoubleNullable(row, dlprcIdx)
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

        private static bool TryParseInt(string? s, out int v)
            => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);

        private static int? TryParseIntNullable(string[] row, int idx)
            => (idx >= 0 && idx < row.Length && int.TryParse(row[idx], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                ? v : null;

        private static long? TryParseLongNullable(string[] row, int idx)
            => (idx >= 0 && idx < row.Length && long.TryParse(row[idx], NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                ? v : null;

        private static double? TryParseDoubleNullable(string[] row, int idx)
            => (idx >= 0 && idx < row.Length && double.TryParse(row[idx], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v))
                ? v : null;

        private static double? TryParseDouble(string[] row, int idx)
            => TryParseDoubleNullable(row, idx);

        private static bool TryParseDate(string s, out DateTime dt)
            => DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);

        private static decimal? ToNullableDecimal(double? v)
            => v.HasValue ? (decimal)v.Value : null;
    }
}

