using MongoDB.Bson.Serialization.Attributes;

namespace Edgar.Models
{
    public class LLmScores
    {
        [BsonElement("risk_score")]
        public double RiskScore { get; init; }

        [BsonElement("rationale")]
        public string Rationale { get; init; } = string.Empty;

        [BsonElement("confidence")]
        public ConfidenceEnum Confidence { get; init; }
    }
}
