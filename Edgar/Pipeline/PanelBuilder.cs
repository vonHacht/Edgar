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

        private readonly AppSettings _settings;
        private readonly EdgarClient _edgarClient;
        private readonly FilingIndexService _indexService;
        private readonly FilingDownloader _downloader;
        private readonly ItemSectionExtractor _extractor;
        private readonly CcmImporter _ccmImporter;
        private readonly CrspImporter _crspImporter;
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

            _mongoDbEdgar = new MongoDb();
        }

        private static string FilingTag(int year, int index, int total, Filing filing)
            => $"[Year {year} | Filing {index}/{total} | CIK {filing.CIK}]";

        public async Task RunAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("=== Starting EDGAR risk pipeline ===");

            var years = new[]
            {
                2009, 2010, 2011, 2012, 2013, 2014,
                2015, 2016, 2017, 2018, 2019,
                2020, 2021, 2022, 2023, 2024
            };

            var totalFilings = 0;

            foreach (var year in years)
            {
                ct.ThrowIfCancellationRequested();

                _logger.LogInformation("----- YEAR {Year} -----", year);

                var filings = await _indexService.Get10KFilingsForYearAsync(year);
                totalFilings += filings.Count;

                _logger.LogInformation("Found {Count} filings for {Year}", filings.Count, year);

                for (int i = 0; i < filings.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var filing = filings[i];
                    var tag = FilingTag(year, i + 1, filings.Count, filing);

                    try
                    {
                        _logger.LogInformation("{Tag} Starting", tag);
                        await ProcessFilingAsync(filing, year, ct);
                        _logger.LogInformation("{Tag} Finished", tag);
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

            void Stage(string msg)
                => _logger.LogInformation("CIK {CIK} | {Msg}", filing.CIK, msg);

            Stage("Downloading filing HTML (cached if available)");
            var htmlPath = await _downloader.GetOrDownloadPrimaryDocAsync(filing);

            Stage($"Reading HTML from {htmlPath}");
            var html = await File.ReadAllTextAsync(htmlPath, ct);

            Stage("Cleaning + extracting sections");
            var cleanedText = HtmlCleaner.HtmlToText(html);

            if (cleanedText.Count() <= 2000)
            {
                await UpsertUncompleteAsync(
                    yearKey,
                    new DatabaseUncompleteDocument
                    {
                        Cik = filing.CIK,
                        ReasonNotFound = "Cleaned text too short (" + cleanedText.Count() + " chars)",
                        AccessionNumber = Accession.GetAccessionFromFilename(filing.Filename),
                        FormType = filing.FormType,
                        DateFiled = filing.DateFiled
                    },
                    ct);

                return;
            }


            var sections = _extractor.Extract(cleanedText, true);

            Stage("Finding CCM match");
            var ccmMatches = _ccmImporter.ReadByCik(filing.CIK, yearKey);
            var ccm = ccmMatches.FirstOrDefault();

            if (ccm is null)
            {
                Stage("No CCM match -> writing uncomplete doc (CcmFound=false)");

                await UpsertUncompleteAsync(
                    yearKey,
                    new DatabaseUncompleteDocument
                    {
                        Cik = filing.CIK,
                        ReasonNotFound = "CCM match not found",
                        AccessionNumber = Accession.GetAccessionFromFilename(filing.Filename),
                        FormType = filing.FormType,
                        DateFiled = filing.DateFiled
                    },
                    ct);

                return;
            }

            if (ccm.permno is null)
            {
                Stage("CCM found but permno missing -> writing uncomplete doc (CrspFound=false)");

                await UpsertUncompleteAsync(
                    yearKey,
                    new DatabaseUncompleteDocument
                    {
                        Cik = filing.CIK,
                        Name = ccm.CompanyName ?? string.Empty,
                        Ticker = ccm.Ticker,
                        ReasonNotFound= "CCM match found but permno is null",
                        AccessionNumber = Accession.GetAccessionFromFilename(filing.Filename),
                        FormType = filing.FormType,
                        DateFiled = filing.DateFiled
                    },
                    ct);

                return;
            }

            var permno = ccm.permno.Value;

            Stage($"Finding CRSP trading days (permno={permno})");
            var tradingDays = _crspImporter.ReadByPermno(permno, yearKey);

            if (tradingDays.Count == 0)
            {
                Stage("No CRSP trading days -> writing uncomplete doc (CrspFound=false)");

                await UpsertUncompleteAsync(
                    yearKey,
                    new DatabaseUncompleteDocument
                    {
                        Cik = filing.CIK,
                        Name = ccm.CompanyName ?? string.Empty,
                        Ticker = ccm.Ticker,
                        ReasonNotFound = "CRSP trading days not found for permno " + permno + " (ccm found)",
                        AccessionNumber = Accession.GetAccessionFromFilename(filing.Filename),
                        FormType = filing.FormType,
                        DateFiled = filing.DateFiled
                    },
                    ct);

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

        private Task UpsertUncompleteAsync(string yearKey, DatabaseUncompleteDocument doc, CancellationToken ct)
        {
            // If your MongoDb methods accept CancellationToken, pass it through.
            // Otherwise ct is here for future-proofing.
            return _mongoDbEdgar.UpsertUncompleteAsync(doc, yearKey);
        }
    }
}

