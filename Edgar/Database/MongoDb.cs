using System.Collections.Concurrent;

using Edgar.Models;

using Edgar.Config;

using MongoDB.Driver;

namespace Edgar.Database
{
    public sealed class MongoDb
    {
        private static readonly ReplaceOptions UpsertOptions = new() { IsUpsert = true };

        private readonly IMongoDatabase _completeDb;
        private readonly IMongoDatabase _uncompleteDb;

        private readonly ConcurrentDictionary<string, IMongoCollection<DatabaseCompleteDocument>> _completeCollections = new();
        private readonly ConcurrentDictionary<string, IMongoCollection<DatabaseUncompleteDocument>> _uncompleteCollections = new();

        // Preferred ctor: pass options in (easy to test)
        public MongoDb(DatabaseOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var client = new MongoClient(options.Host);

            _completeDb = client.GetDatabase(options.EdgarDbName);
            _uncompleteDb = client.GetDatabase(options.EdgarLoggingDbName);
        }

        // Convenience ctor: reads env (DB_HOST/DB_NAME/DB_LOGGING_NAME)
        public MongoDb() : this(DatabaseOptions.FromEnvironment())
        {
        }

        private IMongoCollection<DatabaseCompleteDocument> GetCompleteCollection(string name) =>
            _completeCollections.GetOrAdd(name, n => _completeDb.GetCollection<DatabaseCompleteDocument>(n));

        private IMongoCollection<DatabaseUncompleteDocument> GetUncompleteCollection(string name) =>
            _uncompleteCollections.GetOrAdd(name, n => _uncompleteDb.GetCollection<DatabaseUncompleteDocument>(n));

        public Task UpsertCompleteAsync(
            DatabaseCompleteDocument doc,
            string collection,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(doc);
            ValidateCollectionName(collection);

            var filter = Builders<DatabaseCompleteDocument>.Filter.And(
                Builders<DatabaseCompleteDocument>.Filter.Eq(x => x.Cik, doc.Cik),
                Builders<DatabaseCompleteDocument>.Filter.Eq(x => x.AccessionNumber, doc.AccessionNumber)
            );

            return GetCompleteCollection(collection)
                .ReplaceOneAsync(filter, doc, UpsertOptions, ct);
        }

        public Task UpsertUncompleteAsync(
            DatabaseUncompleteDocument doc,
            string collection,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(doc);
            ValidateCollectionName(collection);

            var filter = Builders<DatabaseUncompleteDocument>.Filter.And(
                Builders<DatabaseUncompleteDocument>.Filter.Eq(x => x.Cik, doc.Cik),
                Builders<DatabaseUncompleteDocument>.Filter.Eq(x => x.AccessionNumber, doc.AccessionNumber)
            );

            return GetUncompleteCollection(collection)
                .ReplaceOneAsync(filter, doc, UpsertOptions, ct);
        }

        private static void ValidateCollectionName(string collection)
        {
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentException("Collection name is required.", nameof(collection));

            // Optional: basic hardening to avoid weird names
            if (collection.Contains('\0'))
                throw new ArgumentException("Invalid collection name.", nameof(collection));
        }
    }
}

