using System.Globalization;

using CsvHelper.Configuration;

using Edgar.Converters;

namespace Edgar.Models
{
    public class CrspCssRecord
    {
        public int PERMNO { get; set; }
        public DateTime date { get; set; }

        public string? TICKER { get; set; }
        public string? COMNAM { get; set; }
        public string? SHRCLS { get; set; }
        public string? TSYMBOL { get; set; }
        public string? TRDSTAT { get; set; }
        public string? SECSTAT { get; set; }

        public int? PERMCO { get; set; }
        public string? CUSIP { get; set; }

        public int? DLSTCD { get; set; }
        public DateTime? NEXTDT { get; set; }

        public string? SHRFLG { get; set; }
        public int? ACPERM { get; set; }
        public int? ACCOMP { get; set; }
        public DateTime? SHRENDDT { get; set; }
        public int? NWPERM { get; set; }

        public double? DLRETX { get; set; }
        public double? DLPRC { get; set; }
        public double? DLRET { get; set; }

        public double? BIDLO { get; set; }
        public double? ASKHI { get; set; }
        public double? PRC { get; set; }
        public double? VOL { get; set; }
        public double? RET { get; set; }

        public double? BID { get; set; }
        public double? ASK { get; set; }
        public double? SHROUT { get; set; }
        public double? OPENPRC { get; set; }
        public int? NUMTRD { get; set; }

        public double? RETX { get; set; }
        public double? vwretd { get; set; }
        public double? vwretx { get; set; }
        public double? ewretd { get; set; }
        public double? ewretx { get; set; }
        public double? sprtrn { get; set; }
    }

    public sealed class CrspCssRecordMap : ClassMap<CrspCssRecord>
    {
        public CrspCssRecordMap()
        {
            AutoMap(CultureInfo.InvariantCulture);

            // date fields
            Map(m => m.date).TypeConverter<SafeNullableDateTimeConverter>();  // but date isn't nullable in your model!
            Map(m => m.NEXTDT).TypeConverter<SafeNullableDateTimeConverter>();
            Map(m => m.SHRENDDT).TypeConverter<SafeNullableDateTimeConverter>();

            // int fields
            Map(m => m.PERMNO); // keep strict (should always parse)
            Map(m => m.PERMCO).TypeConverter<SafeNullableIntConverter>();
            Map(m => m.DLSTCD).TypeConverter<SafeNullableIntConverter>();
            Map(m => m.ACPERM).TypeConverter<SafeNullableIntConverter>();
            Map(m => m.ACCOMP).TypeConverter<SafeNullableIntConverter>();
            Map(m => m.NWPERM).TypeConverter<SafeNullableIntConverter>();
            Map(m => m.NUMTRD).TypeConverter<SafeNullableIntConverter>();

            // double fields (apply across the board)
            Map(m => m.DLRETX).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.DLPRC).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.DLRET).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.BIDLO).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.ASKHI).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.PRC).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.VOL).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.RET).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.BID).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.ASK).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.SHROUT).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.OPENPRC).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.RETX).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.vwretd).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.vwretx).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.ewretd).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.ewretx).TypeConverter<SafeNullableDoubleConverter>();
            Map(m => m.sprtrn).TypeConverter<SafeNullableDoubleConverter>();
        }
    }
}


