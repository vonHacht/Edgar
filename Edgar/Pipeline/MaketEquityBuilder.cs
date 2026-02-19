using Edgar.Companies;
using Edgar.Config;
using Edgar.Database;
using Edgar.Logging;
using Edgar.Models;

using Microsoft.Extensions.Logging;

namespace Edgar.Pipeline
{
    public sealed class MarketEquityBuilder
    {
        private static readonly ILogger<Program> _logger = EdgarLogger.CreateLogger<Program>();

        private static readonly int[] Years =
        {
            2009, 2010, 2011, 2012, 2013, 2014,
            2015, 2016, 2017, 2018, 2019,
            2020, 2021, 2022, 2023, 2024
        };

        private readonly AppSettings _settings;
        private readonly CrspImporter _crspImporter;
        private readonly MongoDb _mongoDbEdgar;

        public MarketEquityBuilder(AppSettings? settings = null)
        {
            _settings = settings ?? AppSettings.Load();
            _crspImporter = new CrspImporter(_settings);
            _mongoDbEdgar = new MongoDb();
        }

        public async Task RunAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("=== Starting Market Equity pipeline ===");

            HashSet<int> permnos = _crspImporter.ReadPermnoFromCrsp();
            _logger.LogInformation("Loaded {Count} PERMNOs from CRSP", permnos.Count);

            foreach (var year in Years)
            {
                ct.ThrowIfCancellationRequested();

                _logger.LogInformation("----- YEAR {Year} -----", year);

                int processed = 0;
                int skipped = 0;

                foreach (var permno in permnos)
                {
                    ct.ThrowIfCancellationRequested();

                    _logger.LogTrace("Processing PERMNO {Permno} for {Year}", permno, year);

                    var firmTradingDays = _crspImporter.ReadByPermno(permno, year.ToString());

                    if (firmTradingDays is null || firmTradingDays.Count == 0)
                    {
                        _logger.LogTrace("No CRSP data for PERMNO {Permno} in {Year}", permno, year);
                        skipped++;
                        continue;
                    }

                    var last = firmTradingDays[^1];

                    var prc = last.Close ?? 0;
                    var shares = last.SharesOut ?? 0;
                    var marketValue = prc * shares;

                    _logger.LogTrace(
                        "PERMNO {Permno} {Year}: prc={Price}, shares={Shares}, MV={MarketValue}",
                        permno, year, prc, shares, marketValue);

                    await _mongoDbEdgar.UpsertMarketValueAsync(
                        new DatabaseMarketValueDocument
                        {
                            MarketValue = marketValue,
                            Permno = permno
                        },
                        year.ToString(),
                        ct);

                    processed++;
                }

                _logger.LogInformation(
                    "Finished YEAR {Year}. Processed={Processed}, Skipped={Skipped}",
                    year, processed, skipped);
            }

            _logger.LogInformation("=== Market Equity pipeline finished ===");
        }
    }
}



