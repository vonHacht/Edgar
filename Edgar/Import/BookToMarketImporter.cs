using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using Edgar.Config;
using Edgar.Logging;
using Edgar.Models;

using Microsoft.Extensions.Logging;

namespace Edgar.Import
{
    public sealed class BookToMarketImporter
    {
        private static readonly ILogger<BookToMarketImporter> _logger =
            EdgarLogger.CreateLogger<BookToMarketImporter>();

        private readonly string _bookToMarketPath;
        private readonly CsvConfiguration _csvConfig;

        public BookToMarketImporter(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _bookToMarketPath = settings.BookToMarketFilename;

            _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                PrepareHeaderForMatch = args => args.Header.Replace("\uFEFF", "").Trim(),
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null
            };
        }

        public BookToMarket? ReadByCik(string cik, DateTime datadate)
        {
            if (string.IsNullOrWhiteSpace(cik))
                return null;

            var targetCik = cik.Trim();
            var targetDate = datadate.Date;

            using var reader = new StreamReader(_bookToMarketPath);
            using var csv = new CsvReader(reader, _csvConfig);
            csv.Context.RegisterClassMap<BookToMarketMap>();

            foreach (var rec in csv.GetRecords<BookToMarket>())
            {
                if (!string.Equals(rec.cik?.Trim(), targetCik, StringComparison.Ordinal))
                    continue;

                if (rec.datadate != targetDate)
                    continue;

                return rec; // first match
            }

            return null;
        }
    }
}


