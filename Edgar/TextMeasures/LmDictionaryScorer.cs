using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Edgar.Models;

namespace Edgar.TextMeasures
{
    /// <summary>
    /// Computes dictionary-based textual measures (risk, negative, uncertainty)
    /// using Loughran-McDonald style word lists.
    ///
    /// Expected dictionary files in AppSettings.DictDir (Data/dictionaries):
    /// - risk.txt
    /// - negative.txt
    /// - uncertainty.txt
    ///
    /// One word per line. Case-insensitive.
    /// </summary>
    public class LmDictionaryScorer
    {
        private readonly HashSet<string> _risk;
        private readonly HashSet<string> _negative;
        private readonly HashSet<string> _uncertainty;

        // Tokenization: keep letters, remove punctuation/digits/underscores etc.
        private static readonly Regex TokenRegex = new Regex(@"[A-Za-z]+", RegexOptions.Compiled);

        public LmDictionaryScorer(string dictDir)
        {
            if (string.IsNullOrWhiteSpace(dictDir))
                throw new ArgumentException("Dictionary directory is required.", nameof(dictDir));

            _risk = LoadWordSet(Path.Combine(dictDir, "risk.txt"));
            _negative = LoadWordSet(Path.Combine(dictDir, "negative.txt"));
            _uncertainty = LoadWordSet(Path.Combine(dictDir, "uncertainty.txt"));

            if (_risk.Count == 0) throw new InvalidOperationException("risk.txt loaded 0 words (check path/format).");
            if (_negative.Count == 0) throw new InvalidOperationException("negative.txt loaded 0 words (check path/format).");
            if (_uncertainty.Count == 0) throw new InvalidOperationException("uncertainty.txt loaded 0 words (check path/format).");
        }

        public DictionaryScores Score(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new DictionaryScores
                {
                    TotalWords = 0,
                    RiskCount = 0,
                    RiskFrequency = 0.0,
                    NegativeCount = 0,
                    NegativeFrequency = 0.0,
                    UncertaintyCount = 0,
                    UncertaintyFrequency = 0.0
                };
            }

            int totalWords = 0;
            int riskCount = 0;
            int negCount = 0;
            int uncCount = 0;

            foreach (var token in Tokenize(text))
            {
                totalWords++;

                if (_riskNote(token)) riskCount++;
                if (_negNote(token)) negCount++;
                if (_uncNote(token)) uncCount++;
            }

            return new DictionaryScores
            {
                TotalWords = totalWords,

                RiskCount = riskCount,
                RiskFrequency = SafeFreq(riskCount, totalWords),

                NegativeCount = negCount,
                NegativeFrequency = SafeFreq(negCount, totalWords),

                UncertaintyCount = uncCount,
                UncertaintyFrequency = SafeFreq(uncCount, totalWords)
            };
        }

        private IEnumerable<string> Tokenize(string text)
        {
            // Yields uppercase tokens only (dictionary sets are uppercase).
            foreach (Match m in TokenRegex.Matches(text))
            {
                var token = m.Value;
                if (token.Length == 0) continue;

                // UpperInvariant avoids locale issues (e.g., Turkish i)
                yield return token.ToUpperInvariant();
            }
        }

        private bool _riskNote(string token) => _risk.Contains(token);
        private bool _negNote(string token) => _negative.Contains(token);
        private bool _uncNote(string token) => _uncertainty.Contains(token);

        private static double SafeFreq(int count, int totalWords)
        {
            if (totalWords <= 0) return 0.0;
            return (double)count / totalWords;
        }

        private static HashSet<string> LoadWordSet(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Dictionary file not found: {filePath}");

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in File.ReadLines(filePath))
            {
                var w = line.Trim();

                // ignore empty lines and comments
                if (w.Length == 0) continue;
                if (w.StartsWith("#", StringComparison.Ordinal)) continue;

                // standardize
                w = w.ToUpperInvariant();

                // Some LM lists include multiword tokens or punctuation; keep only A-Z for matching
                // (If you want to preserve hyphenated terms later, adjust this.)
                w = new string(w.Where(char.IsLetter).ToArray());
                if (w.Length == 0) continue;

                set.Add(w);
            }

            return set;
        }
    }
}
