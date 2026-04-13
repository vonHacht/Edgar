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
        public string ?Id { get; set; }

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
        /// Standard Industrial Classification code.
        /// </summary>
        [BsonElement("sic")]
        public required int Sic { get; set; }

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

        //[BsonElement("llm_scores_item1A")]
        //public required LLmScores LlmScoresItem1A { get; set; }

        //[BsonElement("llm_scores_item7")]
        //public required LLmScores LlmScoresItem7 { get; set; }

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
        [BsonElement("prior_return")]
        public required decimal PriorReturn { get; set; }

        [BsonElement("event_period_excess_return")]
        public required decimal EventPeriodExcessReturn { get; set; }

        /// <summary>
        /// Optional stock return volatility.
        /// </summary>
        [BsonElement("volatility")]
        public required decimal Volatility { get; set; }

        [BsonElement("realized_variance")]
        public required decimal RealizedVariance { get; set; }

        [BsonElement("realized_variance_days_after")]
        public required decimal RealizedVarianceDaysAfter { get; set; }

        [BsonElement("turnover")]
        public required decimal Turnover { get; set; }

        [BsonElement("turnover_avg")]
        public required decimal TurnoverAvg { get; set; }

        [BsonElement("filing_day_return")]
        public required decimal FilingDayReturn { get; set; }

        /// <summary>
        /// Optional leverage control.
        /// </summary>
        [BsonElement("leverage")]
        public required double Leverage { get; set; }

        /// <summary>
        /// Total assets in fiscal year t.
        /// Compustat mnemonic: AT
        /// </summary>
        [BsonElement("total_assets")]
        public required double TotalAssets { get; set; }

        /// <summary>
        /// Net income.
        /// Compustat mnemonic: NI
        /// </summary>
        [BsonElement("net_income")]
        public required double NetIncome { get; set; }

        /// <summary>
        /// Special items.
        /// Compustat mnemonic: SPI
        /// </summary>
        [BsonElement("special_items")]
        public required double SpecialItems { get; set; }

        /// <summary>
        /// Common equity.
        /// Compustat mnemonic: CEQ
        /// </summary>
        [BsonElement("common_equity")]
        public required double CommonEquity { get; set; }

        /// <summary>
        /// Long-term debt.
        /// Compustat mnemonic: DLTT
        /// </summary>
        [BsonElement("long_term_debt")]
        public required double LongTermDebt { get; set; }

        /// <summary>
        /// Pre-tax income.
        /// Compustat mnemonic: PI
        /// </summary>
        [BsonElement("pre_tax_income")]
        public required double PreTaxIncome { get; set; }

        /// <summary>
        /// Loan loss provisions.
        /// Compustat mnemonic: PLL
        /// </summary>
        [BsonElement("loan_loss_provisions")]
        public required double LoanLossProvisions { get; set; }

        /// <summary>
        /// Net charge-offs.
        /// Compustat mnemonic: NCO
        /// </summary>
        [BsonElement("net_charge_offs")]
        public required double NetChargeOffs { get; set; }

        /// <summary>
        /// Non-performing assets.
        /// Compustat mnemonic: NPAT
        /// </summary>
        [BsonElement("non_performing_assets")]
        public required double NonPerformingAssets { get; set; }

        /// <summary>
        /// Tier 1 capital ratio.
        /// Compustat Bank mnemonic: CAPR1
        /// </summary>
        [BsonElement("tier1_capital_ratio")]
        public required double Tier1CapitalRatio { get; set; }

        /// <summary>
        /// Loan loss reserves, component I.
        /// Compustat mnemonic: LLRCI
        /// </summary>
        [BsonElement("loan_loss_reserves_I")]
        public required double LoanLossReservesI { get; set; }

        /// <summary>
        /// Loan loss reserves, component R.
        /// Compustat mnemonic: LLRCR
        /// </summary>
        [BsonElement("loan_loss_reserves_R")]
        public required double LoanLossReservesR { get; set; }

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
        /// Timestamp for data lineage and reprocessing.
        /// </summary>
        [BsonElement("updated_at")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
