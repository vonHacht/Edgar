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

        // New book-to-market variables from your screenshot
        private int _sicIdx, _dlttIdx, _piIdx, _pllIdx, _ncoIdx, _npatIdx, _capr1Idx;
        private int _fdateIdx, _llrciIdx, _llrcrIdx;

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

            LoadIndices();
        }

        public BookToMarketData ReadAllBookToMarket()
        {
            var data = new BookToMarketData();

            using var reader = new StreamReader(_bookToMarketPath);
            using var parser = new CsvParser(reader, _csvConfig);

            if (!parser.Read())
                return data;

            while (parser.Read())
            {
                var row = parser.Record;
                if (row is null || row.Length == 0)
                    continue;

                if (!TryParseDate(GetField(row, _datadateIdx), out var datadate))
                    continue;

                var cik = (GetField(row, _cikIdx) ?? string.Empty).Trim();
                if (cik.Length == 0)
                    continue;

                var year = datadate.Year;

                var item = new BookToMarket
                {
                    Date = datadate,

                    Gvkey = GetField(row, _gvkeyIdx) ?? string.Empty,

                    CommonEquity = ReadDoubleOrNaN(row, _ceqIdx),
                    ShareholdersEquity = ReadDoubleOrNaN(row, _seqIdx),

                    PrefferedStockRedemptionValue = ReadDoubleOrNaN(row, _pstkrvIdx),
                    PrefferedStockLiquidatingValue = ReadDoubleOrNaN(row, _pstklIdx),
                    PrefferedStock = ReadDoubleOrNaN(row, _pstkIdx),

                    DeferredTaxes = ReadDoubleOrNaN(row, _txditcIdx),

                    TotalAssets = ReadDoubleOrNaN(row, _atIdx),
                    TotalLiabilities = ReadDoubleOrNaN(row, _ltIdx),

                    MarketCap = ReadDoubleOrNaN(row, _mkvaltIdx),
                    SpecialItems = ReadDoubleOrNaN(row, _spiIdx),
                    NetIncome = ReadDoubleOrNaN(row, _niIdx),

                    // Added from screenshot
                    Sic = ReadIntOrZero(row, _sicIdx),
                    LongTermDebt = ReadDoubleOrNaN(row, _dlttIdx),
                    PretaxIncome = ReadDoubleOrNaN(row, _piIdx),
                    LoanLossProvision = ReadDoubleOrNaN(row, _pllIdx),
                    NetChargeOffs = ReadDoubleOrNaN(row, _ncoIdx),
                    NonPerformingAssets = ReadDoubleOrNaN(row, _npatIdx),
                    Tier1CapitalRatio = ReadDoubleOrNaN(row, _capr1Idx),
                    FinalDate = ReadDateOrNull(row, _fdateIdx),
                    LoanLossReservesI = ReadDoubleOrNaN(row, _llrciIdx),
                    LoanLossReservesR = ReadDoubleOrNaN(row, _llrcrIdx)
                };

                data.Add(year, cik, item);
            }

            data.ComputeForwardLossProvision();
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

            _mkvaltIdx = IndexOf(header, "mkvalt");
            _spiIdx = IndexOf(header, "spi");
            _niIdx = IndexOf(header, "ni");

            // Added from screenshot
            _sicIdx = IndexOf(header, "sic");
            _dlttIdx = IndexOf(header, "dltt");
            _piIdx = IndexOf(header, "pi");
            _pllIdx = IndexOf(header, "pll");
            _ncoIdx = IndexOf(header, "nco");
            _npatIdx = IndexOf(header, "npat");
            _capr1Idx = IndexOf(header, "capr1");
            _fdateIdx = IndexOf(header, "fdate");
            _llrciIdx = IndexOf(header, "llrci");
            _llrcrIdx = IndexOf(header, "llrcr");

            if (_cikIdx < 0 || _datadateIdx < 0)
                throw new InvalidDataException("BookToMarket CSV is missing required columns: cik, datadate.");
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

        private bool TryParseDate(string? s, out DateTime dt)
            => DateTime.TryParseExact(
                s,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out dt);

        private DateTime? ReadDateOrNull(string[] row, int idx)
        {
            var s = GetField(row, idx);
            return TryParseDate(s, out var dt) ? dt : null;
        }

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

        private int ReadIntOrZero(string[] row, int idx)
        {
            var s = GetField(row, idx);
            return int.TryParse(
                s,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var v)
                ? v
                : 0;
        }
    }
}

