using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Edgar.Parsing
{
    /// <summary>
    /// Converts EDGAR filing HTML into reasonably clean text suitable for section extraction.
    ///
    /// Notes:
    /// - This is a pragmatic cleaner (not perfect).
    /// - If you later want maximum accuracy, switch to HtmlAgilityPack and extract visible text nodes.
    /// </summary>
    public static class HtmlCleaner
    {
        private static readonly Regex ScriptBlock = new Regex(
            @"(?is)<script\b[^>]*>.*?</script>",
            RegexOptions.Compiled);

        private static readonly Regex StyleBlock = new Regex(
            @"(?is)<style\b[^>]*>.*?</style>",
            RegexOptions.Compiled);

        private static readonly Regex CommentBlock = new Regex(
            @"(?is)<!--.*?-->",
            RegexOptions.Compiled);

        private static readonly Regex TagRegex = new Regex(
            @"(?is)<[^>]+>",
            RegexOptions.Compiled);

        private static readonly Regex BrRegex = new Regex(
            @"(?i)<br\s*/?>",
            RegexOptions.Compiled);

        private static readonly Regex BlockEndRegex = new Regex(
            @"(?is)</(p|div|tr|li|h\d|table|section|article)\s*>",
            RegexOptions.Compiled);

        private static readonly Regex ExcessWhitespace = new Regex(
            @"[ \t\f\v]+",
            RegexOptions.Compiled);

        private static readonly Regex ExcessNewlines = new Regex(
            @"(\r?\n){3,}",
            RegexOptions.Compiled);

        public static string HtmlToText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            // Normalize line endings early
            html = html.Replace("\r\n", "\n").Replace("\r", "\n");

            // Remove scripts/styles/comments
            html = ScriptBlock.Replace(html, " ");
            html = StyleBlock.Replace(html, " ");
            html = CommentBlock.Replace(html, " ");

            // Add newlines around common structural boundaries BEFORE stripping tags
            html = BrRegex.Replace(html, "\n");
            html = BlockEndRegex.Replace(html, "\n\n");

            // Strip remaining tags
            html = TagRegex.Replace(html, " ");

            // Decode HTML entities (&amp; etc.)
            html = WebUtility.HtmlDecode(html);

            // Normalize whitespace: keep newlines, collapse excessive spaces
            html = NormalizeWhitespaceKeepNewlines(html);

            // Remove super-long runs of blank lines
            html = ExcessNewlines.Replace(html, "\n\n");

            return html.Trim();
        }

        private static string NormalizeWhitespaceKeepNewlines(string text)
        {
            // Collapse "horizontal" whitespace but preserve newline structure.
            // Then trim each line and drop empty trailing whitespace.
            text = ExcessWhitespace.Replace(text, " ");

            var lines = text.Split('\n')
                            .Select(l => l.Trim())
                            .ToArray();

            return string.Join("\n", lines);
        }
    }
}
