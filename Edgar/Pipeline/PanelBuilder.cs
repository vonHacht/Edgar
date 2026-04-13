using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

using Edgar.Companies;
using Edgar.Config;
using Edgar.Edgar;
using Edgar.Filter;
using Edgar.Import;
using Edgar.Logging;
using Edgar.Models;
using Edgar.Parsing;
using Edgar.TextMeasures;
using Edgar.Utilities;

using Microsoft.Extensions.Logging;

namespace Edgar.Pipeline
{
    public sealed class PanelBuilder : IDisposable
    {
        private readonly ILogger _logger;
        private readonly AppSettings _appSettings;
        private readonly BookToMarketData _bookToMarketData;
        private readonly CikPermnoMap _ccmData;
        private readonly FirmTradingDays _firmTradingDays;
        private readonly EdgarClient _edgarClient;
        private readonly FilingIndexService _indexService;
        private readonly FilingDownloader _downloader;
        private readonly ItemSectionExtractor _extractor = new ItemSectionExtractor();
        private readonly LmDictionaryScorer _dictionaryScorer;
        private readonly LlmScorer _llmDictionaryScorer;
        private readonly Database.MongoDB _db;

        private readonly List<YearProcessingStats> _yearStats = new();

        private static readonly int[] Years =
        {
            // 2009,
            2010, 2011, 2012, 2013, 2014,
            2015, 2016, 2017, 2018, 2019,
            2020, 2021, 2022,
            2023,
            // 2024
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
            _llmDictionaryScorer = new LlmScorer();

            _db = new Database.MongoDB(_appSettings.DefaultLocalHost, _appSettings.DefaultEdgarDbName);
        }

        public async Task RunAsync(CancellationToken ct = default, bool skipDb = false, bool extractFromFile = false)
        {
            foreach (var year in Years)
            {
                ct.ThrowIfCancellationRequested();
                var stats = await ProcessFilingForYearAsync(year, ct, skipDb, extractFromFile);
                _yearStats.Add(stats);
            }

            await ExportStatsAsync(ct);
        }

        private async Task<YearProcessingStats> ProcessFilingForYearAsync(
            int year,
            CancellationToken ct = default,
            bool skipDb = false,
            bool extractFromFile = false)
        {
            _logger.LogInformation("----- EDGAR PROCESS YEAR {Year} -----", year);

            var filings = await _indexService.Get10KFilingsForYearAsync(year);
            _logger.LogInformation("Found {Count} filings for {Year} in Edgar database", filings.Count, year);

            var sw = Stopwatch.StartNew();
            var yearKey = year.ToString(CultureInfo.InvariantCulture);
            var stats = new YearProcessingStats(year, filings.Count);

            for (var i = 0; i < filings.Count; i++)
            {
                var filing = filings[i];

                var permnos = GetPermnosOrWarn(filing);
                var prevTradingDays = GetFirstTradingDaysOrWarn(year - 1, filing, permnos);
                var afterTradingDays = GetFirstTradingDaysOrWarn(year + 1, filing, permnos);
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
                        stats.RecordFailure(FilterStage.RequiredData, "Missing required data (CCM/CRSP/BTM)");
                        LogCik(filing.CIK, "Skipping filing due to missing required data (CCM/CRSP/BTM).");
                        continue;
                    }

                    if (await _db.FirmYearRegressionPanelDocumentExists(year, permnos.First(), filing.CIK, bookToMarket.Gvkey))
                    {
                        LogCikWarning(filing.CIK, "Filing {Index}/{Total} for Year {Year} | Already exists in DB, skipping", i + 1, filings.Count, year);
                        continue;
                    }

                    stats.RecordPass(FilterStage.RequiredData);

                    var filter60Result = FilterFunctions.Filter60DaysBeforeAfter(permnos.First(), year, _firmTradingDays);
                    if (!string.IsNullOrWhiteSpace(filter60Result))
                    {
                        stats.RecordFailure(FilterStage.Filter60DaysBeforeAfter, filter60Result);
                        LogCik(filing.CIK, filter60Result);
                        continue;
                    }

                    stats.RecordPass(FilterStage.Filter60DaysBeforeAfter);


                    var filterProcess = FilterProcess.Process(filing, tradingDays, bookToMarket);
                    if (!string.IsNullOrWhiteSpace(filterProcess))
                    {
                        stats.RecordFailure(FilterStage.FilterProcess, filterProcess);
                        LogCik(filing.CIK, filterProcess);
                        continue;
                    }

                    var extractedSections = await ProcessFilingAsync(yearKey, filing, ct, extractFromFile);

                    stats.RecordPass(FilterStage.FilterProcess);
                    stats.RecordEligible();

                    prevTradingDays.AddRange(tradingDays);
                    tradingDays.AddRange(afterTradingDays);

                    var scoresItem1A = _dictionaryScorer.Score(extractedSections.Item1AText);
                    var scoresItem7 = _dictionaryScorer.Score(extractedSections.Item7Text);

                    if (skipDb)
                    {
                        LogCik(filing.CIK, "Filing {Index}/{Total} for Year {Year} | Eligible (skipDb=true)", i + 1, filings.Count, year);
                        continue;
                    }

                    var firmYearRegressionPanelDocument = new FirmYearRegressionPanelDocument
                    {
                        Cik = filing.CIK,
                        Permno = permnos.First(),
                        Gvkey = bookToMarket.Gvkey,
                        FilingDate = filing.DateFiled,
                        Sic = bookToMarket.Sic,
                        ScoresItem1A = scoresItem1A,
                        ScoresItem7 = scoresItem7,

                        PriorReturn = Maths.Round(TradingDaysCalculations.PriorReturn(prevTradingDays, filing.DateFiled)),
                        Volatility = Maths.Round(TradingDaysCalculations.RealizedVolatility(prevTradingDays, filing.DateFiled)),
                        RealizedVariance = Maths.Round(TradingDaysCalculations.RealizedVariance(prevTradingDays, filing.DateFiled)),
                        RealizedVarianceDaysAfter = Maths.Round(TradingDaysCalculations.RealizedVarianceAfterFiling(tradingDays, filing.DateFiled)),
                        Turnover = Maths.Round(TradingDaysCalculations.CumulativeTurnover(prevTradingDays, filing.DateFiled)),
                        TurnoverAvg = Maths.Round(TradingDaysCalculations.AverageTurnover(prevTradingDays, filing.DateFiled)),
                        FilingDayReturn = Maths.Round(TradingDaysCalculations.FilingDayReturn(prevTradingDays, filing.DateFiled)),
                        EventPeriodExcessReturn = Maths.Round(TradingDaysCalculations.Return4Days(tradingDays, filing.DateFiled)),

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
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _db.SendFirmYearRegressionPanelDocument(year, firmYearRegressionPanelDocument);
                    stats.RecordWrittenToDb();

                    LogCik(filing.CIK, "Filing {Index}/{Total} for Year {Year} | Finished", i + 1, filings.Count, year);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    stats.RecordException(ex.Message);
                    LogCikError(ex, filing.CIK, "Filing {Index}/{Total} for Year {Year} | ERROR", i + 1, filings.Count, year);
                }
            }

