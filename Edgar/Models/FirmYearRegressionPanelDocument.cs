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
        public string Id { get; set; }

        /// <summary>
        /// SEC Central Index Key.
        /// Used to connect the observation back to the EDGAR filing source.
        /// </summary>
        [BsonElement("cik")]
        public string Cik { get; set; }

        /// <summary>
        /// Compustat firm identifier.
        /// Main accounting identifier.
        /// </summary>
        [BsonElement("gvkey")]
        public string Gvkey { get; set; }

        /// <summary>
        /// CRSP permanent security identifier.
        /// Main market-data identifier.
        /// </summary>
        [BsonElement("permno")]
        public int? Permno { get; set; }

        /// <summary>
        /// Fiscal year t for the explanatory variables.
        /// </summary>
        [BsonElement("fyear")]
        public int FiscalYear { get; set; }

        /// <summary>
        /// Filing date for the 10-K used to extract Item 1A / Item 7.
        /// Helpful when aligning the text to the correct fiscal year.
        /// </summary>
        [BsonElement("filing_date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? FilingDate { get; set; }

        /// <summary>
        /// Dictionary-based textual risk score from Item 1A.
        /// Example: proportion of LM risk-related words.
        /// </summary>
        [BsonElement("risk_text_dictionary_item1a")]
        public double? RiskTextDictionaryItem1A { get; set; }

        /// <summary>
        /// LLM-based contextual risk score from Item 1A.
        /// </summary>
        [BsonElement("risk_text_llm_item1a")]
        public double? RiskTextLlmItem1A { get; set; }

        /// <summary>
        /// Optional dictionary-based textual risk score from Item 7 (MD&A).
        /// </summary>
        [BsonElement("risk_text_dictionary_item7")]
        public double? RiskTextDictionaryItem7 { get; set; }

        /// <summary>
        /// Optional LLM-based contextual risk score from Item 7 (MD&A).
        /// </summary>
        [BsonElement("risk_text_llm_item7")]
        public double? RiskTextLlmItem7 { get; set; }

        /// <summary>
        /// Total word count in Item 1A.
        /// Useful for traceability and robustness.
        /// </summary>
        [BsonElement("item1a_word_count")]
        public int? Item1AWordCount { get; set; }

        /// <summary>
        /// Total word count in Item 7.
        /// Useful for traceability and robustness.
        /// </summary>
        [BsonElement("item7_word_count")]
        public int? Item7WordCount { get; set; }

        /// <summary>
        /// Firm size control, typically log market capitalization.
        /// </summary>
        [BsonElement("size")]
        public double? Size { get; set; }

        /// <summary>
        /// Market capitalization level before log transformation, if you want both.
        /// </summary>
        [BsonElement("market_equity")]
        public double? MarketEquity { get; set; }

        /// <summary>
        /// Accounting book equity.
        /// Used to construct book-to-market.
        /// </summary>
        [BsonElement("book_equity")]
        public double? BookEquity { get; set; }

        /// <summary>
        /// Book-to-market ratio.
        /// Standard firm characteristic control.
        /// </summary>
        [BsonElement("book_to_market")]
        public double? BookToMarket { get; set; }

        /// <summary>
        /// Stock return over the chosen control window.
        /// For example, annual buy-and-hold return.
        /// </summary>
        [BsonElement("returns")]
        public double? Returns { get; set; }

        /// <summary>
        /// Optional stock return volatility.
        /// </summary>
        [BsonElement("volatility")]
        public double? Volatility { get; set; }

        /// <summary>
        /// Optional leverage control.
        /// </summary>
        [BsonElement("leverage")]
        public double? Leverage { get; set; }

        /// <summary>
        /// Total assets in fiscal year t.
        /// Often used as scaling denominator.
        /// </summary>
        [BsonElement("total_assets")]
        public double? TotalAssets { get; set; }

        /// <summary>
        /// Raw future loss provisions in year t+1.
        /// Stored for traceability before scaling.
        /// </summary>
        [BsonElement("loss_provisions_raw_t1")]
        public double? LossProvisionsRawT1 { get; set; }

        /// <summary>
        /// Main dependent variable:
        /// future loss provisions in t+1 scaled by total assets.
        /// </summary>
        [BsonElement("loss_provisions_t1")]
        public double? LossProvisionsT1 { get; set; }

        /// <summary>
        /// Optional macro control for robustness testing.
        /// Usually not needed if year fixed effects are included.
        /// </summary>
        [BsonElement("gdp_growth")]
        public double? GdpGrowth { get; set; }

        /// <summary>
        /// Optional macro control for robustness testing.
        /// </summary>
        [BsonElement("unemployment_rate")]
        public double? UnemploymentRate { get; set; }

        /// <summary>
        /// Optional macro control for robustness testing.
        /// </summary>
        [BsonElement("interest_rate")]
        public double? InterestRate { get; set; }

        /// <summary>
        /// Version of your text-scoring pipeline or prompt version.
        /// </summary>
        [BsonElement("text_model_version")]
        public string TextModelVersion { get; set; }

        /// <summary>
        /// Timestamp for data lineage and reprocessing.
        /// </summary>
        [BsonElement("updated_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
