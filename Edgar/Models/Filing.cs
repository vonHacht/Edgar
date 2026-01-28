using System;

namespace Edgar.Models
{
    public class Filing
    {
        public string Cik10 { get; set; } = string.Empty;

        /// <summary>
        /// With dashes, e.g. "0000320193-23-000106"
        /// </summary>
        public string AccessionNumber { get; set; } = string.Empty;

        public DateTime FilingDate { get; set; }

        /// <summary>
        /// Primary doc file name, e.g. "a10-k20230930.htm"
        /// </summary>
        public string PrimaryDocument { get; set; } = string.Empty;

        /// <summary>
        /// Optional: "2019-09-30" or similar if you parse it.
        /// </summary>
        public DateTime? PeriodOfReport { get; set; }

        public string Form { get; set; } = "10-K";

        // Convenience computed properties (no external dependencies)
        public string AccessionNoNoDashes => AccessionNumber.Replace("-", "");
        public string CikNoLeadingZeros => Cik10.TrimStart('0');
    }
}
