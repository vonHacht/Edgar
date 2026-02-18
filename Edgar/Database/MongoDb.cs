using System.Collections.Concurrent;

using Edgar.Config;
using Edgar.Models;

using MongoDB.Bson;
using MongoDB.Driver;

namespace Edgar.Database
{
    public sealed class MongoDb
    {
        private static readonly ReplaceOptions UpsertOptions = new() { IsUpsert = true };

        // IMPORTANT: only two collections total (prevents Cosmos throughput explosion)
        private const string CompleteCollectionName = "complete";
        private const string UncompleteCollectionName = "uncomplete";

        private readonly IMongoDatabase _completeDb;
        private readonly IMongoDatabase _uncompleteDb;

        // Cache the two collections
        private readonly ConcurrentDictionary<string, IMongoCollection<BsonDocument>> _collections = new();

        // Preferred ctor: pass options in (easy to test)
        public MongoDb(DatabaseOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            // NOTE: Make sure options.Host is a Mongo connection string for Cosmos Mongo API,
            // e.g. "mongodb://..." or "mongodb+srv://..."
            var client = new MongoClient(options.Host);

            _completeDb = client.GetDatabase(options.EdgarDbName);
            _uncompleteDb = client.GetDatabase(options.EdgarLoggingDbName);
        }

        // Convenience ctor: reads env (DB_HOST/DB_NAME/DB_LOGGING_NAME)
        public MongoDb() : this(DatabaseOptions.FromEnvironment())
        {
        }

        private IMongoCollection<BsonDocument> GetCollection(IMongoDatabase db, string collectionName)
        {
            // Cache per (dbName, collectionName)
            var key = $"{db.DatabaseNamespace.DatabaseName}:{collectionName}";
            return _collections.GetOrAdd(key, _ => db.GetCollection<BsonDocument>(collectionName));
        }

        public Task UpsertCompleteAsync(
            DatabaseCompleteDocument doc,
            string collection, // <-- keep signature, but interpret as YearKey
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(doc);
            ValidateYearKey(collection);

            var yearKey = collection;

            // Store yearKey inside the document without changing your model types
            var bson = doc.ToBsonDocument();
            bson["YearKey"] = yearKey;

            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("Cik", doc.Cik),
                Builders<BsonDocument>.Filter.Eq("AccessionNumber", doc.AccessionNumber),
                Builders<BsonDocument>.Filter.Eq("YearKey", yearKey)
            );

            return GetCollection(_completeDb, CompleteCollectionName)
                .ReplaceOneAsync(filter, bson, UpsertOptions, ct);
        }

        public Task UpsertUncompleteAsync(
            DatabaseUncompleteDocument doc,
            string collection, // <-- keep signature, but interpret as YearKey
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(doc);
            ValidateYearKey(collection);

            var yearKey = collection;

            var bson = doc.ToBsonDocument();
            bson["YearKey"] = yearKey;

            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("Cik", doc.Cik),
                Builders<BsonDocument>.Filter.Eq("AccessionNumber", doc.AccessionNumber),
                Builders<BsonDocument>.Filter.Eq("YearKey", yearKey)
            );

            return GetCollection(_uncompleteDb, UncompleteCollectionName)
                .ReplaceOneAsync(filter, bson, UpsertOptions, ct);
        }

        private static void ValidateYearKey(string yearKey)
        {
            if (string.IsNullOrWhiteSpace(yearKey))
                throw new ArgumentException("Year key is required.", nameof(yearKey));

            // Optional: ensure it's a year like "2010"
            if (yearKey.Length != 4 || !int.TryParse(yearKey, out _))
                throw new ArgumentException("Year key must look like '2010'.", nameof(yearKey));

            if (yearKey.Contains('\0'))
                throw new ArgumentException("Invalid year key.", nameof(yearKey));
        }
    }
}

