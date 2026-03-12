using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using Edgar.Config;
using Edgar.Models;

namespace Edgar.Import
{
    public sealed class BookToMarketImporter
    {
        private readonly string _bookToMarketPath;
        private readonly CsvConfiguration _csvConfig;

        // Column indices
        private int _lpermnoIdx, _cikIdx, _linkdtIdx, _linkenddtIdx, _gvkeyIdx, _datadateIdx, _fyearIdx;
        private int _ceqIdx, _seqIdx, _txditcIdx, _pstkrvIdx, _pstklIdx, _pstkIdx, _atIdx, _ltIdx;
        private int _mkvaltIdx, _spiIdx, _niIdx;

        public BookToMarketImporter(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _bookToMarketPath = settings.BookToMarketFilename
                ?? throw new ArgumentException("BookToMarketFilename is null.", nameof(settings));

            EnsureFileExists(_bookToMarketPath);

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

            LoadIndices(); // load once, up-front
        }

        public BookToMarketData ReadAllBookToMarket()
        {
            var data = new BookToMarketData();

            using var reader = new StreamReader(_bookToMarketPath);
            using var parser = new CsvParser(reader, _csvConfig);

            // Header already validated in LoadIndices(), but still need to consume it here
            if (!parser.Read())
                return data;

            while (parser.Read())
            {
                var row = parser.Record;
                if (row is null || row.Length == 0)
                    continue;

                // datadate
                if (!TryParseDate(GetField(row, _datadateIdx), out var datadate))
                    continue;

                var cik = (GetField(row, _cikIdx) ?? string.Empty).Trim();
                if (cik.Length == 0)
                    continue;

                var year = datadate.Year; // or parse fyear if you prefer

                var item = new BookToMarket
                {
                    Date = datadate,
                    CommonEquity = ReadDoubleOrNaN(row, _ceqIdx),
                    ShareholdersEquity = ReadDoubleOrNaN(row, _seqIdx),

                    PrefferedStockRedemptionValue = ReadDoubleOrNaN(row, _pstkrvIdx),
                    PrefferedStockLiquidatingValue = ReadDoubleOrNaN(row, _pstklIdx),
                    PrefferedStock = ReadDoubleOrNaN(row, _pstkIdx),

                    DeferredTaxes = ReadDoubleOrNaN(row, _txditcIdx),

                    TotalAssets = ReadDoubleOrNaN(row, _atIdx),
                    TotalLiabilities = ReadDoubleOrNaN(row, _ltIdx),

                    Gvkey = GetField(row, _gvkeyIdx) ?? string.Empty,
                    MarketCap = ReadDoubleOrNaN(row, _mkvaltIdx),
                    SpecialItems = ReadDoubleOrNaN(row, _spiIdx),
                    NetIncome = ReadDoubleOrNaN(row, _niIdx)
                };

                data.Add(year, cik, item);
            }

            return data;
        }

        private void EnsureFileExists(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"BookToMarket path not found: {path}", path);
        }

        private void LoadIndices()
        {
            using var reader = new StreamReader(_bookToMarketPath);
            using var parser = new CsvParser(reader, _csvConfig);

            if (!parser.Read())
                throw new InvalidDataException("BookToMarket CSV appears empty (no header).");

            var header = parser.Record ?? Array.Empty<string>();

            _lpermnoIdx = IndexOf(header, "LPERMNO");
            _cikIdx = IndexOf(header, "cik");
            _linkdtIdx = IndexOf(header, "LINKDT");
            _linkenddtIdx = IndexOf(header, "LINKENDDT");
            _gvkeyIdx = IndexOf(header, "GVKEY");
            _datadateIdx = IndexOf(header, "datadate");
            _fyearIdx = IndexOf(header, "fyear");

            _ceqIdx = IndexOf(header, "ceq");
            _seqIdx = IndexOf(header, "seq");
            _txditcIdx = IndexOf(header, "txditc");
            _pstkrvIdx = IndexOf(header, "pstkrv");
            _pstklIdx = IndexOf(header, "pstkl");
            _pstkIdx = IndexOf(header, "pstk");
            _atIdx = IndexOf(header, "at");
            _ltIdx = IndexOf(header, "lt");

            _gvkeyIdx = IndexOf(header, "GVKEY");
            _mkvaltIdx = IndexOf(header, "mkvalt");
            _spiIdx = IndexOf(header, "spi");

            _niIdx = IndexOf(header, "ni");

            // Enforce only what you truly need for ReadAllBookToMarket
            if (_cikIdx < 0 || _datadateIdx < 0)
                throw new InvalidDataException("BookToMarket CSV is missing required columns: cik, datadate.");

            // Enforce numeric columns only if you want hard failures instead of NaN defaults:
            // if (_seqIdx < 0 && _ceqIdx < 0 && (_atIdx < 0 || _ltIdx < 0))
            //     throw new InvalidDataException("BookToMarket CSV missing equity inputs (seq/ceq or at/lt).");
        }

        private static int IndexOf(string[] header, string name)
        {
            for (int i = 0; i < header.Length; i++)
                if (string.Equals(header[i], name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        private bool TryParseDate(string? s, out DateTime dt)
            => DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);

        private static string? GetField(string[] row, int idx)
            => (idx >= 0 && idx < row.Length) ? row[idx] : null;

        private double ReadDoubleOrNaN(string[] row, int idx)
        {
            var s = GetField(row, idx);
            return double.TryParse(
                s,
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out var v)
                ? v
                : double.NaN;
        }
    }
}


