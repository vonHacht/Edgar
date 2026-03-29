using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Edgar.Parsing
{
    /// <summary>
    /// Pragmatic EDGAR HTML-to-text cleaner.
    ///
    /// Goals:
    /// - Preserve section headings like "Item 1A. Risk Factors"
    /// - Preserve enough line structure for downstream extraction
    /// - Remove obvious markup/script/style noise
    /// - Normalize HTML entities and Unicode whitespace
    ///
    /// This is still regex-based, not a full DOM parser.
    /// For maximum fidelity, use HtmlAgilityPack or AngleSharp later.
    /// </summary>
    public static class HtmlCleaner
    {
        private static readonly Regex ScriptBlock = new(
            @"(?is)<script\b[^>]*>.*?</script>",
            RegexOptions.Compiled);

        private static readonly Regex StyleBlock = new(
            @"(?is)<style\b[^>]*>.*?</style>",
            RegexOptions.Compiled);

        private static readonly Regex CommentBlock = new(
            @"(?is)<!--.*?-->",
            RegexOptions.Compiled);

        private static readonly Regex HeadBlock = new(
            @"(?is)<head\b[^>]*>.*?</head>",
            RegexOptions.Compiled);

        private static readonly Regex XmlLikeBlock = new(
            @"(?is)<\?.*?\?>|<!DOCTYPE.*?>",
            RegexOptions.Compiled);

        private static readonly Regex BrTag = new(
            @"(?is)<br\s*/?>",
            RegexOptions.Compiled);

        // Add paragraph-ish spacing before stripping tags.
        private static readonly Regex DoubleBreakTags = new(
            @"(?is)</?(?:p|div|section|article|header|footer|aside|blockquote|pre|tr|table|ul|ol|li|h[1-6]|hr)\b[^>]*>",
            RegexOptions.Compiled);

        // Table cells usually want separation, but not necessarily blank lines.
        private static readonly Regex SingleBreakTags = new(
            @"(?is)</?(?:td|th)\b[^>]*>",
            RegexOptions.Compiled);

        private static readonly Regex TagRegex = new(
            @"(?is)<[^>]+>",
            RegexOptions.Compiled);

        // Remove SEC SGML-ish wrappers if present.
        private static readonly Regex EdgarWrapperTags = new(
            @"(?im)^\s*</?(?:SEC-DOCUMENT|SEC-HEADER|DOCUMENT|TYPE|SEQUENCE|FILENAME|DESCRIPTION|TEXT)\b[^>]*>\s*$",
            RegexOptions.Compiled);

        // Remove leftover angle-bracket fragments that sometimes survive malformed HTML.
        private static readonly Regex BrokenTagFragments = new(
            @"(?m)^[<>/\s]*$",
            RegexOptions.Compiled);

        private static readonly Regex HorizontalWhitespace = new(
            @"[ \t\f\v\u00A0\u2000-\u200B\u202F\u205F\u3000]+",
            RegexOptions.Compiled);

        private static readonly Regex ExcessBlankLines = new(
            @"\n{3,}",
            RegexOptions.Compiled);

        // Optional: collapse page number only lines like "6", "7", etc. if isolated.
        private static readonly Regex StandalonePageNumber = new(
            @"(?m)^\s{0,3}\d{1,4}\s{0,3}$",
            RegexOptions.Compiled);

        // Optional: remove repeated "Table of Contents" nav lines that appear alone.
        private static readonly Regex StandaloneTocLine = new(
            @"(?im)^\s*table\s+of\s+contents\s*$",
            RegexOptions.Compiled);

        public static string HtmlToText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            // Normalize line endings first.
            html = html.Replace("\r\n", "\n").Replace("\r", "\n");

            // Remove obvious non-content blocks.
            html = XmlLikeBlock.Replace(html, " ");
            html = HeadBlock.Replace(html, " ");
            html = ScriptBlock.Replace(html, " ");
            html = StyleBlock.Replace(html, " ");
            html = CommentBlock.Replace(html, " ");

            // Decode entities before whitespace normalization.
            html = WebUtility.HtmlDecode(html);

            // Normalize important HTML-decoded Unicode chars explicitly.
            html = NormalizeUnicode(html);

            // Preserve structure before stripping tags.
            html = BrTag.Replace(html, "\n");
            html = DoubleBreakTags.Replace(html, "\n\n");
            html = SingleBreakTags.Replace(html, "\n");

            // Strip remaining tags.
            html = TagRegex.Replace(html, " ");

            // Remove EDGAR wrapper markers if they became plain text lines.
            html = EdgarWrapperTags.Replace(html, " ");

            // Clean weird leftovers.
            html = BrokenTagFragments.Replace(html, " ");

            // Normalize whitespace while preserving line structure.
            html = NormalizeWhitespaceKeepNewlines(html);

            // Optional cleanup of obvious navigation/page artifacts.
            html = RemoveLowValueNoiseLines(html);

            // Final blank-line cleanup.
            html = ExcessBlankLines.Replace(html, "\n\n");

            return html.Trim();
        }

        private static string NormalizeUnicode(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace('\u00A0', ' ')   // NBSP
                .Replace('\u00AD', '-')   // soft hyphen; sometimes better than dropping
                .Replace('\u2010', '-')   // hyphen
                .Replace('\u2011', '-')   // non-breaking hyphen
                .Replace('\u2012', '-')   // figure dash
                .Replace('\u2013', '-')   // en dash
                .Replace('\u2014', '-')   // em dash
                .Replace('\u2018', '\'')
                .Replace('\u2019', '\'')
                .Replace('\u201C', '"')
                .Replace('\u201D', '"')
                .Replace("\f", "\n");     // form feed often marks page breaks
        }

        private static string NormalizeWhitespaceKeepNewlines(string text)
        {
            var lines = text
                .Split('\n')
                .Select(line => HorizontalWhitespace.Replace(line, " ").Trim());

            var sb = new StringBuilder();
            bool previousBlank = false;

            foreach (var line in lines)
            {
                var isBlank = string.IsNullOrWhiteSpace(line);

                if (isBlank)
                {
                    if (!previousBlank)
                    {
                        sb.AppendLine();
                        previousBlank = true;
                    }

                    continue;
                }

                sb.AppendLine(line);
                previousBlank = false;
            }

            return sb.ToString().Trim();
        }

        private static string RemoveLowValueNoiseLines(string text)
        {
            var lines = text.Split('\n');
            var cleaned = new List<string>(lines.Length);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                if (line.Length == 0)
                {
                    cleaned.Add(string.Empty);
                    continue;
                }

                // Repeated nav artifact common in SEC filings.
                if (StandaloneTocLine.IsMatch(line))
                    continue;

                // Very conservative page-number removal:
                // only drop if surrounded by blank lines.
                if (StandalonePageNumber.IsMatch(line))
                {
                    var prevBlank = i == 0 || string.IsNullOrWhiteSpace(lines[i - 1]);
                    var nextBlank = i == lines.Length - 1 || string.IsNullOrWhiteSpace(lines[i + 1]);

                    if (prevBlank && nextBlank)
                        continue;
                }

                cleaned.Add(line);
            }

            return string.Join("\n", cleaned);
        }
    }
}
