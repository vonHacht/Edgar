using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Edgar.Models;

namespace Edgar.TextMeasures
{
    public sealed class LlmScorer
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;
        private readonly string _keepAlive;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private const string PromptPrefix =
"""
YOU MUST RETURN EXACTLY ONE JSON OBJECT AND NOTHING ELSE.
No markdown. No explanations. No text before '{' or after '}'.
All string values MUST be enclosed in double quotes.

You are evaluating the expected SHORT-TERM STOCK RETURN IMPACT of a corporate disclosure, as perceived by investors at the time of filing.

Interpret the paragraph as new information entering the market and assess its likely effect on returns over the next trading days.

Score meaning:
- 0.0–0.2 = strongly positive (favorable news, upside expected)
- 0.2–0.4 = moderately positive
- 0.4–0.6 = neutral or mixed impact
- 0.6–0.8 = moderately negative
- 0.8–1.0 = strongly negative (adverse news, downside risk)

Focus on:
- direction and magnitude of new information
- whether risks are already mitigated or still material
- implications for future cash flows, liquidity, or uncertainty

If you cannot comply, return exactly:
{"risk_score": 0.5, "rationale": "", "confidence": "low"}

The input contains approximately
""";

        private const string PromptSuffix =
"""

Return ONLY this JSON:
{
  "risk_score": <number between 0.0 and 1.0>,
  "rationale": "<ONE sentence (10-30 words): key information + investor interpretation + expected return impact>",
  "confidence": "<low|medium|high>"
}
""";

        public LlmScorer(
            HttpClient? httpClient = null,
            string baseUrl = "http://localhost:11434",
            string model = "mistral",
            string keepAlive = "30m")
        {
            _httpClient = httpClient ?? new HttpClient
            {
                BaseAddress = new Uri(baseUrl, UriKind.Absolute)
            };

            if (_httpClient.BaseAddress is null)
            {
                _httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            }

            if (_httpClient.DefaultRequestHeaders.Accept.Count == 0)
            {
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            }

            _model = model;
            _keepAlive = keepAlive;
        }

        public async Task<LLmScores> ScoreAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return DefaultScores();
            }

            int totalWords = CountWords(text);
            string prompt = BuildPrompt(text, totalWords);

            var request = new GenerateRequest
            {
                Model = _model,
                Prompt = prompt,
                Stream = false,
                Format = "json",
                KeepAlive = _keepAlive
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/generate")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };

            using var response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var outerDoc = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

            string llmResponse =
                outerDoc.RootElement.TryGetProperty("response", out var responseProp)
                    ? responseProp.GetString() ?? "{}"
                    : "{}";

            return TryParseLlmScores(llmResponse);
        }

        private static LLmScores DefaultScores() =>
            new()
            {
                RiskScore = 0.5,
                Rationale = string.Empty,
                Confidence = ConfidenceEnum.low
            };

        private static int CountWords(string text)
        {
            int count = 0;
            bool inWord = false;

            foreach (char c in text)
            {
                bool isLetter = (uint)((c | 0x20) - 'a') <= ('z' - 'a');

                if (isLetter)
                {
                    if (!inWord)
                    {
                        count++;
                        inWord = true;
                    }
                }
                else
                {
                    inWord = false;
                }
            }

            return count;
        }

        private static string BuildPrompt(string text, int totalWords)
        {
            var sb = new StringBuilder(PromptPrefix.Length + PromptSuffix.Length + text.Length + 64);

            sb.Append(PromptPrefix);
            sb.Append(totalWords);
            sb.Append(" words.\n\nInput:\n");
            sb.Append(text);
            sb.Append(PromptSuffix);

            return sb.ToString();
        }

        private static LLmScores TryParseLlmScores(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                double riskScore = TryGetDouble(root, "risk_score", 0.5);
                string rationale = TryGetString(root, "rationale", string.Empty);
                string confidenceRaw = TryGetString(root, "confidence", "low");

                return new LLmScores
                {
                    RiskScore = Clamp01(riskScore),
                    Rationale = rationale,
                    Confidence = ParseConfidence(confidenceRaw)
                };
            }
            catch (JsonException)
            {
                return DefaultScores();
            }
        }

        private static double TryGetDouble(JsonElement root, string propertyName, double defaultValue)
        {
            if (!root.TryGetProperty(propertyName, out var prop))
                return defaultValue;

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out double value))
                return value;

            if (prop.ValueKind == JsonValueKind.String &&
                double.TryParse(
                    prop.GetString(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out value))
            {
                return value;
            }

            return defaultValue;
        }

        private static string TryGetString(JsonElement root, string propertyName, string defaultValue)
        {
            if (!root.TryGetProperty(propertyName, out var prop))
                return defaultValue;

            return prop.ValueKind == JsonValueKind.String
                ? prop.GetString() ?? defaultValue
                : defaultValue;
        }

        private static ConfidenceEnum ParseConfidence(string? confidence) =>
            confidence?.Trim().ToLowerInvariant() switch
            {
                "high" => ConfidenceEnum.high,
                "medium" => ConfidenceEnum.medium,
                _ => ConfidenceEnum.low
            };

        private static double Clamp01(double value)
        {
            if (value < 0.0) return 0.0;
            if (value > 1.0) return 1.0;
            return value;
        }

        private sealed class GenerateRequest
        {
            public string Model { get; init; } = string.Empty;
            public string Prompt { get; init; } = string.Empty;
            public bool Stream { get; init; }
            public string Format { get; init; } = "json";
            public string KeepAlive { get; init; } = "30m";
        }
    }
}
