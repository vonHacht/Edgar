using Edgar.Models;

using MongoDB.Bson;
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
            FirmYearRegressionPanelDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (string.IsNullOrWhiteSpace(document.Id))
                document.Id = ObjectId.GenerateNewId().ToString();

            var collection = _database.GetCollection<FirmYearRegressionPanelDocument>(year.ToString());

            var filter = Builders<FirmYearRegressionPanelDocument>.Filter.And(
                Builders<FirmYearRegressionPanelDocument>.Filter.Eq(x => x.Permno, document.Permno),
                Builders<FirmYearRegressionPanelDocument>.Filter.Eq(x => x.Cik, document.Cik),
                Builders<FirmYearRegressionPanelDocument>.Filter.Eq(x => x.Gvkey, document.Gvkey)
            );

            var options = new ReplaceOptions { IsUpsert = true };

            await collection.ReplaceOneAsync(filter, document, options);
        }

        public async Task<bool> FirmYearRegressionPanelDocumentExists(
            int year, int permno, string cik, string gvkey)
        {
            var collection = _database.GetCollection<FirmYearRegressionPanelDocument>(year.ToString());

            var filter = Builders<FirmYearRegressionPanelDocument>.Filter.And(
                Builders<FirmYearRegressionPanelDocument>.Filter.Eq(x => x.Permno, permno),
                Builders<FirmYearRegressionPanelDocument>.Filter.Eq(x => x.Cik, cik),
                Builders<FirmYearRegressionPanelDocument>.Filter.Eq(x => x.Gvkey, gvkey)
            );

            // Efficient: stops as soon as it finds one match
            return await collection.Find(filter).Limit(1).AnyAsync();
        }
    }
}
