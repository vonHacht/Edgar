using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Edgar.Converters
{
    public sealed class SafeNullableDateTimeConverter : DateTimeConverter
    {
        private static readonly string[] Formats =
        {
        "yyyy-MM-dd",
        "yyyyMMdd",
        "MM/dd/yyyy"
    };

        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            text = text.Trim();

            if (text is "." or "NA" or "N/A" or "NULL") return null;

            if (DateTime.TryParseExact(text, Formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
                return dt;

            // fallback parse
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt;

            return null;
        }
    }
}