            sw.Stop();
            stats.SetElapsed(sw.Elapsed);

            LogYearSummary(stats);

            _logger.LogInformation("Finished with Edgar database in {Seconds:F1}s", sw.Elapsed.TotalSeconds);

            return stats;
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

        private async Task<ExtractedSections> ProcessFilingAsync(string yearKey, Filing filing, CancellationToken ct, bool extractFromFile)
        {
            LogCik(filing.CIK, "Downloading filing HTML (cached if available)");

            if (extractFromFile)
            {
                LogCik(filing.CIK, "Extracting sections from previously extracted text files");
                var directoryPath = _downloader.BuildDirectoryPath(yearKey, filing);
                return _extractor.ExtractFile(directoryPath ?? "");
            }

            var htmlPath = await _downloader.GetOrDownloadPrimaryDocAsync(yearKey, filing);


            LogCik(filing.CIK, "Reading HTML from {HtmlPath}", htmlPath);

            var html = await File.ReadAllTextAsync(htmlPath, ct);

            LogCik(filing.CIK, "Cleaning + extracting sections");

            var cleanedText = HtmlCleaner.HtmlToText(html);

            return _extractor.Extract(cleanedText, true, Path.GetDirectoryName(htmlPath) ?? "");
        }

        private void LogYearSummary(YearProcessingStats stats)
        {
            _logger.LogInformation(
                "YEAR {Year} SUMMARY | " +
                "StartedWith={StartedWith} | " +
                "RemovedRequiredData={RemovedRequiredData} | RemainingAfterRequiredData={RemainingAfterRequiredData} | " +
                "Removed60DayFilter={Removed60DayFilter} | RemainingAfter60DayFilter={RemainingAfter60DayFilter} | " +
                "RemovedFilterProcess={RemovedFilterProcess} | RemainingAfterFilterProcess={RemainingAfterFilterProcess} | " +
                "FinalEligible={FinalEligible} | WrittenToDb={WrittenToDb} | Exceptions={Exceptions} | ElapsedSeconds={ElapsedSeconds:F1}",
                stats.Year,
                stats.TotalFilings,
                stats.RemovedRequiredData,
                stats.RemainingAfterRequiredData,
                stats.Removed60DayFilter,
                stats.RemainingAfter60DayFilter,
                stats.RemovedFilterProcess,
                stats.RemainingAfterFilterProcess,
                stats.EligibleFinal,
                stats.WrittenToDb,
                stats.Exceptions,
                stats.Elapsed.TotalSeconds);

            LogFailureBreakdown(stats.Year, "RequiredData", stats.RequiredDataFailures);
            LogFailureBreakdown(stats.Year, "Filter60DaysBeforeAfter", stats.Filter60Failures);
            LogFailureBreakdown(stats.Year, "FilterProcess", stats.FilterProcessFailures);
            LogFailureBreakdown(stats.Year, "Exceptions", stats.ExceptionReasons);
        }

