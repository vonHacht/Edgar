using System.Diagnostics;
using System.Globalization;

using Edgar.Companies;
using Edgar.Config;
using Edgar.Database;
using Edgar.Edgar;
using Edgar.Import;
using Edgar.Logging;
using Edgar.Models;
using Edgar.Parsing;
using Edgar.Utilities;

using Microsoft.Extensions.Logging;

namespace Edgar.Pipeline
{
    public sealed class PanelBuilder
    {
        private static readonly ILogger<Program> _logger = EdgarLogger.CreateLogger<Program>();

        private const int MinCleanTextChars = 2000;
        private static readonly int[] Years =
        {
            2009, 2010, 2011, 2012, 2013, 2014,
            2015, 2016, 2017, 2018, 2019,
            2020, 2021, 2022, 2023, 2024
        };

        private readonly AppSettings _settings;
        private readonly EdgarClient _edgarClient;
        private readonly FilingIndexService _indexService;
        private readonly FilingDownloader _downloader;
        private readonly ItemSectionExtractor _extractor;
        private readonly CcmImporter _ccmImporter;
        private readonly CrspImporter _crspImporter;
        private readonly BookToMarketImporter _bookToMarketImporter;
        private readonly MongoDb _mongoDbEdgar;

        public PanelBuilder(AppSettings? settings = null)
        {
            _settings = settings ?? AppSettings.Load();

            _edgarClient = new EdgarClient(_settings);
            _indexService = new FilingIndexService(_edgarClient);
            _downloader = new FilingDownloader(_edgarClient, _settings);

            _extractor = new ItemSectionExtractor();
            _ccmImporter = new CcmImporter(_settings);
            _crspImporter = new CrspImporter(_settings);
            _bookToMarketImporter = new BookToMarketImporter(_settings);

            _mongoDbEdgar = new MongoDb();
        }

        public async Task RunAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("=== Starting EDGAR risk pipeline ===");

            var totalFilings = 0;

            foreach (var year in Years)
            {
                ct.ThrowIfCancellationRequested();

                _logger.LogInformation("----- YEAR {Year} -----", year);

                // Filings rules described in comment are assumed handled by Get10KFilingsForYearAsync.
                var filings = await _indexService.Get10KFilingsForYearAsync(year);
                totalFilings += filings.Count;

                _logger.LogInformation("Found {Count} filings for {Year}", filings.Count, year);

                for (var i = 0; i < filings.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var filing = filings[i];
                    var tag = $"[Year {year} | Filing {i + 1}/{filings.Count} | CIK {filing.CIK}]";

                    try
                    {
                        _logger.LogInformation("{Tag} Starting", tag);
                        await ProcessFilingAsync(filing, year, ct);
                        _logger.LogInformation("{Tag} Finished", tag);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "{Tag} ERROR", tag);
                    }
                }
            }

            _logger.LogInformation("=== Pipeline complete. Total filings found: {Count} ===", totalFilings);
        }

        private async Task ProcessFilingAsync(Filing filing, int year, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            var yearKey = year.ToString(CultureInfo.InvariantCulture);

            void Stage(string msg) => _logger.LogInformation("CIK {CIK} | {Msg}", filing.CIK, msg);

            var bookToMarket = _bookToMarketImporter.ReadByCik(filing.CIK, filing.DateFiled);

            if (Filter.Filter.BookValueAboveZero(bookToMarket))
            {
                Stage("Book value below 0 or negative -> writing uncomplete doc");
                await WriteUncompleteAsync(
                    yearKey,
                    filing,
                    reason: $"Book value is {Filter.Filter.BookValue(bookToMarket):F2} (below 0 or negative)",
                    ct: ct);
                return;
            }

            Stage("Downloading filing HTML (cached if available)");
            var htmlPath = await _downloader.GetOrDownloadPrimaryDocAsync(filing);

            ct.ThrowIfCancellationRequested();

            Stage($"Reading HTML from {htmlPath}");
            var html = await File.ReadAllTextAsync(htmlPath, ct);

            Stage("Cleaning + extracting sections");
            var cleanedText = HtmlCleaner.HtmlToText(html);

            if (cleanedText.Length <= MinCleanTextChars)
            {
                await WriteUncompleteAsync(
                    yearKey,
                    filing,
                    reason: $"Cleaned text too short ({cleanedText.Length} chars)",
                    ct: ct);

                return;
            }

            var sections = _extractor.Extract(cleanedText, true);

            Stage("Finding CCM match");
            var ccm = _ccmImporter.ReadByCik(filing.CIK, yearKey).FirstOrDefault();

            if (ccm is null)
            {
                Stage("No CCM match -> writing uncomplete doc");
                await WriteUncompleteAsync(yearKey, filing, "CCM match not found", ct);
                return;
            }

            if (ccm.permno is null)
            {
                Stage("CCM found but permno missing -> writing uncomplete doc");
                await WriteUncompleteAsync(
                    yearKey,
                    filing,
                    reason: "CCM match found but permno is null",
                    ct: ct,
                    name: ccm.CompanyName,
                    ticker: ccm.Ticker);

                return;
            }

            var permno = ccm.permno.Value;

            Stage($"Finding CRSP trading days (permno={permno})");
            var tradingDays = _crspImporter.ReadByPermno(permno, yearKey);

            if (tradingDays.Count == 0)
            {
                Stage("No CRSP trading days -> writing uncomplete doc");
                await WriteUncompleteAsync(
                    yearKey,
                    filing,
                    reason: $"CRSP trading days not found for permno {permno} (ccm found)",
                    ct: ct,
                    name: ccm.CompanyName,
                    ticker: ccm.Ticker);

                return;
            }

            if (Filter.Filter.IsStockPriceBelow3OnDayBeforeFiling(filing.DateFiled, tradingDays))
            {
                Stage("Stock price on day before filing is below $3 -> writing uncomplete doc");
                await WriteUncompleteAsync(
                    yearKey,
                    filing,
                    reason: "Stock price on day before filing is below $3",
                    ct: ct,
                    name: ccm.CompanyName,
                    ticker: ccm.Ticker);

                return;
            }



            Stage("Writing complete document to MongoDB");

            var doc = new DatabaseCompleteDocument
            {
                Name = ccm.CompanyName ?? string.Empty,
                Ticker = ccm.Ticker,
                Cik = filing.CIK,

                permno = permno,
                permco = ccm.permco ?? 0,

                AccessionNumber = Accession.GetAccessionFromFilename(filing.Filename),
                FormType = filing.FormType,
                DateFiled = filing.DateFiled,

                Sections = sections,
                TradingDays = tradingDays
            };

            await _mongoDbEdgar.UpsertCompleteAsync(doc, yearKey);

            sw.Stop();
            Stage($"Done in {sw.Elapsed.TotalSeconds:F1}s");
        }

        private Task WriteUncompleteAsync(
            string yearKey,
            Filing filing,
            string reason,
            CancellationToken ct,
            string? name = null,
            string? ticker = null)
        {
            // ct not currently used by MongoDb method; retained for future-proofing.
            var doc = new DatabaseUncompleteDocument
            {
                Cik = filing.CIK,
                Name = name ?? string.Empty,
                Ticker = ticker,
                ReasonNotFound = reason,
                AccessionNumber = Accession.GetAccessionFromFilename(filing.Filename),
                FormType = filing.FormType,
                DateFiled = filing.DateFiled
            };

            return _mongoDbEdgar.UpsertUncompleteAsync(doc, yearKey);
        }



    }
}


