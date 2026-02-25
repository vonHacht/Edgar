using System.Diagnostics;

using Edgar.Companies;
using Edgar.Config;
using Edgar.Import;
using Edgar.Logging;

using Microsoft.Extensions.Logging;

namespace Edgar.Pipeline
{
    public class ImporterBuilder
    {
        private readonly ILogger<Program> _logger;

        private BookToMarketImporter _bookToMarketImporter;
        private CcmImporter _ccmImporter;
        private CrspImporter _crspImporter;

        private BookToMarketData _bookToMarketData;
        private CikPermnoMap _ccmData;
        private CrspData _crspData;


        public ImporterBuilder(AppSettings? appSettings = null)
        {
            if (appSettings == null)
                appSettings = AppSettings.Load();

            _logger = EdgarLogger.CreateLogger<Program>(appSettings);

            _bookToMarketImporter = new BookToMarketImporter(appSettings);
            _ccmImporter = new CcmImporter(appSettings);
            _crspImporter = new CrspImporter(appSettings);
        }

        public void LoadDataToMemory()
        {
            _logger.LogInformation("----- LOADING CRSP (ccm) INTO MEMORY -----");

            var sw = Stopwatch.StartNew();

            _bookToMarketData = _bookToMarketImporter.ReadAllBookToMarket();
            _ccmData = _ccmImporter.ReadAllYearsUniqueCcms();
            _crspData = _crspImporter.ReadAllCrsp();

            sw.Stop();

            _logger.LogInformation("----- FINISHED IN {Seconds:F1}s -----", sw.Elapsed.TotalSeconds);
        }

        public void EnsureConsistentData()
        {
            if (_crspData is null || _ccmData is null || _bookToMarketData is null)
                throw new InvalidOperationException("Data not loaded. Call LoadDataToMemory() first.");

            const int LOG_EVERY = 100_000;

            var seen = new HashSet<(int Year, int Permno)>();

            int checkedFirmYears = 0;
            int missingCcm = 0;
            int missingBtm = 0;
            int matched = 0;

            _logger.LogInformation("---- Checking CRSP -> CCM -> BookToMarket consistency ----");

            foreach (var (year, permno, _) in _crspData.AllDays())
            {
                // only once per firm-year (AllDays contains many rows per permno)
                if (!seen.Add((year, permno)))
                    continue;

                checkedFirmYears++;
                if (checkedFirmYears % LOG_EVERY == 0)
                    _logger.LogInformation("Checked {Count:N0} CRSP firm-years...", checkedFirmYears);

                // CRSP -> CCM
                if (!_ccmData.HasPermno(permno))
                {
                    missingCcm++;
                    _logger.LogWarning("Permno {Permno} in CRSP (year {Year}) not found in CCM data", permno, year);
                    continue;
                }

                // CRSP -> BTM via mapped CIK(s)
                // Optimization: pull year dictionary once
                if (!_bookToMarketData.TryGetValue(year, out var byCik) || byCik.Count == 0)
                {
                    missingBtm++;
                    _logger.LogWarning("Permno {Permno} in CRSP (year {Year}) has CCM mapping but BookToMarket has no data for that year", permno, year);
                    continue;
                }

                bool found = false;
                foreach (var cik in _ccmData.GetCiks(permno))
                {
                    if (string.IsNullOrWhiteSpace(cik))
                        continue;

                    if (byCik.TryGetValue(cik.Trim(), out var list) && list.Count > 0)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    missingBtm++;
                    var sampleCiks = string.Join(", ", _ccmData.GetCiks(permno).Take(5));
                    _logger.LogWarning(
                        "Permno {Permno} in CRSP (year {Year}) not found in BookToMarket for mapped CIK(s) [{Ciks}]",
                        permno, year, sampleCiks);
                    continue;
                }

                matched++;

                // If this is too noisy, comment it out or make it LogDebug
                //_logger.LogInformation("Matched permno {Permno} year {Year}", permno, year);
            }

            _logger.LogInformation("---- CONSISTENCY SUMMARY ----");
            _logger.LogInformation("CRSP firm-years checked: {N:N0}", checkedFirmYears);
            _logger.LogInformation("Matched: {N:N0}", matched);
            _logger.LogInformation("Missing CCM: {N:N0}", missingCcm);
            _logger.LogInformation("Missing BookToMarket: {N:N0}", missingBtm);
            _logger.LogInformation("---- DONE ----");
        }
    }
}
