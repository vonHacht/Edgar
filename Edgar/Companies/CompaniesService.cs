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
        private const string ReturnsFileName = "returns_2010_2023.csv";

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
        private static List<Firm> ReadSamples(string samplesFilePath)
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
        private static void AttachReturnsToFirms(string returnsFilePath, List<Firm> firms)
        {
            if (!File.Exists(returnsFilePath))
                throw new FileNotFoundException($"Returns file not found: {returnsFilePath}");

            // Build fast lookup: Permno -> Firm
            // (Best practice: link on PERMNO, not company name)
            /*var byPermno = firms
                .Where(f => f.Permno.HasValue)
                .GroupBy(f => f.Permno!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());*/

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

            foreach (var rec in csv.GetRecords<CrspDailyRecord>())
            {
                /*if (!byPermno.TryGetValue(rec.Permno, out var firmList))
                    continue;*/

                // Attach to every firm sharing this PERMNO (rare but safe)
                /*foreach (var firm in firmList)
                {
                    //firm.CRSP ??= new List<CrspDailyRecord>();
                    firm.CRSP.Add(rec);
                }*/

                Console.WriteLine("..");
            }
        }
    }
}
