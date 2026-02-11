using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Edgar.Converters
{
    public sealed class NullableDoubleOrNullConverter : DoubleConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            text = text.Trim();

            // Treat letter flags like "C" as missing numeric values
            if (text.Length == 1 && char.IsLetter(text[0]))
                return null;

            // Sometimes data uses "NA", "N/A", etc.
            if (text.Equals("NA", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                return null;

            // Normal numeric parsing
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                return v;

            // Fall back to null rather than throwing
            return null;
        }
    }
}
