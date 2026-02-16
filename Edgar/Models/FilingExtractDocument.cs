using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Edgar.Models
{
    public class FilingExtractDocument
    {
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; set; }



        // EDGAR identifiers / metadata
        public string Cik { get; set; } = string.Empty;                 // "0000012345"
        public string AccessionNumber { get; set; } = string.Empty;     // "0000012345-23-000012"
        public string FormType { get; set; } = "10-K";                  // optional
        public DateTime DateFiled { get; set; }                        // optional

        // Your extracted content
        public ExtractedSections Sections { get; set; } = new();
    }
}
