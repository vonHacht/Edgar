using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Edgar.Models
{
    public class DatabaseDocument
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }

        // EDGAR identifiers / metadata
        public string Name { get; set; } = string.Empty;

        public string Ticker { get; set; } = string.Empty;              // "AAPL"

        public string Cik { get; set; } = string.Empty;                 // "0000012345"
        public string AccessionNumber { get; set; } = string.Empty;     // "0000012345-23-000012"
        public string FormType { get; set; } = "10-K";
        public DateTime DateFiled { get; set; }
    }
}