        private void LogFailureBreakdown(int year, string stageName, Dictionary<string, int> failures)
        {
            if (failures.Count == 0)
            {
                _logger.LogInformation("YEAR {Year} FILTER {Stage} | No failures", year, stageName);
                return;
            }

            foreach (var kvp in failures.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
            {
                _logger.LogInformation(
                    "YEAR {Year} FILTER {Stage} | Reason={Reason} | Count={Count}",
                    year,
                    stageName,
                    kvp.Key,
                    kvp.Value);
            }
        }

        private async Task ExportStatsAsync(CancellationToken ct)
        {
            if (_yearStats.Count == 0)
            {
                _logger.LogInformation("No year stats to export.");
                return;
            }

            var exportDirectory = Path.Combine(AppContext.BaseDirectory, "output", "filter-stats");
            Directory.CreateDirectory(exportDirectory);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);

            var csvPath = Path.Combine(exportDirectory, $"panelbuilder_filter_stats_{timestamp}.csv");
            var jsonPath = Path.Combine(exportDirectory, $"panelbuilder_filter_stats_{timestamp}.json");

            await File.WriteAllTextAsync(csvPath, BuildCsv(_yearStats), Encoding.UTF8, ct);

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(_yearStats.Select(CreateExportModel).ToList(), jsonOptions);
            await File.WriteAllTextAsync(jsonPath, json, Encoding.UTF8, ct);

            _logger.LogInformation("Exported filter statistics CSV to {CsvPath}", csvPath);
            _logger.LogInformation("Exported filter statistics JSON to {JsonPath}", jsonPath);
        }

