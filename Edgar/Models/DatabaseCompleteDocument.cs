namespace Edgar.Models
{
    public class DatabaseCompleteDocument : DatabaseDocument
    {
        public int permno { get; set; }
        public int permco { get; set; }

        public ExtractedSections Sections { get; set; } = new();

        public decimal MarketValue { get; set; }

        public List<FirmTradingDay> TradingDays { get; set; } = new();

    }
}
