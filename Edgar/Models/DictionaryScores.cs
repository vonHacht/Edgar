namespace Edgar.Models
{
    public class DictionaryScores
    {
        public int TotalWords { get; set; }

        public int RiskCount { get; set; }
        public double RiskFrequency { get; set; }

        public int NegativeCount { get; set; }
        public double NegativeFrequency { get; set; }

        public int UncertaintyCount { get; set; }
        public double UncertaintyFrequency { get; set; }
    }
}
