using System.Globalization;
using System.Text;

using CsvHelper;
using CsvHelper.Configuration;

using Edgar.Config;
using Edgar.Logging;
using Edgar.Models;

using Microsoft.Extensions.Logging;

namespace Edgar.Companies
{
    public class CompaniesService
    {
        private static readonly ILogger<Program> _logger =
        EdgarLogger.CreateLogger<Program>();

        private readonly List<Firm> _firms = new();

        public CompaniesService(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var samples = Path.Combine(settings.CompaniesDir, Filepaths.samplesFileName);
            var ccm = Path.Combine(settings.CompaniesDir, Filepaths.returnsCCMFileName);
            var crsp = Path.Combine(settings.CompaniesDir, Filepaths.returnsCRSPFileName);

            _firms = ReadSamples(samples);

            // Optional: only load returns if you actually need them in memory
            // This attaches returns to firms if Firm.Permno matches.
            AttachReturnsToFirms_Streaming(ccm, crsp, _firms);
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
        private void AttachReturnsToFirms(string ccm, string crsp, List<Firm> firms)
        {
            if (!File.Exists(ccm))
                throw new FileNotFoundException($"{ccm} file not found");

            if (!File.Exists(crsp))
                throw new FileNotFoundException($"{crsp} file not found");

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

            using var CcmCsv = new CsvReader(new StreamReader(ccm), config);

            using var CrspCsv = new CsvReader(new StreamReader(crsp), config);
            CrspCsv.Context.RegisterClassMap<CrspCssRecordMap>();

            var ccmByPermno = CcmCsv.GetRecords<CcmCssRecord>()
                .Where(static r => !string.IsNullOrWhiteSpace(r.cik))
                .GroupBy(static r => r.LPERMNO)
                .ToDictionary(g => g.Key, g => g.First());

            var firmsByCik = firms
                .Where(f => !string.IsNullOrWhiteSpace(f.CIK))
                .ToDictionary(f => f.CIK!, f => f);

            foreach (var crsprec in CrspCsv.GetRecords<CrspCssRecord>())
            {
                if (!ccmByPermno.TryGetValue(crsprec.PERMNO, out var ccmrec))
                {
                    _logger.LogWarning($"Warning: No CCM record found for PERMNO {crsprec.PERMNO} on date {crsprec.date:d}");
                    continue;
                }

                if (!firmsByCik.TryGetValue(ccmrec.cik!, out var firm))
                {
                    _logger.LogWarning($"Warning: No firm found for CIK {ccmrec.cik} (PERMNO {crsprec.PERMNO}) on date {crsprec.date:d}");
                    continue;
                }

                AddFirmTradingDay(firm, crsprec);
            }
        }

        private void AttachReturnsToFirms_Streaming(string ccm, string crsp, List<Firm> firms)
        {
            if (!File.Exists(ccm)) throw new FileNotFoundException($"{ccm} file not found");
            if (!File.Exists(crsp)) throw new FileNotFoundException($"{crsp} file not found");

            var neededCiks = firms
                .Where(f => !string.IsNullOrWhiteSpace(f.CIK))
                .Select(f => f.CIK!)
                .ToHashSet(StringComparer.Ordinal);

            var firmsByCik = firms
                .Where(f => !string.IsNullOrWhiteSpace(f.CIK))
                .ToDictionary(f => f.CIK!, f => f, StringComparer.Ordinal);

            // --- Stream CCM and only keep permno->cik for ciks we care about
            var ccmConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                PrepareHeaderForMatch = args => args.Header.Replace("\uFEFF", "").Trim(),
                MissingFieldFound = null,
                HeaderValidated = null
            };

            var cikByPermno = new Dictionary<int, string>();

            using (var reader = new StreamReader(ccm,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1 << 20)) // 1MB buffer
            using (var csv = new CsvReader(reader, ccmConfig))
            {
                foreach (var r in csv.GetRecords<CcmCssRecord>())
                {
                    if (string.IsNullOrWhiteSpace(r.cik)) continue;
                    if (!neededCiks.Contains(r.cik)) continue;

                    // if duplicates exist, keep first (or overwrite—your choice)
                    if (r.LPERMNO is int permno && !cikByPermno.ContainsKey(permno))
                    {
                        cikByPermno[permno] = r.cik!;
                    }
                }
            }

            // --- Stream CRSP
            using var crspReader = new StreamReader(crsp,
                 Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                bufferSize: 1 << 20);
            using var crspCsv = new CsvReader(crspReader, ccmConfig);
            crspCsv.Context.RegisterClassMap<CrspCssRecordMap>();

            var missingCcmPermnos = new HashSet<int>();
            var missingFirmCiks = new HashSet<string>(StringComparer.Ordinal);

            foreach (var crsprec in crspCsv.GetRecords<CrspCssRecord>())
            {
                int permno = crsprec.PERMNO;

                if (!cikByPermno.TryGetValue(permno, out var cik))
                {
                    missingCcmPermnos.Add(permno);
                    continue;
                }

                if (!firmsByCik.TryGetValue(cik, out var firm))
                {
                    missingFirmCiks.Add(cik);
                    continue;
                }

                AddFirmTradingDay(firm, crsprec);
            }

            static string Sample<T>(IEnumerable<T> values, int max = 10)
            {
                return string.Join(", ", values);
            }



            if (missingCcmPermnos.Count > 0)
            {
                _logger.LogWarning(
                    "Missing CCM mappings: count={Count}, sample PERMNOs=[{Sample}]",
                    missingCcmPermnos.Count,
                    Sample(missingCcmPermnos)
                );
            }

            if (missingFirmCiks.Count > 0)
            {
                _logger.LogWarning(
                    "Missing firms for CIKs: count={Count}, sample CIKs=[{Sample}]",
                    missingFirmCiks.Count,
                    Sample(missingFirmCiks)
                );
            }

        }


