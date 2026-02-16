namespace Edgar.Models
{
    public class Ccm
    {
        /// <summary>
        /// Link permno (CRSP Permanent Security Number).
        /// 
        /// LPERMNO identifies the specific CRSP security (stock) linked to a
        /// Compustat firm (GVKEY) during a valid link period.
        /// 
        /// - permno is security-level (not firm-level).
        /// - It uniquely identifies a traded stock.
        /// - It remains stable over time even if ticker or CUSIP changes.
        /// 
        /// Use this when merging Compustat data with CRSP return data.
        /// </summary>
        public int? permno { get; set; }

        /// <summary>
        /// Link permco (CRSP Permanent Company Number).
        /// 
        /// LPERMCO identifies the CRSP company entity linked to a Compustat firm.
        /// 
        /// - permco is company-level.
        /// - It groups together multiple PERMNOs (e.g., different share classes).
        /// - It remains stable over time.
        /// 
        /// Use this when aggregating at the firm/company level rather than
        /// at the individual security level.
        /// </summary>
        public int? permco { get; set; }
    }
}

