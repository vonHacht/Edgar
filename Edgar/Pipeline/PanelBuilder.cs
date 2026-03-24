using System.Diagnostics;
using System.Globalization;

using Edgar.Companies;
using Edgar.Config;
using Edgar.Edgar;
using Edgar.Filter;
using Edgar.Import;
using Edgar.Logging;
using Edgar.Models;
using Edgar.Parsing;
using Edgar.TextMeasures;

using Microsoft.Extensions.Logging;

namespace Edgar.Pipeline
{
    public sealed class PanelBuilder : IDisposable
    {
        private readonly ILogger<PanelBuilder> _logger;
        private readonly AppSettings _appSettings;

        private readonly BookToMarketData _bookToMarketData;
        private readonly CikPermnoMap _ccmData;
        private readonly FirmTradingDays _firmTradingDays;

        private readonly EdgarClient _edgarClient;
        private readonly FilingIndexService _indexService;
        private readonly FilingDownloader _downloader;
        private readonly ItemSectionExtractor _extractor = new ItemSectionExtractor();

        private readonly LmDictionaryScorer _dictionaryScorer;

        private readonly Database.MongoDB _db;

        private static readonly int[] Years =
       {
            //2009,
            2010, 2011, 2012, 2013, 2014,
            2015, 2016, 2017, 2018, 2019,
            2020, 2021, 2022, 2023, 2024
        };

        public PanelBuilder(AppSettings appSettings)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _logger = EdgarLogger.CreateLogger<PanelBuilder>(appSettings);

            _bookToMarketData = new BookToMarketImporter(_appSettings).ReadAllBookToMarket();
            _ccmData = new CcmImporter(_appSettings).ReadAllYearsUniqueCcms();
            _firmTradingDays = new CrspImporter(_appSettings).ReadAllCrsp();

            _edgarClient = new EdgarClient(_appSettings);
            _indexService = new FilingIndexService(_edgarClient);
            _downloader = new FilingDownloader(_edgarClient, _appSettings);

            _dictionaryScorer = new LmDictionaryScorer(_appSettings);

            _db = new Database.MongoDB(_appSettings.DefaultLocalHost, _appSettings.DefaultEdgarDbName);
        }

        public async Task RunAsync(CancellationToken ct = default)
        {
            foreach (var year in Years)
            {
                ct.ThrowIfCancellationRequested();
                await ProcessFilingForYearAsync(year, ct);
            }
        }

        private async Task ProcessFilingForYearAsync(int year, CancellationToken ct = default)
        {
            _logger.LogInformation("----- EDGAR PROCESS YEAR {Year} -----", year);

            var filings = await _indexService.Get10KFilingsForYearAsync(year);
            _logger.LogInformation("Found {Count} filings for {Year} in Edgar database", filings.Count, year);

            var sw = Stopwatch.StartNew();
            var yearKey = year.ToString(CultureInfo.InvariantCulture);

            for (var i = 0; i < filings.Count; i++)
            {
                var filing = filings[i];

                var permnos = GetPermnosOrWarn(filing);
                var prevTradingDays = GetFirstTradingDaysOrWarn(year - 1, filing, permnos);
                var tradingDays = GetFirstTradingDaysOrWarn(year, filing, permnos);
                var bookToMarket = GetBookToMarketsOrWarn(year, filing, filing.DateFiled);

                using var scope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["CIK"] = filing.CIK,
                    ["Company"] = filing.CompanyName,
                    ["Year"] = year,
                    ["FilingIndex"] = i + 1,
                    ["FilingTotal"] = filings.Count
                });

