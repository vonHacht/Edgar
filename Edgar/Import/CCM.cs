using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using Edgar.Config;
using Edgar.Logging;
using Edgar.Models;

using Microsoft.Extensions.Logging;

namespace Edgar.Import
{
    public sealed class CcmImporter
    {
        private static readonly ILogger<Program> _logger = EdgarLogger.CreateLogger<Program>();

        private readonly string _ccmPath;
        private readonly CsvConfiguration _csvConfig;

        public CcmImporter(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _ccmPath = Path.Combine(settings.CompaniesDir, Filepaths.returnsCCMFileName);

            _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                PrepareHeaderForMatch = args => args.Header.Replace("\uFEFF", "").Trim(),
                MissingFieldFound = null,
                HeaderValidated = null
            };
        }

        public List<Ccm> ReadByCik(string cik)
        {
            if (string.IsNullOrWhiteSpace(cik))
                return new List<Ccm>();

            if (!File.Exists(_ccmPath))
                throw new FileNotFoundException($"CCM path not found: {_ccmPath}");

            var normalizedCik = cik.Trim();

            using var reader = new StreamReader(_ccmPath);
            using var csv = new CsvReader(reader, _csvConfig);

            // Streams the file; materializes only the matches.
            return csv.GetRecords<CcmCssRecord>()
                      .Where(r => string.Equals(r.cik?.Trim(), normalizedCik, StringComparison.Ordinal))
                      .Select(r => new Ccm
                      {
                          permno = r.LPERMNO,
                          permco = r.LPERMCO
                      })
                      .ToList();
        }
    }
}

