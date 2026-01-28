namespace Edgar.Models
{
    public class Firm
    {
        /// <summary>
        /// 10-digit zero-padded CIK string, e.g. "0000320193".
        /// </summary>
        public string Cik10 { get; set; } = string.Empty;
            
        public string? Ticker { get; set; }
        public string? Name { get; set; }

        // If you later merge with Compustat:
        public string? Gvkey { get; set; }
    }
}
