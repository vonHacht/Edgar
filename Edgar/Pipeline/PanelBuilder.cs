using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Edgar.Config;
using Edgar.Edgar;
using Edgar.Export;
using Edgar.Parsing;
using Edgar.Models;
using Edgar.TextMeasures;

namespace Edgar.Pipeline
{
    public class PanelBuilder
    {
        private readonly AppSettings _settings;
        private readonly EdgarClient _edgarClient;
        private readonly FilingIndexService _indexService;
        private readonly FilingDownloader _downloader;
        private readonly ItemSectionExtractor _extractor;
        private readonly LmDictionaryScorer _dictionaryScorer;
        private readonly CsvExporter _exporter;

        public PanelBuilder()
        {
            // Configuration
            _settings = AppSettings.Load();

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
                Console.WriteLine($"Processing firm {firm.Cik10}");

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
                        if (row != null)
                            panelRows.Add(row);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error {firm.Cik10} {filing.AccessionNumber}: {ex.Message}");
                    }
                }
            }

            var outputPath = Path.Combine(_settings.OutputDir, "risk_panel.csv");
            await _exporter.WriteAsync(panelRows, outputPath);

            Console.WriteLine($"Pipeline complete. Rows written: {panelRows.Count}");
        }

        private async Task<PanelRow?> ProcessFilingAsync(Firm firm, Filing filing)
        {
            // 1. Download filing HTML (cached)
            var htmlPath = await _downloader.GetOrDownloadPrimaryDocAsync(filing);
            var html = await File.ReadAllTextAsync(htmlPath);

            // 2. Clean + extract sections
            var cleanedText = HtmlCleaner.HtmlToText(html);
            var sections = _extractor.Extract(cleanedText, _settings.ExtractItem7);

            if (!sections.FoundItem1A || sections.WordCountItem1A < 200)
                return null; // basic quality filter

            // 3. Dictionary-based scores
            var dictScores = _dictionaryScorer.Score(sections.Item1AText);

            // 4. Build panel row
            return new PanelRow
            {
                Cik10 = firm.Cik10,
                AccessionNumber = filing.AccessionNumber,
                FilingDate = filing.FilingDate,
                Year = filing.FilingDate.Year,

                Item1AWordCount = sections.WordCountItem1A,

                RiskCount = dictScores.RiskCount,
                RiskFrequency = dictScores.RiskFrequency,

                NegativeCount = dictScores.NegativeCount,
                NegativeFrequency = dictScores.NegativeFrequency,

                UncertaintyCount = dictScores.UncertaintyCount,
                UncertaintyFrequency = dictScores.UncertaintyFrequency
            };
        }

        private List<Firm> LoadFirms()
        {
            // START SMALL: hardcode a few CIKs first
            return new List<Firm>
            {
                new Firm { Cik10 = "0000320193" }, // Apple
                new Firm { Cik10 = "0000789019" }  // Microsoft
            };
        }
    }
}

