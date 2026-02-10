using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using CsvHelper.TypeConversion;

namespace Edgar.Converters 
{
    public sealed class SafeNullableDoubleConverter : DoubleConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            text = text.Trim();

            // treat single-letter codes & common null markers as null
            if (text.Length == 1 && char.IsLetter(text[0])) return null;

            if (text is "." or "NA" or "N/A" or "NULL") return null;

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                return v;

            return null; // safegate
        }
    }
}
