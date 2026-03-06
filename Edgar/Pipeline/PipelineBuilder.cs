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

using Microsoft.Extensions.Logging;

namespace Edgar.Pipeline
{
    public class PipelineBuilder
    {
        private readonly ILogger<Program> _logger;
        private readonly AppSettings _appSettings;

        private BookToMarketData _bookToMarketData = null!;
        private CikPermnoMap _ccmData = null!;
        private CrspData _crspData = null!;

        private EdgarClient _edgarClient = null!;
        private FilingIndexService _indexService = null!;
        private FilingDownloader _downloader = null!;
        private readonly ItemSectionExtractor _extractor = new ItemSectionExtractor();

        public PipelineBuilder(AppSettings appSettings)
        {
            _appSettings = appSettings;
            _logger = EdgarLogger.CreateLogger<Program>(appSettings);
        }

        public void Run2009To2024()
        {
            LoadDataToMemory();
            LoadEdgarClient();

            for (var year = 2009; year <= 2024; year++)
            {
                _ = DownloadFilingsForYear(year).GetAwaiter().GetResult();
            }
        }

        public void LoadDataToMemory()
        {
            _logger.LogInformation("----- LOADING CRSP, CCM & BOOKTOMARKET INTO MEMORY -----");

            var sw = Stopwatch.StartNew();

            _bookToMarketData = new BookToMarketImporter(_appSettings).ReadAllBookToMarket();
            _ccmData = new CcmImporter(_appSettings).ReadAllYearsUniqueCcms();
            _crspData = new CrspImporter(_appSettings).ReadAllCrsp();

            sw.Stop();
            _logger.LogInformation("----- FINISHED IN {Seconds:F1}s -----", sw.Elapsed.TotalSeconds);
        }

        public void LoadEdgarClient()
        {
            _edgarClient = new EdgarClient(_appSettings);
            _indexService = new FilingIndexService(_edgarClient);
            _downloader = new FilingDownloader(_edgarClient, _appSettings);
        }

        public async Task<List<string>> DownloadFilingsForYear(int year)
        {
            _logger.LogInformation("----- EDGAR PROCESS YEAR {Year} -----", year);

            var filings = await _indexService.Get10KFilingsForYearAsync(year);
            _logger.LogInformation("Found {Count} filings for {Year} in Edgar database", filings.Count, year);

            // Keep if later stages add outputs; otherwise remove.
            var savedPaths = new List<string>(capacity: filings.Count);

            var sw = Stopwatch.StartNew();
            var yearKey = year.ToString(CultureInfo.InvariantCulture);

            for (var i = 0; i < filings.Count; i++)
            {
                var filing = filings[i];

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

                    // --- Load dependent datasets for this filing
                    var permnos = GetPermnosOrWarn(filing);
                    var tradingDays = GetFirstTradingDaysOrWarn(year, filing, permnos);
                    var bookToMarkets = GetBookToMarketsOrWarn(year, filing);

                    // If required to proceed, skip when missing.
                    if (permnos.Count == 0 || tradingDays.Count == 0 || bookToMarkets.Count == 0)
                    {
                        LogCik(filing.CIK, "Skipping filing due to missing required data (CCM/CRSP/BTM).");
                        continue;
                    }

                    if(Filter.FilterFunctions.Filter60DaysBeforeAfter(permnos.First(), year, _crspData))
                    {
                        LogCik(filing.CIK, "Skipping filing due to failing 60 days before/after filter.");
                        continue;
                    }

                    // --- Download + extract sections
                    var extractedSections = await ProcessFilingAsync(yearKey, filing);

                    // TODO: Do something with extractedSections, tradingDays, bookToMarkets, etc.
                    // If you save a file, add it:
                    // savedPaths.Add(outputPath);
                    FilterProcess.Passed(filing, extractedSections, tradingDays, bookToMarkets);

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

            return savedPaths;
        }

        private List<int> GetPermnosOrWarn(Filing filing)
        {
            var permnos = _ccmData.GetPermnos(filing.CIK)?.ToList() ?? new List<int>();

            if (permnos.Count == 0)
            {
                LogCik(
                    filing.CIK,
                    "Missing permno mapping in CCM for Company {Company} ({CIK}).",
                    filing.CompanyName, filing.CIK);
            }

            return permnos;
        }

        private List<FirmTradingDay> GetFirstTradingDaysOrWarn(int year, Filing filing, List<int> permnos)
        {
            if (permnos.Count == 0)
                return new List<FirmTradingDay>();

            var tradingDays = permnos
                .Select(p => _crspData.GetDays(year, p))
                .FirstOrDefault(days => days != null && days.Any())
                ?.ToList() ?? new List<FirmTradingDay>();

            if (tradingDays.Count == 0)
            {
                LogCik(
                    filing.CIK,
                    "Missing CRSP trading days for Company {Company} ({CIK}) in {Year}.",
                    filing.CompanyName, filing.CIK, year);
            }

            return tradingDays;
        }

        private List<BookToMarket> GetBookToMarketsOrWarn(int year, Filing filing)
        {
            var bookToMarkets = _bookToMarketData.Get(year, filing.CIK)?.ToList() ?? new List<BookToMarket>();

            if (bookToMarkets.Count == 0)
            {
                LogCik(
                    filing.CIK,
                    "Missing Book-to-Market data for Company {Company} ({CIK}) in {Year}.",
                    filing.CompanyName, filing.CIK, year);
            }

            return bookToMarkets;
        }

        private async Task<ExtractedSections> ProcessFilingAsync(string yearKey, Filing filing)
        {
            LogCik(filing.CIK, "Downloading filing HTML (cached if available)");
            // Filter here:
            // Only first filing per firm per calendar year
            // Minimum 180 days between two filings for the same firm
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
    }
}
