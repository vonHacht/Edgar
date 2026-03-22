using Edgar.Models;
using Edgar.TextMeasures;

namespace TestEdgar
{
    public class TestLLMDictionaryScorer
    {

        [Fact]
        public async Task TestScorer()
        {
            var scorer = new LlmDictionaryScorer(baseUrl: "http://localhost:11434", model: "mistral");

            var lmscorer = new LmDictionaryScorer(Utilities.Settings);

            string text = Utilities.PositiveInNegativeLmText;

            DictionaryScores scores = await scorer.ScoreAsync(text);

            DictionaryScores lmscores = lmscorer.Score(text);

            foreach (var score in new List<DictionaryScores> { scores, lmscores })
            {
                Console.WriteLine($"TotalWords: {scores.TotalWords}");
                Console.WriteLine($"PositiveWords: {scores.PositiveWords}");
                Console.WriteLine($"NegativeWords: {scores.NegativeWords}");
                Console.WriteLine($"UncertaintyWords: {scores.UncertaintyWords}");
                Console.WriteLine($"Sentiment: {scores.Sentiment}");
                Console.WriteLine($"UncertaintyScore: {scores.UncertaintyScore:F4}");
            }
        }
    }
}
