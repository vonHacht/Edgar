using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using Edgar.Config;
using Edgar.Logging;
using Edgar.Models;

using Microsoft.Extensions.Logging;

namespace Edgar.Companies
{
    public sealed class CrspImporter
    {
        private static readonly ILogger<Program> _logger = EdgarLogger.CreateLogger<Program>();

        private readonly string _crspPath;
        private readonly CsvConfiguration _csvConfig;

        public CrspImporter(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _crspPath = Path.Combine(settings.CompaniesDir, Filepaths.returnsCRSPFileName);

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

        public List<FirmTradingDay> ReadByPermno(int permno)
        {
            if (!File.Exists(_crspPath))
                throw new FileNotFoundException($"CRSP file not found: {_crspPath}");

            using var reader = new StreamReader(_crspPath);
            using var csv = new CsvReader(reader, _csvConfig);

            return csv.GetRecords<CrspCssRecord>()
                      .Where(r => r.PERMNO == permno)
                      .Select(MapTradingDay)
                      .ToList();
        }

        private static FirmTradingDay MapTradingDay(CrspCssRecord r)
        {
            var prc = r.PRC;

            return new FirmTradingDay
            {
                Date = DateOnly.FromDateTime(r.date),

                ClosePrcRaw = prc.HasValue ? (decimal)prc.Value : null,
                Close = prc.HasValue ? (decimal)Math.Abs(prc.Value) : null,
                CloseIsMidpoint = prc.HasValue && prc.Value < 0,

                Open = r.OPENPRC.HasValue ? (decimal)r.OPENPRC.Value : null,
                Bid = r.BID.HasValue ? (decimal)r.BID.Value : null,
                Ask = r.ASK.HasValue ? (decimal)r.ASK.Value : null,
                BidLow = r.BIDLO.HasValue ? (decimal)r.BIDLO.Value : null,
                AskHigh = r.ASKHI.HasValue ? (decimal)r.ASKHI.Value : null,

                Volume = r.VOL.HasValue ? (long)r.VOL.Value : null,
                NumberOfTrades = r.NUMTRD,

                // ⚠️ SHROUT is often in thousands depending on extract
                SharesOut = r.SHROUT.HasValue ? (long)r.SHROUT.Value : null,

                Ret = r.RET,
                RetExDiv = r.RETX,

                DelistCode = r.DLSTCD.HasValue ? (int)r.DLSTCD.Value : null,
                DelistRet = r.DLRET.HasValue ? (double)r.DLRET.Value : null,
                DelistRetExDiv = r.DLRETX.HasValue ? (double)r.DLRETX.Value : null,
                DelistPrice = r.DLPRC.HasValue ? (double)r.DLPRC.Value : null
            };
        }
    }
}

