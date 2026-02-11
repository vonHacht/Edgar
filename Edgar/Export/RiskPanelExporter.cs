using System.Globalization;

using Edgar.Models;

namespace Edgar.Export
{
    public class RiskPanelExporter : CsvExporter<PanelRow>
    {
        private static readonly CultureInfo CsvCulture = CultureInfo.InvariantCulture;

        private static string Delimiter = ";";

        public override string BuildHeader()
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

        public override string BuildRow(PanelRow r)
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
    }
}