        private static string BuildCsv(IEnumerable<YearProcessingStats> stats)
        {
            var sb = new StringBuilder();

            sb.AppendLine(
                "Year,StartedWith," +
                "RemovedRequiredData,RemainingAfterRequiredData," +
                "Removed60DayFilter,RemainingAfter60DayFilter," +
                "RemovedFilterProcess,RemainingAfterFilterProcess," +
                "FinalEligible,WrittenToDb,Exceptions,ElapsedSeconds," +
                "RequiredDataFailures,Filter60Failures,FilterProcessFailures,ExceptionReasons");

            foreach (var yearStat in stats.OrderBy(x => x.Year))
            {
                sb.Append(EscapeCsv(yearStat.Year.ToString(CultureInfo.InvariantCulture))).Append(',');
                sb.Append(EscapeCsv(yearStat.TotalFilings.ToString(CultureInfo.InvariantCulture))).Append(',');

                sb.Append(EscapeCsv(yearStat.RemovedRequiredData.ToString(CultureInfo.InvariantCulture))).Append(',');
                sb.Append(EscapeCsv(yearStat.RemainingAfterRequiredData.ToString(CultureInfo.InvariantCulture))).Append(',');

                sb.Append(EscapeCsv(yearStat.Removed60DayFilter.ToString(CultureInfo.InvariantCulture))).Append(',');
                sb.Append(EscapeCsv(yearStat.RemainingAfter60DayFilter.ToString(CultureInfo.InvariantCulture))).Append(',');

                sb.Append(EscapeCsv(yearStat.RemovedFilterProcess.ToString(CultureInfo.InvariantCulture))).Append(',');
                sb.Append(EscapeCsv(yearStat.RemainingAfterFilterProcess.ToString(CultureInfo.InvariantCulture))).Append(',');

                sb.Append(EscapeCsv(yearStat.EligibleFinal.ToString(CultureInfo.InvariantCulture))).Append(',');
                sb.Append(EscapeCsv(yearStat.WrittenToDb.ToString(CultureInfo.InvariantCulture))).Append(',');
                sb.Append(EscapeCsv(yearStat.Exceptions.ToString(CultureInfo.InvariantCulture))).Append(',');
                sb.Append(EscapeCsv(yearStat.Elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture))).Append(',');
                sb.Append(EscapeCsv(FlattenReasonCounts(yearStat.RequiredDataFailures))).Append(',');
                sb.Append(EscapeCsv(FlattenReasonCounts(yearStat.Filter60Failures))).Append(',');
                sb.Append(EscapeCsv(FlattenReasonCounts(yearStat.FilterProcessFailures))).Append(',');
                sb.Append(EscapeCsv(FlattenReasonCounts(yearStat.ExceptionReasons)));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static object CreateExportModel(YearProcessingStats stats)
        {
            return new
            {
                stats.Year,
                StartedWith = stats.TotalFilings,

                stats.RemovedRequiredData,
                stats.RemainingAfterRequiredData,

                stats.Removed60DayFilter,
                stats.RemainingAfter60DayFilter,

                stats.RemovedFilterProcess,
                stats.RemainingAfterFilterProcess,

                FinalEligible = stats.EligibleFinal,
                stats.WrittenToDb,
                stats.Exceptions,
                ElapsedSeconds = Math.Round(stats.Elapsed.TotalSeconds, 1),

                RequiredDataFailures = stats.RequiredDataFailures,
                Filter60Failures = stats.Filter60Failures,
                FilterProcessFailures = stats.FilterProcessFailures,
                ExceptionReasons = stats.ExceptionReasons
            };
        }

        private static string FlattenReasonCounts(Dictionary<string, int> dictionary)
        {
            if (dictionary.Count == 0)
                return string.Empty;

            return string.Join(
                " | ",
                dictionary
                    .OrderByDescending(x => x.Value)
                    .ThenBy(x => x.Key)
                    .Select(x => $"{x.Key}={x.Value}"));
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains('"'))
                value = value.Replace("\"", "\"\"");

            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                return $"\"{value}\"";

            return value;
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

        private enum FilterStage
        {
            RequiredData,
            Filter60DaysBeforeAfter,
            FilterProcess
        }

        private sealed class YearProcessingStats
        {
            public int Year { get; }
            public int TotalFilings { get; }

            public int PassedRequiredData { get; private set; }
            public int Passed60DayFilter { get; private set; }
            public int PassedFilterProcess { get; private set; }
            public int EligibleFinal { get; private set; }
            public int WrittenToDb { get; private set; }
            public int Exceptions { get; private set; }
            public TimeSpan Elapsed { get; private set; }

            public int RemovedRequiredData => TotalFilings - PassedRequiredData;
            public int RemainingAfterRequiredData => PassedRequiredData;

            public int Removed60DayFilter => PassedRequiredData - Passed60DayFilter;
            public int RemainingAfter60DayFilter => Passed60DayFilter;

            public int RemovedFilterProcess => Passed60DayFilter - PassedFilterProcess;
            public int RemainingAfterFilterProcess => PassedFilterProcess;

            public Dictionary<string, int> RequiredDataFailures { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, int> Filter60Failures { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, int> FilterProcessFailures { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, int> ExceptionReasons { get; } = new(StringComparer.Ordinal);

            public YearProcessingStats(int year, int totalFilings)
            {
                Year = year;
                TotalFilings = totalFilings;
            }

            public void RecordPass(FilterStage stage)
            {
                switch (stage)
                {
                    case FilterStage.RequiredData:
                        PassedRequiredData++;
                        break;
                    case FilterStage.Filter60DaysBeforeAfter:
                        Passed60DayFilter++;
                        break;
                    case FilterStage.FilterProcess:
                        PassedFilterProcess++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
                }
            }

            public void RecordFailure(FilterStage stage, string reason)
            {
                reason = NormalizeReason(reason);

                switch (stage)
                {
                    case FilterStage.RequiredData:
                        Increment(RequiredDataFailures, reason);
                        break;
                    case FilterStage.Filter60DaysBeforeAfter:
                        Increment(Filter60Failures, reason);
                        break;
                    case FilterStage.FilterProcess:
                        Increment(FilterProcessFailures, reason);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(stage), stage, null);
                }
            }

            public void RecordEligible()
            {
                EligibleFinal++;
            }

            public void RecordWrittenToDb()
            {
                WrittenToDb++;
            }

            public void RecordException(string reason)
            {
                Exceptions++;
                Increment(ExceptionReasons, NormalizeReason(reason));
            }

            public void SetElapsed(TimeSpan elapsed)
            {
                Elapsed = elapsed;
            }

            private static string NormalizeReason(string? reason)
            {
                return string.IsNullOrWhiteSpace(reason) ? "Unknown" : reason.Trim();
            }

            private static void Increment(Dictionary<string, int> dictionary, string key)
            {
                if (dictionary.TryGetValue(key, out var current))
                {
                    dictionary[key] = current + 1;
                }
                else
                {
                    dictionary[key] = 1;
                }
            }
        }
    }
}
