using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Edgar.Models;

namespace Edgar.TextMeasures
{
    public sealed class LlmDictionaryScorer
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;
        private static readonly Regex WordRegex = new Regex(@"[A-Za-z]+", RegexOptions.Compiled);

        public LlmDictionaryScorer(string baseUrl = "http://localhost:11434", string model = "mistral")
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
            _model = model;
        }

        public async Task<DictionaryScores> ScoreAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new DictionaryScores();

            int totalWords = CountWords(text);

            string prompt = BuildPrompt(text, totalWords);

            var requestBody = new
            {
                model = _model,
                prompt = prompt,
                stream = false,
                format = "json"
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            using var outerDoc = JsonDocument.Parse(raw);
            string llmResponse = outerDoc.RootElement.GetProperty("response").GetString() ?? "{}";

            var result = ParseLlmScores(llmResponse, totalWords);
            result.Recalculate();

            return result;
        }

        private static int CountWords(string text)
        {
            return WordRegex.Matches(text).Count;
        }

        private static string BuildPrompt(string text, int totalWords)
        {
            return $"""
You are analyzing financial disclosure text from SEC EDGAR reports.

Your task is to estimate counts similar to a Loughran-McDonald dictionary scorer, but based on semantic meaning rather than exact dictionary matching.

Definitions:
- positive_words: words or phrases expressing favorable performance, confidence, strength, opportunity, improvement
- negative_words: words or phrases expressing weakness, loss, decline, threat, adverse outcomes
- uncertainty_words: words or phrases expressing ambiguity, unpredictability, risk, volatility, doubt, unknown outcomes

Important rules:
1. Return ONLY valid JSON.
2. Do not include markdown.
3. Use these exact fields:
   total_words
   positive_words
   negative_words
   uncertainty_words
4. total_words must be exactly {totalWords}.
5. The three category counts should be reasonable approximations based on the text.
6. Do not invent extra fields.

Text:
\"\"\"
{text}
\"\"\"
""";
        }

        private static DictionaryScores ParseLlmScores(string json, int fallbackTotalWords)
        {
            var scores = new DictionaryScores();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            scores.TotalWords = TryGetInt(root, "total_words", fallbackTotalWords);
            scores.PositiveWords = TryGetInt(root, "positive_words", 0);
            scores.NegativeWords = TryGetInt(root, "negative_words", 0);
            scores.UncertaintyWords = TryGetInt(root, "uncertainty_words", 0);

            return scores;
        }

        private static int TryGetInt(JsonElement root, string propertyName, int defaultValue)
        {
            if (root.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out int value))
                    return value;

                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out value))
                    return value;
            }

            return defaultValue;
        }
    }
}
