using Edgar.Models;
using System.Globalization;
using System.Text;

namespace Edgar.Export
{
    public class CIKMatchExporter : CsvExporter<Firm>
    {
        private int _rowCount = 0;
        private int _numOfMatches = 0;

        public CIKMatchExporter() : base(endcontent: true) {}

        private static readonly CultureInfo CsvCulture = CultureInfo.InvariantCulture;

        private static string Delimiter = ";";

        public override string BuildHeader()
        {
            return string.Join(Delimiter,
                "cik",
                "name",
                "ticker",
                "found"
            );
        }

        public override string BuildRow(Firm f)
        {
            return string.Join(Delimiter,
                Esc(f.CIK),
                Esc(f.LatestCompanyName),
                Esc(f.Ticker),
                Esc(Filter.Filter.MatchingCIK(f).ToString())
            );
        }

        protected override void OnBeforeWrite(IReadOnlyList<Firm> list)
        {
            _rowCount = list.Count;

            _numOfMatches = list.ToList().Count(f => Filter.Filter.MatchingCIK(f));
        }

        public override string EndContent()
        {
            var sb = new StringBuilder();

            sb.AppendLine("# ---");
            sb.AppendLine("# SUMMARY");
            sb.AppendLine($"# row_count;{_rowCount}");
            sb.AppendLine($"# number_of_matches;{_numOfMatches}");

            return sb.ToString().TrimEnd();
        }

    }
}
