using System.Diagnostics;
using System.Globalization;

using Edgar.Config;
using Edgar.Edgar;
using Edgar.Logging;
using Edgar.Models;
using Edgar.Parsing;

using Microsoft.Extensions.Logging;

namespace Edgar.Pipeline
{
    public sealed class EdgarBuilder
    {
        private readonly ILogger<Program> _logger;

        private readonly EdgarClient _edgarClient;
        private readonly FilingIndexService _indexService;
        private readonly FilingDownloader _downloader;

        public EdgarBuilder(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _logger = EdgarLogger.CreateLogger<Program>(settings);

            _edgarClient = new EdgarClient(settings);
            _indexService = new FilingIndexService(_edgarClient);
            _downloader = new FilingDownloader(_edgarClient, settings);
        }

        public async Task<List<string>> DownloadFilingsForYear(int year)
        {
            _logger.LogInformation("----- EDGAR PROCESS YEAR {Year} -----", year);

            var filings = await _indexService.Get10KFilingsForYearAsync(year);
            _logger.LogInformation("Found {Count} filings for {Year} in Edgar database", filings.Count, year);

            var savedPaths = new List<string>(capacity: filings.Count);
            var sw = Stopwatch.StartNew();

            var yearKey = year.ToString(CultureInfo.InvariantCulture);

            for (var i = 0; i < filings.Count; i++)
            {
                var filing = filings[i];
                var tag = BuildTag(year, i, filings.Count, filing.CIK);

                try
                {
                    _logger.LogInformation("{Tag} Starting", tag);

                    var path = await ProcessFilingAsync(yearKey, filing);
                    if (!string.IsNullOrWhiteSpace(path))
                        savedPaths.Add(path);
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

            sw.Stop();
            _logger.LogInformation("Finished with Edgar database in {Seconds:F1}s", sw.Elapsed.TotalSeconds);

            return savedPaths;
        }

        // Replace 'Filing' with your actual filing type returned by Get10KFilingsForYearAsync
        private async Task<string> ProcessFilingAsync(string yearKey, Filing filing)
        {
            Stage(filing.CIK, "Downloading filing HTML (cached if available)");
            var htmlPath = await _downloader.GetOrDownloadPrimaryDocAsync(yearKey, filing);

            Stage(filing.CIK, $"Reading HTML from {htmlPath}");
            var html = await File.ReadAllTextAsync(htmlPath);

            Stage(filing.CIK, "Cleaning + extracting sections");
            var cleanedText = HtmlCleaner.HtmlToText(html);

            // FILTER
            if (Filter.Filter.FilingToShort(cleanedText))
            {
                Stage(filing.CIK, "Filings to short");
                return "";
            }



            // Safer: keep original HTML, write cleaned text alongside it.
            var cleanedPath = Path.ChangeExtension(htmlPath, ".txt");
            await File.WriteAllTextAsync(cleanedPath, cleanedText);

            // Return the cleaned file path (or return htmlPath if that’s what you want)
            return cleanedPath;
        }

        private void Stage(string cik, string msg)
            => _logger.LogInformation("CIK {CIK} | {Msg}", cik, msg);

        private static string BuildTag(int year, int index, int total, string cik)
            => $"CIK {cik} | Filing {index + 1}/{total} for Year {year}";

    }
}
