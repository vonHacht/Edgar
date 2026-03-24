using System.Text.RegularExpressions;

using Edgar.Config;
using Edgar.Import;
using Edgar.Models;

namespace Edgar.TextMeasures
{
    public class LmDictionaryScorer
    {
        private LmDictionaries lmDictionaries;

        private static readonly Regex TokenRegex = new Regex(@"[A-Za-z]+", RegexOptions.Compiled);

        public LmDictionaryScorer(AppSettings appSettings)
        {
            lmDictionaries = new DictionaryImporter(appSettings).ReadAllDictionaries();
        }

        public DictionaryScores Score(string text)
        {
            DictionaryScores dictionaryScores = new DictionaryScores();

            foreach (var token in Tokenize(text))
            {
                dictionaryScores.TotalWords++;

                if (lmDictionaries.Uncertainty.Contains(token)) dictionaryScores.UncertaintyWords++;
                if (lmDictionaries.Positive.Contains(token)) dictionaryScores.PositiveWords++;
                if (lmDictionaries.Negative.Contains(token)) dictionaryScores.NegativeWords++;
            }

            dictionaryScores.Recalculate();
            return dictionaryScores;
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
    }
}
