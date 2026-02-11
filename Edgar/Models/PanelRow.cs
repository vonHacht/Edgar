namespace Edgar.Models
{
    /// <summary>
    /// One firm-filing observation (can be collapsed to firm-year later).
    /// Extended to support sample-selection filters similar to "10-K Sample Creation" tables.
    /// </summary>
    public class PanelRow
    {
        // ----------------------------
        // Identifiers
        // ----------------------------
        public string Cik10 { get; set; } = string.Empty;
        public string? Ticker { get; set; }

        /// <summary>
        /// CRSP identifier (if you link to CRSP).
        /// </summary>
        public int? Permno { get; set; }

        /// <summary>
        /// Compustat identifier (optional, if you link to Compustat).
        /// </summary>
        public string? Gvkey { get; set; }

        // ----------------------------
        // Filing metadata
        // ----------------------------
        public int Year { get; set; }
        public DateTime FilingDate { get; set; }

        /// <summary>
        /// Optional: Period of report / fiscal year end date (if available from submissions JSON).
        /// Useful for aligning accounting data.
        /// </summary>
        public DateTime? PeriodOfReport { get; set; }

        public string AccessionNumber { get; set; } = string.Empty;

        // ----------------------------
        // Text extraction metadata
        // ----------------------------
        public bool FoundItem1A { get; set; } = true;
        public int Item1AWordCount { get; set; }

        /// <summary>
        /// Full filing word count (cleaned text) - used for filters like "Number of words in 10-K >= 2,000".
        /// </summary>
        public int DocWordCount { get; set; }

        /// <summary>
        /// Optional: MD&A (Item 7) extracted word count for filters like "MD&A section >= 250 words".
        /// Null means not extracted or not found.
        /// </summary>
        public int? Item7WordCount { get; set; }

        /// <summary>
        /// Optional: helpful for debugging extraction quality (e.g., TOC false positive).
        /// </summary>
        public bool LooksLikeTocHit { get; set; }

        /// <summary>
        /// Optional: store where the raw doc is cached (helps debugging).
        /// </summary>
        public string? LocalHtmlPath { get; set; }

        // ----------------------------
        // Dictionary scores (LM)
        // ----------------------------
        public int RiskCount { get; set; }
        public double RiskFrequency { get; set; }

        public int NegativeCount { get; set; }
        public double NegativeFrequency { get; set; }

        public int UncertaintyCount { get; set; }
        public double UncertaintyFrequency { get; set; }

        // ----------------------------
        // LLM score (optional)
        // ----------------------------
        public double? LlmRiskScore { get; set; }

        // ----------------------------
        // Market data fields (CRSP-style filters)
        // ----------------------------
        /// <summary>
        /// Market capitalization on (or near) filing date. Units depend on your source.
        /// </summary>
        public double? MarketCap { get; set; }

        /// <summary>
        /// Price on trading day -1 relative to filing date (commonly used for price >= $3 filter).
        /// </summary>
        public double? PriceMinus1 { get; set; }

        /// <summary>
        /// Exchange code / listing category if you have it (NYSE/AMEX/NASDAQ).
        /// </summary>
        public string? Exchange { get; set; }

        /// <summary>
        /// If you need "ordinary common equity" filter.
        /// </summary>
        public bool? IsOrdinaryCommonEquity { get; set; }

        /// <summary>
        /// Event window availability indicator (e.g., returns/volume for days 0–3).
        /// Set this after merging CRSP returns data.
        /// </summary>
        public bool? HasEventWindowReturnsVolume { get; set; }

        /// <summary>
        /// Availability of sufficient returns/volume history (e.g., >=60 days around the filing date).
        /// </summary>
        public bool? HasSufficientReturnsVolumeHistory { get; set; }

        // ----------------------------
        // Accounting fields (Compustat-style filters)
        // ----------------------------
        public double? BookValue { get; set; }
        public double? BookToMarket { get; set; }

        // ----------------------------
        // CUSIP (if you have it, useful for linking to other datasets or applying CUSIP-based filters).
        // ----------------------------
        public string? Cusip8 { get; set; }   // e.g., "03783310"
        public string? Cusip9 { get; set; }   // e.g., "037833100" (optional)


        // ----------------------------
        // Helpful computed properties for filtering
        // ----------------------------

        /// <summary>
        /// MD&A identified (Item 7 found).
        /// </summary>
        public bool HasMda => Item7WordCount.HasValue && Item7WordCount.Value > 0;

        /// <summary>
        /// MD&A passes minimum length threshold.
        /// </summary>
        public bool MdaAtLeast(int minWords) => Item7WordCount.HasValue && Item7WordCount.Value >= minWords;

        /// <summary>
        /// Full 10-K text length passes a threshold.
        /// </summary>
        public bool DocAtLeast(int minWords) => DocWordCount >= minWords;

        /// <summary>
        /// Item 1A text length passes a threshold.
        /// </summary>
        public bool Item1AAtLeast(int minWords) => Item1AWordCount >= minWords;
    }
}
