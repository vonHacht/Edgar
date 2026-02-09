using CsvHelper;
using CsvHelper.Configuration;
using Edgar.Config;
using Edgar.Models;
using System.Globalization;

namespace Edgar.Companies
{
    public class CompaniesService
    {
        private const string SamplesFileName = "samples_2010_2023.csv";
        private const string ReturnsFileName = "returns_small_2010_2023.csv";

        private readonly List<Firm> _firms = new();

        public CompaniesService(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var samplesPath = Path.Combine(settings.CompaniesDir, SamplesFileName);
            var returnsPath = Path.Combine(settings.CompaniesDir, ReturnsFileName);

            _firms = ReadSamples(samplesPath);

            // Optional: only load returns if you actually need them in memory
            // This attaches returns to firms if Firm.Permno matches.
            AttachReturnsToFirms(returnsPath, _firms);
        }

        public List<Firm> LoadFirms() => _firms;

        // ----------------------------
        // Read samples (firm list)
        // ----------------------------
        private List<Firm> ReadSamples(string samplesFilePath)
        {
            if (!File.Exists(samplesFilePath))
                throw new FileNotFoundException($"Samples file not found: {samplesFilePath}");

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                PrepareHeaderForMatch = args => args.Header.Replace("\uFEFF", "").Trim(),
                MissingFieldFound = null,
                HeaderValidated = null
            };

            using var reader = new StreamReader(samplesFilePath);
            using var csv = new CsvReader(reader, config);

            return csv.GetRecords<Firm>().ToList();
        }

        // ----------------------------
        // Attach CRSP daily returns
        // ----------------------------
        private void AttachReturnsToFirms(string returnsFilePath, List<Firm> firms)
        {
            if (!File.Exists(returnsFilePath))
                throw new FileNotFoundException($"Returns file not found: {returnsFilePath}");

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                PrepareHeaderForMatch = args => args.Header.Replace("\uFEFF", "").Trim(),
                MissingFieldFound = null,
                HeaderValidated = null
            };

            using var reader = new StreamReader(returnsFilePath);
            using var csv = new CsvReader(reader, config);

            foreach (var rec in csv.GetRecords<CrspDailyRecordSmall>())
            {
                foreach (var firm in _firms)
                {
                    if (rec.cik == firm.CIK) 
                    {
                        firm.CRSPSmall.Add(rec);
                    }
                }
            }
        }
    }
}
