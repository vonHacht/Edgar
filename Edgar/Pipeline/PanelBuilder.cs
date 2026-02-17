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

        public async Task RunAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Starting EDGAR risk pipeline...");

            var years = new[]
            {
                2010, 2011, 2012, 2013, 2014,
                2015, 2016, 2017, 2018, 2019,
                2020, 2021, 2022, 2023
            };

            var totalFilings = 0;

            foreach (var year in years)
            {
                ct.ThrowIfCancellationRequested();

                var filings = await _indexService.Get10KFilingsForYearAsync(year);
                totalFilings += filings.Count;

                foreach (var filing in filings)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        await ProcessFilingAsync(filing, year, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing filing {CIK}", filing.CIK);
                    }
                }
            }

            _logger.LogInformation("Pipeline complete. Total filings found: {Count}", totalFilings);
        }

        private async Task ProcessFilingAsync(Filing filing, int year, CancellationToken ct)
        {
            var yearKey = year.ToString(CultureInfo.InvariantCulture);

            // 1) Download filing HTML (cached) + read
            var htmlPath = await _downloader.GetOrDownloadPrimaryDocAsync(filing);
            var html = await File.ReadAllTextAsync(htmlPath, ct);

            // 2) Clean + extract sections
            var cleanedText = HtmlCleaner.HtmlToText(html);
            var sections = _extractor.Extract(cleanedText, true);

            // 3) Find CCM match(es)
            var ccmMatches = _ccmImporter.ReadByCik(filing.CIK, yearKey);
            var ccm = ccmMatches.FirstOrDefault();

            if (ccm is null)
            {
                await UpsertUncompleteAsync(
                    yearKey,
                    new DatabaseUncompleteDocument
                    {
                        Cik = filing.CIK,
                        CcmFound = false,
                        CrspFound = false,
                        AccessionNumber = Accession.GetAccessionFromFilename(filing.Filename),
                    },
                    ct);

                return;
            }

            // permno should not silently become 0 (0 will cause wasted scans / wrong joins)
            if (ccm.permno is null)
            {
                await UpsertUncompleteAsync(
                    yearKey,
                    new DatabaseUncompleteDocument
                    {
                        Cik = filing.CIK,
                        Name = ccm.CompanyName ?? string.Empty,
                        Ticker = ccm.Ticker,
                        CcmFound = true,
                        CrspFound = false,
                        AccessionNumber = Accession.GetAccessionFromFilename(filing.Filename),
                        FormType = filing.FormType,
                        DateFiled = filing.DateFiled
                    },
                    ct);

                return;
            }

            var permno = ccm.permno.Value;

            // 4) Find CRSP trading days
            var tradingDays = _crspImporter.ReadByPermno(permno, yearKey);

            if (tradingDays.Count == 0)
            {
                await UpsertUncompleteAsync(
                    yearKey,
                    new DatabaseUncompleteDocument
                    {
                        Cik = filing.CIK,
                        Name = ccm.CompanyName ?? string.Empty,
                        Ticker = ccm.Ticker,
                        CcmFound = true,
                        CrspFound = false,
                        AccessionNumber = Accession.GetAccessionFromFilename(filing.Filename),
                        FormType = filing.FormType,
                        DateFiled = filing.DateFiled
                    },
                    ct);

                return;
            }

            // 5) Write complete document
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

            Console.WriteLine("");
        }

        private Task UpsertUncompleteAsync(string yearKey, DatabaseUncompleteDocument doc, CancellationToken ct)
        {
            // If your mDb methods accept CancellationToken, pass it through.
            // Otherwise ct is just for future-proofing.
            return _mongoDbEdgar.UpsertUncompleteAsync(doc, yearKey);
        }
    }
}
