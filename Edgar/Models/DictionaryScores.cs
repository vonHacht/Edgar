namespace Edgar.Models
{
    public class DictionaryScores
    {
        public int TotalWords { get; set; } = 0;

        public int PositiveWords { get; set; } = 0;
        public int NegativeWords { get; set; } = 0;

        public int UncertaintyWords { get; set; } = 0;

        public int Sentiment => PositiveWords - NegativeWords;

        public int UncertaintyScore => (TotalWords > 0) ? UncertaintyWords / TotalWords : 0;
    }
}
