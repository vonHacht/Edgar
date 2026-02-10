using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using CsvHelper.TypeConversion;

namespace Edgar.Converters 
{
    public sealed class SafeNullableIntConverter : Int32Converter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            text = text.Trim();

            if (text.Length == 1 && char.IsLetter(text[0])) return null;
            if (text is "." or "NA" or "N/A" or "NULL") return null;

            if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                return v;

            return null;
        }
    }
}
