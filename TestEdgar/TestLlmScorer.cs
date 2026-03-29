using Edgar.Models;
using Edgar.TextMeasures;

namespace TestEdgar
{
    public class TestLlmScorer
    {

        [Fact]
        public async Task TestScorer()
        {
            var scorer = new LlmScorer(baseUrl: "http://localhost:11434", model: "mistral");

            foreach (var text in new List<string> {
                Utilities.EasyLmText,
                Utilities.NegationLmText,
                Utilities.PositiveInNegativeLmText,
                Utilities.SubtleLmText
            }) 
            {
                LLmScores scores = await scorer.ScoreAsync(text);

                Console.WriteLine($"{text}");
                Console.WriteLine($"Confidence: {scores.Confidence}");
                Console.WriteLine($"Rationale: {scores.Rationale}");
                Console.WriteLine($"Risk score: {scores.RiskScore}");
                Console.WriteLine();
            }
        }
    }
}
