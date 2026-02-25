using System.Diagnostics;

using Edgar.Companies;
using Edgar.Config;
using Edgar.Import;
using Edgar.Logging;
using Edgar.Models;

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
                appSettings = new AppSettings();

            _logger = EdgarLogger.CreateLogger<Program>(appSettings);

            _bookToMarketImporter = new BookToMarketImporter(appSettings);
            _ccmImporter = new CcmImporter(appSettings);
            _crspImporter = new CrspImporter(appSettings);
        }

        /*public void LoadDataToMemory()
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
            if (_ccmData is null || _crspData is null || _bookToMarketData is null)
                throw new InvalidOperationException("Data not loaded. Call LoadDataToMemory() first.");

            const int LOG_EVERY = 100_000;

            int ccmCount = 0;
            int crspCount = 0;
            int btmCount = 0;

            int missingCrspFromCcm = 0;
            int missingBtmFromCcm = 0;
            int missingCcmFromCrsp = 0;
            int missingBtmFromCrsp = 0;
            int missingCcmFromBtm = 0;
            int missingCrspFromBtm = 0;

            _logger.LogInformation("---- Checking CCM -> CRSP & BookToMarket ----");

            var warnedCcmCrsp = new HashSet<(int, int)>();
            var warnedCcmBtm = new HashSet<(int, string)>();

            /* foreach (var (year, cik, ccm) in _ccmData.All())
            {
                ccmCount++;
                if (ccmCount % LOG_EVERY == 0)
                    _logger.LogInformation("Checked {Count:N0} CCM rows...", ccmCount);

                if (ccm.permno is int permno && permno > 0)
                {
                    if (!_crspData.GetDays(year, permno).Any() &&
                        warnedCcmCrsp.Add((year, permno)))
                    {
                        missingCrspFromCcm++;
                        _logger.LogWarning("Missing CRSP: year={Year} cik={Cik} permno={Permno}",
                            year, cik, permno);
                    }
                }

                if (!_bookToMarketData.Get(year, cik).Any() &&
                    warnedCcmBtm.Add((year, cik)))
                {
                    missingBtmFromCcm++;
                    _logger.LogWarning("Missing BookToMarket: year={Year} cik={Cik}", year, cik);
                }
            } 

            _logger.LogInformation("---- Checking CRSP -> CCM & BookToMarket ----");

            var warnedCrspCcm = new HashSet<(int, int)>();
            var warnedCrspBtm = new HashSet<(int, int)>();
            var seenCrsp = new HashSet<(int, int)>();

            foreach (var (year, permno, _) in _crspData.AllDays())
            {
                if (!seenCrsp.Add((year, permno)))
                    continue;

                crspCount++;
                if (crspCount % LOG_EVERY == 0)
                    _logger.LogInformation("Checked {Count:N0} CRSP firm-years...", crspCount);

                if (!_ccmData.Get(year, permno).Any() &&
                    warnedCrspCcm.Add((year, permno)))
                {
                    missingCcmFromCrsp++;
                    _logger.LogWarning("Missing CCM: year={Year} permno={Permno}", year, permno);
                }

                // check BTM via CCM mapping
                var ccms = _ccmData.Get(year, permno);
                if (ccms.Any())
                {
                    bool hasBtm = ccms.Any(c => _bookToMarketData.Get(year, c.CompanyName).Any());
                    if (!hasBtm && warnedCrspBtm.Add((year, permno)))
                    {
                        missingBtmFromCrsp++;
                        _logger.LogWarning("Missing BookToMarket via CCM: year={Year} permno={Permno}",
                            year, permno);
                    }
                }
            }

            _logger.LogInformation("---- Checking BookToMarket -> CCM & CRSP ----");

            foreach (var (year, byCik) in _bookToMarketData)
            {
                foreach (var (cik, list) in byCik)
                {
                    btmCount++;
                    if (btmCount % LOG_EVERY == 0)
                        _logger.LogInformation("Checked {Count:N0} BookToMarket firm-years...", btmCount);

                    /if (!_ccmData.TryGet(year, cik, out var ccms))
                    {
                        missingCcmFromBtm++;
                        _logger.LogWarning("Missing CCM: year={Year} cik={Cik}", year, cik);
                        continue;
                    }

                    bool hasCrsp = ccms.Any(c =>
                        c.permno is int p && _crspData.GetDays(year, p).Any());

                    if (!hasCrsp)
                    {
                        missingCrspFromBtm++;
                        _logger.LogWarning("Missing CRSP via CCM: year={Year} cik={Cik}", year, cik);
                    }
                }
            }

            _logger.LogInformation("---- CONSISTENCY SUMMARY ----");
            _logger.LogInformation("CCM checked: {CcmCount:N0}", ccmCount);
            _logger.LogInformation("CRSP checked: {CrspCount:N0}", crspCount);
            _logger.LogInformation("BTM checked: {BtmCount:N0}", btmCount);

            _logger.LogInformation("Missing CRSP from CCM: {N}", missingCrspFromCcm);
            _logger.LogInformation("Missing BTM from CCM: {N}", missingBtmFromCcm);
            _logger.LogInformation("Missing CCM from CRSP: {N}", missingCcmFromCrsp);
            _logger.LogInformation("Missing BTM from CRSP: {N}", missingBtmFromCrsp);
            _logger.LogInformation("Missing CCM from BTM: {N}", missingCcmFromBtm);
            _logger.LogInformation("Missing CRSP from BTM: {N}", missingCrspFromBtm);

            _logger.LogInformation("---- CONSISTENCY CHECK COMPLETE ----");
        }*/
    }
}
