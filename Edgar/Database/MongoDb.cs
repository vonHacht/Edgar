using Edgar.Models;

using MongoDB.Driver;

namespace Edgar.Database
{
    public class MongoDB
    {
        private readonly IMongoDatabase _database;

        public MongoDB(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public async Task SendFirmYearRegressionPanelDocument(
            int year,
            int permno,
            string cik,
            string gvkey,
            int sic,
            Filing filing,
            DictionaryScores scoresItem1A,
            DictionaryScores scoreItem7,
            double returns,
            double volatility,
            List<BookToMarket> btm
            
            )
        {
            if (btm == null || btm.Count == 0)
                return;

            BookToMarket bookToMarket = btm[0];

            /*var filter = Builders<FirmYearRegressionPanelDocument>.Filter.And(
                Builders<FirmYearRegressionPanelDocument>.Filter.Eq(x => x.Permno, permno),
                Builders<FirmYearRegressionPanelDocument>.Filter.Eq(x => x.Cik, filing.CIK),
                Builders<FirmYearRegressionPanelDocument>.Filter.Eq(x => x.Gvkey, bookToMarket.Gvkey)
            ); */

            /*var update = Builders<FirmYearRegressionPanelDocument>.Update
                .Set(x => x.Cik, filing.CIK)
                .Set(x => x.Gvkey, bookToMarket.Gvkey)
                .Set(x => x.FilingDate, filing.DateFiled)

                .Set(x => x.ScoresItem1A, scoresItem1A)
                .Set(x => x.ScoresItem7, scoreItem7)

                .Set(x => x.Size, bookToMarket.Size)
                .Set(x => x.MarketEquity, bookToMarket.MarketCap)
                .Set(x => x.BookEquity, bookToMarket.BookEquity)
                .Set(x => x.BookToMarket, bookToMarket.BM)

                //.Set(x => x.Returns, returns)
                .Set(x => x.Volatility, volatility)

                .Set(x => x.Leverage, bookToMarket.Leverage)
                .Set(x => x.TotalAssets, bookToMarket.TotalAssets)

                .Set(x => x.LossProvisionsRawT1, bookToMarket.LossProvisionRaw)
                .Set(x => x.LossProvisionsT1, bookToMarket.LossProvision)

                .Set(x => x.NetIncome, bookToMarket.NetIncome)

                .Set(x => x.TextModelVersion, "llm-risk-v1"); */

            var options = new UpdateOptions { IsUpsert = true };

            //await _database.GetCollection<FirmYearRegressionPanelDocument>(year.ToString()).UpdateOneAsync(filter, update, options);
        }
    }
}