                try
                {
                    LogCik(filing.CIK, "Filing {Index}/{Total} for Year {Year} | Starting", i + 1, filings.Count, year);

                    if (permnos.Count == 0 || tradingDays.Count == 0 || bookToMarket == null)
                    {
                        LogCik(filing.CIK, "Skipping filing due to missing required data (CCM/CRSP/BTM).");
                        continue;
                    }

                    var filter60Result = FilterFunctions.Filter60DaysBeforeAfter(permnos.First(), year, _firmTradingDays);

                    if (filter60Result != "")
                    {
                        LogCik(filing.CIK, filter60Result);
                        continue;
                    }

                    ExtractedSections extractedSections = await ProcessFilingAsync(yearKey, filing, ct);

                    var filterProcess = FilterProcess.Process(filing, extractedSections, tradingDays, bookToMarket);

                    if (filterProcess != "")
                    {
                        LogCik(filing.CIK, filterProcess);
                        continue;
                    }

                    prevTradingDays.AddRange(tradingDays);

                    FirmYearRegressionPanelDocument firmYearRegressionPanelDocument = new FirmYearRegressionPanelDocument
                    {
                        Cik = filing.CIK,
                        Permno = permnos.First(),
                        Gvkey = bookToMarket.Gvkey,
                        FilingDate = filing.DateFiled,
                        Sic = bookToMarket.Sic,
                        ScoresItem1A = _dictionaryScorer.Score(extractedSections.Item1AText),
                        ScoresItem7 = _dictionaryScorer.Score(extractedSections.Item7Text),

                        PriorReturn = Utilities.TradingDaysCalculations.PriorReturn(prevTradingDays, filing.DateFiled),
                        Volatility = Utilities.TradingDaysCalculations.RealizedVolatility(prevTradingDays, filing.DateFiled),
                        RealizedVariance = Utilities.TradingDaysCalculations.RealizedVariance(prevTradingDays, filing.DateFiled),
                        Turnover = Utilities.TradingDaysCalculations.CumulativeTurnover(prevTradingDays, filing.DateFiled),
                        TurnoverAvg = Utilities.TradingDaysCalculations.AverageTurnover(prevTradingDays, filing.DateFiled),
                        FilingDayReturn = Utilities.TradingDaysCalculations.FilingDayReturn(prevTradingDays, filing.DateFiled),

                        LossProvisionsT1 = bookToMarket.LossProvision,
                        LossProvisionsRawT1 = bookToMarket.LossProvisionRaw,

                        CommonEquity = bookToMarket.CommonEquity,
                        SpecialItems = bookToMarket.SpecialItems,
                        BookEquity = bookToMarket.BookEquity,
                        BookToMarket = bookToMarket.BM,
                        Leverage = bookToMarket.Leverage,
                        TotalAssets = bookToMarket.TotalAssets,
                        LoanLossProvisions = bookToMarket.LoanLossProvision,
                        NetIncome = bookToMarket.NetIncome,
                        Size = bookToMarket.Size,
                        MarketEquity = bookToMarket.MarketCap,
                        Tier1CapitalRatio = bookToMarket.Tier1CapitalRatio,
                        LoanLossReservesR = bookToMarket.LoanLossReservesR,
                        LoanLossReservesI = bookToMarket.LoanLossReservesI,
                        NonPerformingAssets = bookToMarket.NonPerformingAssets,
                        NetChargeOffs = bookToMarket.NetChargeOffs,
                        PreTaxIncome = bookToMarket.PretaxIncome,
                        LongTermDebt = bookToMarket.PretaxIncome,

                        // from Compustat BANK
                        TotalLoansNet = 0,
                        LoanLossReserves = 0,

                        TextModelVersion = "",
                        UpdatedAt = DateTime.UtcNow
                    };


                    await _db.SendFirmYearRegressionPanelDocument(year, firmYearRegressionPanelDocument);

                    LogCik(filing.CIK, "Filing {Index}/{Total} for Year {Year} | Finished", i + 1, filings.Count, year);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogCikError(ex, filing.CIK, "Filing {Index}/{Total} for Year {Year} | ERROR", i + 1, filings.Count, year);
                }
            }

            sw.Stop();
            _logger.LogInformation("Finished with Edgar database in {Seconds:F1}s", sw.Elapsed.TotalSeconds);
        }

        private List<int> GetPermnosOrWarn(Filing filing)
        {
            var permnos = _ccmData.GetPermnos(filing.CIK)?.ToList() ?? new List<int>();

            if (permnos.Count == 0)
            {
                LogCik(
                    filing.CIK,
                    "Missing permno mapping in CCM for Company {Company}.",
                    filing.CompanyName);
            }

            return permnos;
        }

        private List<FirmTradingDay> GetFirstTradingDaysOrWarn(int year, Filing filing, List<int> permnos)
        {
            if (permnos.Count == 0)
                return new List<FirmTradingDay>();

            var tradingDays = permnos
                .Select(p => _firmTradingDays.GetDays(year, p))
                .FirstOrDefault(days => days != null && days.Any())
                ?.ToList() ?? new List<FirmTradingDay>();

            if (tradingDays.Count == 0)
            {
                LogCik(
                    filing.CIK,
                    "Missing CRSP trading days for Company {Company} in {Year}.",
                    filing.CompanyName, year);
            }

            return tradingDays;
        }

        private BookToMarket? GetBookToMarketsOrWarn(int year, Filing filing, DateTime filingDate)
        {
            var bookToMarkets = _bookToMarketData.Get(year, filing.CIK, filingDate);

            if (bookToMarkets == null)
            {
                LogCik(
                    filing.CIK,
                    "Missing Book-to-Market data for Company {Company} in {Year}.",
                    filing.CompanyName, year);
            }

            return bookToMarkets;
        }

        private async Task<ExtractedSections> ProcessFilingAsync(string yearKey, Filing filing, CancellationToken ct)
        {
            LogCik(filing.CIK, "Downloading filing HTML (cached if available)");

            // Only first filing per firm per calendar year
            // Minimum 180 days between two filings for the same firm
            // 10-K must contain > 2,000 words (fullfilled other way)
            var htmlPath = await _downloader.GetOrDownloadPrimaryDocAsync(yearKey, filing);

            LogCik(filing.CIK, "Reading HTML from {HtmlPath}", htmlPath);
            var html = await File.ReadAllTextAsync(htmlPath);

            LogCik(filing.CIK, "Cleaning + extracting sections");
            var cleanedText = HtmlCleaner.HtmlToText(html);

            return _extractor.Extract(cleanedText, true);
        }

        // ---------- Logging helpers (CIK first, always) ----------

        private void LogCik(string cik, string messageTemplate, params object[] args)
            => _logger.LogInformation($"CIK {{CIK}} | {messageTemplate}", Prepend(cik, args));

        private void LogCikWarning(string cik, string messageTemplate, params object[] args)
            => _logger.LogWarning($"CIK {{CIK}} | {messageTemplate}", Prepend(cik, args));

        private void LogCikError(Exception ex, string cik, string messageTemplate, params object[] args)
            => _logger.LogError(ex, $"CIK {{CIK}} | {messageTemplate}", Prepend(cik, args));

        private static object[] Prepend(string cik, object[] args)
        {
            var all = new object[args.Length + 1];
            all[0] = cik;
            Array.Copy(args, 0, all, 1, args.Length);
            return all;
        }

        public void Dispose()
        {
            _edgarClient.Dispose();
        }
    }
}
