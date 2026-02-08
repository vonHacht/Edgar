using Edgar.Companies;
using Edgar.Config;
using Edgar.Edgar;
using Edgar.Export;
using Edgar.Models;
using Edgar.Parsing;
using Edgar.TextMeasures;

namespace Edgar.Pipeline
{
    public class PanelBuilder
    {
        private readonly AppSettings _settings;
        private readonly CompaniesService _companiesService;

        private readonly EdgarClient _edgarClient;
        private readonly FilingIndexService _indexService;
        private readonly FilingDownloader _downloader;

        private readonly ItemSectionExtractor _extractor;
        private readonly LmDictionaryScorer _dictionaryScorer;

        private readonly CsvExporter _exporter;
        //private readonly CikLinker _linker;

        public PanelBuilder()
        {
            // Configuration
            _settings = AppSettings.Load();

            // Firms
            _companiesService = new CompaniesService(_settings);

            // EDGAR
            _edgarClient = new EdgarClient(_settings);
            _indexService = new FilingIndexService(_edgarClient);
            _downloader = new FilingDownloader(_edgarClient, _settings);

            // Parsing + measures
            _extractor = new ItemSectionExtractor();
            _dictionaryScorer = new LmDictionaryScorer(_settings.DictDir);

            // Output
            _exporter = new CsvExporter();
        }

        public async Task RunAsync()
        {
            Console.WriteLine("Starting EDGAR risk pipeline...");

            var firms = LoadFirms();
            var panelRows = new List<PanelRow>();

            foreach (var firm in firms)
            {
                Console.WriteLine($"Processing firm {firm.CIK}");

                var filings = await _indexService.Get10KFilingsAsync(
                    firm,
                    _settings.StartYear,
                    _settings.EndYear
                );

                foreach (var filing in filings)
                {
                    try
                    {
                        var row = await ProcessFilingAsync(firm, filing);
                        if (row == null)
                            continue;

                        // Attach CUSIP / GVKEY / PERMNO (date-aware)
                        //_linker.AttachLinks(row);

                        panelRows.Add(row);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"Error {firm.CIK} {filing.AccessionNumber}: {ex.Message}"
                        );
                    }
                }
            }

            // OPTIONAL but recommended:
            // enforce "first filing per firm-year"
            panelRows = panelRows
                .GroupBy(r => (r.Cik10, r.Year))
                .Select(g => g.OrderBy(r => r.FilingDate).First())
                .ToList();

            var outputPath = Path.Combine(_settings.OutputDir, "risk_panel.csv");
            await _exporter.WriteAsync(panelRows, outputPath);

            Console.WriteLine($"Pipeline complete. Rows written: {panelRows.Count}");
        }

        private async Task<PanelRow?> ProcessFilingAsync(Firm firm, Filing filing)
        {
            // 1) Download filing HTML (cached)
            var htmlPath = await _downloader.GetOrDownloadPrimaryDocAsync(filing);
            var html = await File.ReadAllTextAsync(htmlPath);

            // 2) Clean + extract sections
            var cleanedText = HtmlCleaner.HtmlToText(html);
            var sections = _extractor.Extract(cleanedText, _settings.ExtractItem7);

            // Basic quality filters (EDGAR-only)
            if (!sections.FoundItem1A)
                return null;

            if (sections.WordCountItem1A < 200)
                return null;

            // 3) Dictionary-based scores
            var dictScores = _dictionaryScorer.Score(sections.Item1AText);

            // 4) Build panel row
            return new PanelRow
            {
                Cik10 = firm.CIK ?? string.Empty,
                Ticker = firm.Ticker,

                AccessionNumber = filing.AccessionNumber,
                FilingDate = filing.FilingDate,
                Year = filing.FilingDate.Year,

                Item1AWordCount = sections.WordCountItem1A,

                RiskCount = dictScores.RiskCount,
                RiskFrequency = dictScores.RiskFrequency,

                NegativeCount = dictScores.NegativeCount,
                NegativeFrequency = dictScores.NegativeFrequency,

                UncertaintyCount = dictScores.UncertaintyCount,
                UncertaintyFrequency = dictScores.UncertaintyFrequency,

                LocalHtmlPath = htmlPath
            };
        }

        private List<Firm> LoadFirms()
        {
            // For now: hardcoded test firms
            // Later: replace with _companiesService.LoadFirms()
            return new List<Firm>
            {
                new Firm { CIK = "0000320193", Ticker = "AAPL" },
                new Firm { CIK = "0000789019", Ticker = "MSFT" }
            };
        }
    }
}
