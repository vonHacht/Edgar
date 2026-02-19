using CsvHelper.Configuration;

namespace Edgar.Models
{
    public class BookToMarket
    {
        public string? LPERMNO { get; set; }
        public string? cik { get; set; }
        public string? LINKDT { get; set; }
        public string? LINKENDDT { get; set; }
        public string? GVKEY { get; set; }
        public DateTime? datadate { get; set; }
        public string? fyear { get; set; }

        public double? ceq { get; set; }
        public double? seq { get; set; }
        public double? txditc { get; set; }
        public double? pstkrv { get; set; }
        public double? pstkl { get; set; }
        public double? pstk { get; set; }
        public double? at { get; set; }
        public double? lt { get; set; }
    }

    public sealed class BookToMarketMap : ClassMap<BookToMarket>
    {
        public BookToMarketMap()
        {
            Map(m => m.LPERMNO).Name("LPERMNO");
            Map(m => m.cik).Name("cik");
            Map(m => m.LINKDT).Name("LINKDT");
            Map(m => m.LINKENDDT).Name("LINKENDDT");
            Map(m => m.GVKEY).Name("GVKEY");
            Map(m => m.datadate).Name("datadate");
            Map(m => m.fyear).Name("fyear");

            Map(m => m.ceq).Name("ceq");
            Map(m => m.seq).Name("seq");
            Map(m => m.txditc).Name("txditc");
            Map(m => m.pstkrv).Name("pstkrv");
            Map(m => m.pstkl).Name("pstkl");
            Map(m => m.pstk).Name("pstk");
            Map(m => m.at).Name("at");
            Map(m => m.lt).Name("lt");
        }
    }
}

