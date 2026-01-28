using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Edgar.Models;

namespace EDGARRiskPipeline.TextMeasures
{
    /// <summary>
    /// Stub implementation for an LLM-based, context-aware risk score.
    ///
    /// This class is intentionally implemented without any vendor-specific SDK calls,
    /// so your project compiles and runs before you plug in an API.
    ///
    /// Later: replace ScoreAsync's body with a real call to your chosen LLM provider,
    /// using a fixed prompt structure and returning a standardized numeric score.
    /// </summary>
    public class LlmRiskScorer
    {
        public string ModelName { get; }
        public int MaxCharsPerChunk { get; }

        public LlmRiskScorer(string modelName = "LLM-TBD", int maxCharsPerChunk = 12000)
        {
            ModelName = modelName;
            MaxCharsPerChunk = maxCharsPerChunk;
        }

        public async Task<LlmScore?> ScoreAsync(string item1aText, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(item1aText))
                return null;

            // If the text is long, chunk it deterministically (important for reproducibility).
            var chunks = ChunkText(item1aText, MaxCharsPerChunk).ToList();

            // ---- STUB BEHAVIOR ----
            // Until you integrate a real LLM call, we return a placeholder score.
            // This keeps the rest of your pipeline (download → extract → export) working.
            //
            // Replace this with:
            //  1) build prompt per chunk
            //  2) call LLM
            //  3) parse numeric score
            //  4) aggregate across chunks (mean/median/max)
            await Task.Yield();

            // Simple placeholder: longer risk sections often contain more risk discussion.
            // DO NOT use this for your thesis results — it's only for wiring/testing.
            var pseudo = Math.Min(100.0, Math.Log10(item1aText.Length + 1) * 25.0);

            return new LlmScore
            {
                Score = pseudo,
                Model = ModelName,
                Notes = $"STUB score from {chunks.Count} chunk(s). Replace with real LLM scoring."
            };
        }

        public string BuildPrompt(string textChunk)
        {
            // Fixed prompt template (example). Keep stable across runs for the thesis.
            // You can tighten the instructions and output format once you implement the real call.
            return
$@"You are scoring the severity and forward-looking content of risk disclosures.

Task:
Given the text from ITEM 1A (Risk Factors) of a firm's 10-K, produce a single numeric score.

Requirements:
- Output MUST be valid JSON with keys: score, notes
- score must be a number between 0 and 100
- Consider specificity, severity, and forward-looking nature; handle negation properly.

TEXT:
{textChunk}";
        }

        private static IEnumerable<string> ChunkText(string text, int maxChars)
        {
            if (maxChars <= 0) throw new ArgumentOutOfRangeException(nameof(maxChars));

            text = text.Trim();
            if (text.Length <= maxChars)
            {
                yield return text;
                yield break;
            }

            int i = 0;
            while (i < text.Length)
            {
                int len = Math.Min(maxChars, text.Length - i);

                // Try to break on a whitespace boundary to avoid splitting words.
                int end = i + len;
                if (end < text.Length)
                {
                    int back = end;
                    while (back > i && !char.IsWhiteSpace(text[back - 1]))
                        back--;

                    // If we found a reasonable break, use it; otherwise hard split.
                    if (back > i + (maxChars * 0.6))
                        end = back;
                }

                yield return text.Substring(i, end - i).Trim();
                i = end;
            }
        }
    }
}