        private void AddFirmTradingDay(Firm firm, CrspCssRecord crsprec)
        {
            var prc = crsprec.PRC;
            firm.FirmTradingDays.Add(new FirmTradingDay
            {
                Date = DateOnly.FromDateTime(crsprec.date),
                ClosePrcRaw = prc.HasValue ? (decimal)prc.Value : null,
                Close = prc.HasValue ? (decimal)Math.Abs(prc.Value) : null,
                CloseIsMidpoint = prc.HasValue && prc.Value < 0,
                Open = crsprec.OPENPRC.HasValue ? (decimal)crsprec.OPENPRC.Value : null,
                Bid = crsprec.BID.HasValue ? (decimal)crsprec.BID.Value : null,
                Ask = crsprec.ASK.HasValue ? (decimal)crsprec.ASK.Value : null,
                BidLow = crsprec.BIDLO.HasValue ? (decimal)crsprec.BIDLO.Value : null,
                AskHigh = crsprec.ASKHI.HasValue ? (decimal)crsprec.ASKHI.Value : null,
                Volume = crsprec.VOL.HasValue ? (long)crsprec.VOL.Value : null,
                NumberOfTrades = crsprec.NUMTRD,
                // ⚠️ SHROUT is often reported in thousands in CRSP outputs.
                // Only cast directly if your extract is in actual shares.
                SharesOut = crsprec.SHROUT.HasValue ? (long)crsprec.SHROUT.Value : null,
                Ret = crsprec.RET,
                RetExDiv = crsprec.RETX,
                DelistCode = crsprec.DLSTCD.HasValue ? (int)crsprec.DLSTCD.Value : null,
                DelistRet = crsprec.DLRET.HasValue ? (double)crsprec.DLRET.Value : null,
                DelistRetExDiv = crsprec.DLRETX.HasValue ? (double)crsprec.DLRETX.Value : null,
                DelistPrice = crsprec.DLPRC.HasValue ? (double)crsprec.DLPRC.Value : null
            });
        }
    }
}
