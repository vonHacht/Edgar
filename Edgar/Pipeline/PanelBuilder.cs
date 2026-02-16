using Edgar.Companies;
using Edgar.Config;
using Edgar.Edgar;
using Edgar.Export;
using Edgar.Logging;
using Edgar.Parsing;
using Edgar.TextMeasures;
using Edgar.Models;
using Edgar.Utilities;

using Microsoft.Extensions.Logging;

namespace Edgar.Pipeline
{
    public class PanelBuilder
    {
        private static readonly ILogger<Program> _logger =
       EdgarLogger.CreateLogger<Program>();

        private readonly AppSettings _settings;
        private readonly CompaniesService _companiesService;

        private readonly EdgarClient _edgarClient;
        private readonly FilingIndexService _indexService;
        private readonly FilingDownloader _downloader;

        private readonly ItemSectionExtractor _extractor;
        private readonly LmDictionaryScorer _dictionaryScorer;

        private readonly Database.MongoDB _mongoDbEdgar;

        // private readonly CsvExporter _exporter;
        // private readonly CikLinker _linker;

        private readonly FilterExporter _filterExporter;

        public PanelBuilder(AppSettings settings)
        {
            _settings = settings;

            _edgarClient = new EdgarClient(settings);
            _indexService = new FilingIndexService(_edgarClient);
            _downloader = new FilingDownloader(_edgarClient, settings);

            _extractor = new ItemSectionExtractor();

            _mongoDbEdgar = new Database.MongoDB();
        }

        public PanelBuilder()
        {
            _settings = AppSettings.Load();

            _edgarClient = new EdgarClient(_settings);
            _indexService = new FilingIndexService(_edgarClient);
            _downloader = new FilingDownloader(_edgarClient, _settings);

            _extractor = new ItemSectionExtractor();

            _mongoDbEdgar = new Database.MongoDB();
        }

        public async Task RunAsync()
        {
            Console.WriteLine("Starting EDGAR risk pipeline...");

            var years = new List<int>([
                2010, 2011, 2012, 2013, 2014,
                2015, 2016, 2017, 2018, 2019,
                2020, 2021, 2022, 2023
                ]);

            int cntFilings = 0;

            foreach (var year in years)
            {
                var filings = await _indexService.Get10KFilingsForYearAsync(
                    year
                );

                cntFilings += filings.Count;

                foreach (var filing in filings)
                {
                    try
                    {
                        await WriteToDBAsync(filing, year.ToString());                      
                    }
                    catch (Exception ex)
                    {
                        /*Console.WriteLine(
                            $"Error {firm.CIK} {filing.AccessionNumber}: {ex.Message}"
                        );*/
                    }
                }
            }

            // OPTIONAL but recommended:
            // enforce "first filing per firm-year"
            /*panelRows = panelRows
                .GroupBy(r => (r.Cik10, r.Year))
                .Select(g => g.OrderBy(r => r.FilingDate).First())
                .ToList();

            var outputPath = Path.Combine(_settings.OutputDir, filenameRiskpanel);
            await _exporter.WriteAsync(panelRows, outputPath);

            Console.WriteLine($"Pipeline complete. Rows written: {panelRows.Count}");*/

            //await _filterExporter.WriteAsync(firms, Path.Combine(_settings.OutputDir, Config.Filepaths.filterMatches));

            _logger.LogInformation("Pipeline complete. Total filings found: {Count}", cntFilings);
        }

        private async Task WriteToDBAsync(Filing filing, string year)
        {
            // 1) Download filing HTML (cached)
            var htmlPath = await _downloader.GetOrDownloadPrimaryDocAsync(filing);
            var html = await File.ReadAllTextAsync(htmlPath);

            // 2) Clean + extract sections
            var cleanedText = HtmlCleaner.HtmlToText(html);
            var sections = _extractor.Extract(cleanedText, true);

            // 3) Write to DB
            var doc = new FilingExtractDocument
            {
                Cik = filing.CIK,
                AccessionNumber = Accession.GetAccessionFromFilename(filing.Filename),
                FormType = filing.FormType,
                DateFiled = filing.DateFiled,
                Sections = sections
            };

            await _mongoDbEdgar.UpsertAsync(doc, year);

        }

        private async Task ProcessFilingAsync(Filing filing)
        {
            // 1) Download filing HTML (cached)
            var htmlPath = await _downloader.GetOrDownloadPrimaryDocAsync(filing);
            var html = await File.ReadAllTextAsync(htmlPath);

            // 2) Clean + extract sections
            var cleanedText = HtmlCleaner.HtmlToText(html);
            var sections = _extractor.Extract(cleanedText, true);

            // 3) Filtering based on extraction results


            Console.WriteLine("STOP");

            // Basic quality filters (EDGAR-only)
            //if (!sections.FoundItem1A)
            //    return null;

            //if (sections.WordCountItem1A < 200)
            //    return null;

            // 3) Dictionary-based scores
            //var dictScores = _dictionaryScorer.Score(sections.Item1AText);

            // 4) Build panel row
            /*return new PanelRow
            {
                Cik10 = firm.CIK ?? string.Empty,
                Ticker = firm.Ticker,

                //AccessionNumber = filing.AccessionNumber,
                //FilingDate = filing.FilingDate,
                //Year = filing.FilingDate.Year,

                Item1AWordCount = sections.WordCountItem1A,

                RiskCount = dictScores.RiskCount,
                RiskFrequency = dictScores.RiskFrequency,

                NegativeCount = dictScores.NegativeCount,
                NegativeFrequency = dictScores.NegativeFrequency,

                UncertaintyCount = dictScores.UncertaintyCount,
                UncertaintyFrequency = dictScores.UncertaintyFrequency,

                LocalHtmlPath = htmlPath
            };*/
        }


    }
}
