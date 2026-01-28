using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Edgar.Models;

namespace Edgar.Export
{
    public class CsvExporter
    {
        private static readonly CultureInfo CsvCulture = CultureInfo.InvariantCulture;
        
        private static string Delimiter = ";";

        public async Task WriteAsync(IEnumerable<PanelRow> rows, string outputPath, bool overwrite = true)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));

            var list = rows.ToList();
            if (list.Count == 0)
                return;

            var writeHeader = overwrite || !File.Exists(outputPath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using var stream = new FileStream(
                outputPath,
                overwrite ? FileMode.Create : FileMode.Append,
                FileAccess.Write,
                FileShare.Read
            );

            using var writer = new StreamWriter(stream, Encoding.UTF8);

            if (writeHeader)
                await writer.WriteLineAsync(BuildHeader());

            foreach (var row in list)
                await writer.WriteLineAsync(BuildRow(row));
        }

        private static string BuildHeader()
        {
            return string.Join(Delimiter,
                "cik",
                "ticker",
                "year",
                "filing_date",
                "accession_number",

                "item1a_word_count",

                "risk_count",
                "risk_freq",

                "negative_count",
                "negative_freq",

                "uncertainty_count",
                "uncertainty_freq",

                "llm_risk_score"
            );
        }

        private static string BuildRow(PanelRow r)
        {
            return string.Join(Delimiter,
                Esc(r.Cik10),
                Esc(r.Ticker),
                r.Year.ToString(CsvCulture),
                r.FilingDate.ToString("yyyy-MM-dd", CsvCulture),
                Esc(r.AccessionNumber),

                r.Item1AWordCount.ToString(CsvCulture),

                r.RiskCount.ToString(CsvCulture),
                r.RiskFrequency.ToString("G", CsvCulture),

                r.NegativeCount.ToString(CsvCulture),
                r.NegativeFrequency.ToString("G", CsvCulture),

                r.UncertaintyCount.ToString(CsvCulture),
                r.UncertaintyFrequency.ToString("G", CsvCulture),

                r.LlmRiskScore?.ToString("G", CsvCulture) ?? ""
            );
        }

        /// <summary>
        /// Escapes CSV fields according to RFC 4180.
        /// </summary>
        private static string Esc(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var needsQuotes =
                value.Contains(',') ||
                value.Contains('"') ||
                value.Contains('\n') ||
                value.Contains('\r');

            if (!needsQuotes)
                return value;

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}

