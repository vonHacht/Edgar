using System.Globalization;
using System.Text;

using Edgar.Models;

namespace Edgar.Export
{
    public class FilterExporter : CsvExporter<Firm>
    {
        private int _rowCount = 0;
        private int _numOfDelisted = 0;
        private int _numOfNonCIKMatches = 0;


        public FilterExporter() : base(endcontent: true) { }

        private static readonly CultureInfo CsvCulture = CultureInfo.InvariantCulture;

        private static string Delimiter = ";";

        public override string BuildHeader()
        {
            return string.Join(Delimiter,
                "cik",
                "name",
                "ticker",
                "cikmatch",
                "delisted"
            );
        }

        public override string BuildRow(Firm f)
        {
            return string.Join(Delimiter,
                Esc(f.CIK),
                Esc(f.LatestCompanyName),
                Esc(f.Ticker),
                Esc(Filter.Filter.NonMatchingCIK(f).ToString()),
                Esc(Filter.Filter.Delisted(f).ToString())
            );
        }

        protected override void OnBeforeWrite(IReadOnlyList<Firm> list)
        {
            _rowCount = list.Count;

            _numOfDelisted = list.ToList().Count(f => Filter.Filter.Delisted(f));

            _numOfNonCIKMatches = list.ToList().Count(f => Filter.Filter.NonMatchingCIK(f));
        }

        public override string EndContent()
        {
            var sb = new StringBuilder();

            sb.AppendLine("# ---");
            sb.AppendLine("# SUMMARY");

            AppendSummaryLine(sb, "number_of_companies", "total firms processed", _rowCount);
            AppendSummaryLine(sb, "number_of_delisted", "firms removed due to delisting", _numOfDelisted);
            AppendSummaryLine(sb, "number_of_non_cik_matches", "firms with no trading days", _numOfNonCIKMatches);
            AppendSummaryLine(
                sb,
                "after_filtering",
                "firms remaining after all filters",
                _rowCount - _numOfNonCIKMatches - _numOfDelisted
            );

            return sb.ToString().TrimEnd();
        }

        private static void AppendSummaryLine(
            StringBuilder sb,
            string key,
            string description,
            int value)
        {
            sb.AppendLine($"# {key}; {description}");
            sb.AppendLine($"# {key};{value}");
        }
    }
}
