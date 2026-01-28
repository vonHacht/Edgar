using System;

namespace Edgar.Models
{
    /// <summary>
    /// Output of the LLM-based, context-aware risk scoring step.
    /// </summary>
    public class LlmScore
    {
        /// <summary>
        /// Standardized numeric risk score you define (e.g., 0–100 or 0–1).
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Optional: model name/version used (for reproducibility).
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// Optional: free-text notes or brief rationale (avoid storing long rationales if you prefer).
        /// </summary>
        public string? Notes { get; set; }
    }
}
