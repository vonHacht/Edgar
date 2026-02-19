using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Edgar.Models
{
    public class DatabaseMarketValueDocument
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }
        public required decimal MarketValue { get; set; }

        public required int Permno { get; set; }
    }
}
