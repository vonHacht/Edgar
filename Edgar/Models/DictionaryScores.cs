using MongoDB.Bson.Serialization.Attributes;

namespace Edgar.Models
{
    public class DictionaryScores
    {
        [BsonElement("total_words")]
        public int TotalWords { get; set; }

        [BsonElement("positive_words")]
        public int PositiveWords { get; set; }

        [BsonElement("negative_words")]
        public int NegativeWords { get; set; }

        [BsonElement("uncertainty_words")]
        public int UncertaintyWords { get; set; }

        [BsonElement("sentiment")]
        public int Sentiment { get; set; }

        [BsonElement("uncertainty_score")]
        public double UncertaintyScore { get; set; }

        public void Recalculate()
        {
            Sentiment = PositiveWords - NegativeWords;
            UncertaintyScore = TotalWords > 0
                ? (double)UncertaintyWords / TotalWords
                : 0.0;
        }
    }
}
