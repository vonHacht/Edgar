using System.Collections.Concurrent;

using Edgar.Models;

using MongoDB.Driver;

namespace Edgar.Database
{
    public sealed class mDb
    {
        private static readonly ReplaceOptions UpsertReplaceOptions = new() { IsUpsert = true };
        private static readonly FilterDefinitionBuilder<FilingExtractDocument> F = Builders<FilingExtractDocument>.Filter;

        private readonly IMongoDatabase _db;
        private readonly ConcurrentDictionary<string, IMongoCollection<FilingExtractDocument>> _collections = new();

        public mDb()
        {
            var client = new MongoClient(Config.Database.localhost);
            _db = client.GetDatabase(Config.Database.edgarDbName);
        }

        private IMongoCollection<FilingExtractDocument> GetCollection(string name) =>
            _collections.GetOrAdd(name, n => _db.GetCollection<FilingExtractDocument>(n));

        public Task UpsertAsync(
            FilingExtractDocument doc,
            string collection,
            CancellationToken ct = default)
        {
            if (doc is null) throw new ArgumentNullException(nameof(doc));
            if (string.IsNullOrWhiteSpace(collection)) throw new ArgumentException("Collection name is required.", nameof(collection));

            var filter = F.And(
                F.Eq(x => x.Cik, doc.Cik),
                F.Eq(x => x.AccessionNumber, doc.AccessionNumber)
            );

            return GetCollection(collection)
                .ReplaceOneAsync(filter, doc, UpsertReplaceOptions, ct);
        }
    }
}

