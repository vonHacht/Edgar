using System;
using System.Linq;
using System.Text.RegularExpressions;

using Edgar.Models;


namespace Edgar.Parsing
{
    /// <summary>
    /// Extracts Item 1A (Risk Factors) and (optionally) Item 7 (MD&A) from cleaned filing text.
    ///
    /// Assumes input is already reasonably "text-like" (HTML removed / normalized).
    /// Works via robust regex start/end markers + heuristics to avoid Table-of-Contents hits.
    /// </summary>
    public class ItemSectionExtractor
    {
        // Item 1A start: try to match "Item 1A" with optional punctuation and optional "Risk Factors"
        private static readonly Regex Item1AStart = new Regex(
            @"(?is)\bitem\s*1a\b\s*[\.\-:–—]?\s*(risk\s*factors)?\b",
            RegexOptions.Compiled);

        // Item 1A end: usually Item 1B, Item 2, or Part II (fallback)
        private static readonly Regex Item1AEnd = new Regex(
            @"(?is)\bitem\s*1b\b|\bitem\s*2\b|\bpart\s*ii\b",
            RegexOptions.Compiled);

        // Item 7 start: "Item 7" with optional "Management's Discussion..."
        private static readonly Regex Item7Start = new Regex(
            @"(?is)\bitem\s*7\b\s*[\.\-:–—]?\s*(management'?s\s*discussion\s*and\s*analysis)?\b",
            RegexOptions.Compiled);

        // Item 7 end: usually Item 7A or Item 8
        private static readonly Regex Item7End = new Regex(
            @"(?is)\bitem\s*7a\b|\bitem\s*8\b",
            RegexOptions.Compiled);

        // For TOC detection: "Table of Contents" and/or a high density of "Item X" lines near the match
        private static readonly Regex TocPhrase = new Regex(
            @"(?is)table\s+of\s+contents",
            RegexOptions.Compiled);

        private static readonly Regex ItemHeadingAny = new Regex(
            @"(?is)\bitem\s*\d+\s*[a-z]?\b",
            RegexOptions.Compiled);

        public ExtractedSections Extract(string cleanedText, bool extractItem7)
        {
            if (string.IsNullOrWhiteSpace(cleanedText))
            {
                return new ExtractedSections
                {
                    FoundItem1A = false,
                    FoundItem7 = false,
                    WordCountItem1A = 0,
                    WordCountItem7 = 0,
                    ExtractionMethodVersion = "v1"
                };
            }

            var result = new ExtractedSections
            {
                ExtractionMethodVersion = "v1"
            };

            // ITEM 1A
            var item1a = ExtractSection(
                cleanedText,
                Item1AStart,
                Item1AEnd,
                out bool likelyTocHit);

            result.LooksLikeTocHit = likelyTocHit;

            if (!string.IsNullOrWhiteSpace(item1a))
            {
                result.Item1AText = item1a;
                result.FoundItem1A = true;
                result.WordCountItem1A = CountWords(item1a);
            }
            else
            {
                result.FoundItem1A = false;
                result.WordCountItem1A = 0;
            }

            // ITEM 7 (optional)
            if (extractItem7)
            {
                var item7 = ExtractSection(cleanedText, Item7Start, Item7End, out _);

                if (!string.IsNullOrWhiteSpace(item7))
                {
                    result.Item7Text = item7;
                    result.FoundItem7 = true;
                    result.WordCountItem7 = CountWords(item7);
                }
                else
                {
                    result.FoundItem7 = false;
                    result.WordCountItem7 = 0;
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts a section between startRegex and endRegex.
        /// Uses heuristics to skip TOC-like matches.
        /// </summary>
        private static string? ExtractSection(
            string text,
            Regex startRegex,
            Regex endRegex,
            out bool likelyTocHit)
        {
            likelyTocHit = false;

            // Find all candidate starts
            var starts = startRegex.Matches(text);
            if (starts.Count == 0)
                return null;

            // Choose the best start:
            // - skip TOC-like starts (often near the beginning, or near "Table of Contents")
            // - prefer a start that yields a reasonable-sized section
            Match? chosenStart = null;

            foreach (Match s in starts)
            {
                if (!s.Success) continue;

                if (IsLikelyTocStart(text, s.Index))
                {
                    likelyTocHit = true;
                    continue;
                }

                // Find an end after this start
                var end = endRegex.Match(text, s.Index + s.Length);
                if (!end.Success) continue;

                var len = end.Index - s.Index;
                if (len <= 0) continue;

                // Require some minimal size to avoid accidental tiny captures
                if (len < 500) continue;

                chosenStart = s;
                break;
            }

            // Fallback: if everything looked like TOC, try the last start match (often real body starts later)
            if (chosenStart == null)
            {
                chosenStart = starts[starts.Count - 1];
                if (!chosenStart.Success) return null;
            }

            var chosenEnd = endRegex.Match(text, chosenStart.Index + chosenStart.Length);
            if (!chosenEnd.Success)
                return null;

            var section = text.Substring(chosenStart.Index, chosenEnd.Index - chosenStart.Index);

            // Final cleanup: trim excessive whitespace
            section = NormalizeWhitespace(section);

            return section;
        }

        private static bool IsLikelyTocStart(string text, int startIndex)
        {
            // Heuristic 1: very early "Item ..." (TOC often in first ~10–15% of doc)
            if (startIndex < Math.Min(25000, text.Length / 8))
            {
                // If near "Table of Contents", it's almost certainly TOC
                if (WindowContains(text, startIndex, 3000, TocPhrase))
                    return true;

                // If there are many "Item X" headings nearby, likely TOC
                var window = GetWindow(text, startIndex, 4000);
                var itemHeadingCount = ItemHeadingAny.Matches(window).Count;
                if (itemHeadingCount >= 6)
                    return true;
            }

            // Heuristic 2: if the nearby window has lots of dot leaders "....." (common in TOC)
            var win2 = GetWindow(text, startIndex, 2500);
            var dotLeaders = win2.Count(c => c == '.');
            if (dotLeaders > 300) // coarse signal; adjust if needed
                return true;

            return false;
        }

        private static bool WindowContains(string text, int index, int radius, Regex pattern)
        {
            var w = GetWindow(text, index, radius);
            return pattern.IsMatch(w);
        }

        private static string GetWindow(string text, int center, int radius)
        {
            var start = Math.Max(0, center - radius);
            var end = Math.Min(text.Length, center + radius);
            return text.Substring(start, end - start);
        }

        private static int CountWords(string text)
        {
            // Split on whitespace; remove empties
            return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static string NormalizeWhitespace(string s)
        {
            // Collapse whitespace but keep basic readability.
            // If you prefer keeping line breaks, adjust this to preserve \n.
            return Regex.Replace(s, @"\s+", " ").Trim();
        }
    }
}