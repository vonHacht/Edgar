using Edgar.Models;

using MongoDB.Driver;

namespace Edgar.Database
{
    public class MongoDB
    {
        private readonly IMongoCollection<FirmYearRegressionPanelDocument> _collection;

        public MongoDB(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _collection = database.GetCollection<FirmYearRegressionPanelDocument>("FirmYearRegressionPanel");
        }

        public async Task SendFirmYearRegressionPanelDocument(
            int year,
            int permno,
            Filing filing,
            DictionaryScores scoresItem1A,
            DictionaryScores scoreItem7,
            ExtractedSections sections,
            double returns,
            double volatility,
            List<BookToMarket> btm)
        {
            if (btm == null || btm.Count == 0)
                return;

            BookToMarket bookToMarket = btm[0];

            var filter = Builders<FirmYearRegressionPanelDocument>.Filter.And(
                Builders<FirmYearRegressionPanelDocument>.Filter.Eq(x => x.Permno, permno),
                Builders<FirmYearRegressionPanelDocument>.Filter.Eq(x => x.FiscalYear, year)
            );

            var update = Builders<FirmYearRegressionPanelDocument>.Update
                .Set(x => x.Cik, filing.CIK)
                .Set(x => x.Gvkey, bookToMarket.Gvkey)
                .Set(x => x.FilingDate, filing.DateFiled)

                .Set(x => x.RiskTextDictionaryItem1A, scoresItem1A.RiskFrequency)
                .Set(x => x.RiskTextLlmItem1A, 0.0)
                .Set(x => x.RiskTextDictionaryItem7, scoreItem7.RiskFrequency)
                .Set(x => x.RiskTextLlmItem7, 0.0)

                .Set(x => x.Item1AWordCount, scoresItem1A.TotalWords)
                .Set(x => x.Item7WordCount, scoreItem7.TotalWords)

                .Set(x => x.Size, bookToMarket.Size)
                .Set(x => x.MarketEquity, bookToMarket.MarketCap)
                .Set(x => x.BookEquity, bookToMarket.BookEquity)
                .Set(x => x.BookToMarket, bookToMarket.BM)

                .Set(x => x.Returns, returns)
                .Set(x => x.Volatility, volatility)

                .Set(x => x.Leverage, bookToMarket.Leverage)
                .Set(x => x.TotalAssets, bookToMarket.TotalAssets)

                .Set(x => x.LossProvisionsRawT1, bookToMarket.LossProvisionRaw)
                .Set(x => x.LossProvisionsT1, bookToMarket.LossProvision)

                .Set(x => x.GdpGrowth, 0.0)
                .Set(x => x.UnemploymentRate, 0.0)
                .Set(x => x.InterestRate, 0.0)

                .Set(x => x.TextModelVersion, "llm-risk-v1")

                // ensure these exist if inserted
                .SetOnInsert(x => x.Permno, permno)
                .SetOnInsert(x => x.FiscalYear, year);

            var options = new UpdateOptions { IsUpsert = true };

            await _collection.UpdateOneAsync(filter, update, options);
        }
    }
}
