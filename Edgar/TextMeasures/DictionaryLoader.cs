using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Edgar.Models;

namespace EDGARRiskPipeline.TextMeasures
{
    /// <summary>
    /// Loads Loughran–McDonald style word lists from disk into HashSets.
    ///
    /// Expected files (one word per line):
    /// - risk.txt
    /// - negative.txt
    /// - uncertainty.txt
    /// </summary>
    public static class DictionaryLoader
    {
        public static LmDictionaries LoadLm(string dictDir)
        {
            if (string.IsNullOrWhiteSpace(dictDir))
                throw new ArgumentException("Dictionary directory is required.", nameof(dictDir));

            var riskPath = Path.Combine(dictDir, "risk.txt");
            var negPath = Path.Combine(dictDir, "negative.txt");
            var uncPath = Path.Combine(dictDir, "uncertainty.txt");

            var dicts = new LmDictionaries
            {
                Risk = LoadWordSet(riskPath),
                Negative = LoadWordSet(negPath),
                Uncertainty = LoadWordSet(uncPath)
            };

            if (dicts.Risk.Count == 0) throw new InvalidOperationException($"risk.txt loaded 0 words (check {riskPath}).");
            if (dicts.Negative.Count == 0) throw new InvalidOperationException($"negative.txt loaded 0 words (check {negPath}).");
            if (dicts.Uncertainty.Count == 0) throw new InvalidOperationException($"uncertainty.txt loaded 0 words (check {uncPath}).");

            return dicts;
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

                // Keep letters only for robust matching against tokenizer [A-Za-z]+
                w = new string(w.Where(char.IsLetter).ToArray());
                if (w.Length == 0) continue;

                set.Add(w);
            }

            return set;
        }
    }
}
