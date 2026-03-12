using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Edgar.Models
{
    /// <summary>
    /// One firm-year observation for the regression panel.
    /// Designed for MongoDB storage and OLS / panel regressions.
    /// </summary>
    public class FirmYearRegressionPanelDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string Id { get; set; }

        /// <summary>
        /// SEC Central Index Key.
        /// Used to connect the observation back to the EDGAR filing source.
        /// </summary>
        [BsonElement("cik")]
        public required string Cik { get; set; }

        /// <summary>
        /// Compustat firm identifier.
        /// Main accounting identifier.
        /// </summary>
        [BsonElement("gvkey")]
        public required string Gvkey { get; set; }

        /// <summary>
        /// CRSP permanent security identifier.
        /// Main market-data identifier.
        /// </summary>
        [BsonElement("permno")]
        public required int Permno { get; set; }

        /// <summary>
        /// Filing date for the 10-K used to extract Item 1A / Item 7.
        /// Helpful when aligning the text to the correct fiscal year.
        /// </summary>
        [BsonElement("filing_date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public required DateTime FilingDate { get; set; }

        [BsonElement("scores_item1A")]
        public required DictionaryScores ScoresItem1A { get; set; }

        [BsonElement("scores_item7")]
        public required DictionaryScores ScoresItem7 { get; set; }

        /// <summary>
        /// Firm size control, typically log market capitalization.
        /// </summary>
        [BsonElement("size")]
        public required double Size { get; set; }

        /// <summary>
        /// Market capitalization level before log transformation, if you want both.
        /// </summary>
        [BsonElement("market_equity")]
        public required double MarketEquity { get; set; }

        /// <summary>
        /// Accounting book equity.
        /// Used to construct book-to-market.
        /// </summary>
        [BsonElement("book_equity")]
        public required double BookEquity { get; set; }

        /// <summary>
        /// Book-to-market ratio.
        /// Standard firm characteristic control.
        /// </summary>
        [BsonElement("book_to_market")]
        public required double BookToMarket { get; set; }

        /// <summary>
        /// Stock return over the chosen control window.
        /// For example, annual buy-and-hold return.
        /// </summary>
        [BsonElement("returns")]
        public required double Returns { get; set; }

        /// <summary>
        /// Optional stock return volatility.
        /// </summary>
        [BsonElement("volatility")]
        public required double Volatility { get; set; }

        /// <summary>
        /// Optional leverage control.
        /// </summary>
        [BsonElement("leverage")]
        public required double Leverage { get; set; }

        /// <summary>
        /// Total assets in fiscal year t.
        /// Often used as scaling denominator.
        /// </summary>
        [BsonElement("total_assets")]
        public required double TotalAssets { get; set; }

        [BsonElement("net_income")]
        public required double NetIncome { get; set; }

        /// <summary>
        /// Raw future loss provisions in year t+1.
        /// Stored for traceability before scaling.
        /// </summary>
        [BsonElement("loss_provisions_raw_t1")]
        public required double LossProvisionsRawT1 { get; set; }

        /// <summary>
        /// Main dependent variable:
        /// future loss provisions in t+1 scaled by total assets.
        /// </summary>
        [BsonElement("loss_provisions_t1")]
        public required double LossProvisionsT1 { get; set; }

        /// <summary>
        /// Version of your text-scoring pipeline or prompt version.
        /// </summary>
        [BsonElement("text_model_version")]
        public required string TextModelVersion { get; set; }

        /// <summary>
        /// Timestamp for data lineage and reprocessing.
        /// </summary>
        [BsonElement("updated_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
