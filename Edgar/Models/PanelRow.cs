using System;

namespace Edgar.Models
{
    /// <summary>
    /// One firm-filing observation (you can aggregate to firm-year later if needed).
    /// This is what you export to CSV for regression workflows.
    /// </summary>
    public class PanelRow
    {
        public string Cik10 { get; set; } = string.Empty;
        public string? Ticker { get; set; }

        public int Year { get; set; }
        public DateTime FilingDate { get; set; }

        public string AccessionNumber { get; set; } = string.Empty;

        // Extraction metadata
        public int Item1AWordCount { get; set; }
        public bool FoundItem1A { get; set; } = true;

        // Dictionary scores (LM)
        public int RiskCount { get; set; }
        public double RiskFrequency { get; set; }

        public int NegativeCount { get; set; }
        public double NegativeFrequency { get; set; }

        public int UncertaintyCount { get; set; }
        public double UncertaintyFrequency { get; set; }

        // Optional: LLM score placeholder (null until implemented)
        public double? LlmRiskScore { get; set; }

        // Optional: store where the raw doc is cached (helps debugging)
        public string? LocalHtmlPath { get; set; }
    }
}
