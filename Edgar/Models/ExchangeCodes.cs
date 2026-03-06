namespace Edgar.Models
{
    using System.ComponentModel;

    public enum ExchangeCodes
    {
        UNKOWN = 0,

        [Description("NYSE (New York Stock Exchange)")]
        NYSE = 1,

        [Description("AMEX / American Stock Exchange")]
        AMEX = 2,

        [Description("NASDAQ")]
        NASDAQ = 3,

        [Description("Arca (NYSE Arca)")]
        ARCA = 4,

        [Description("Boston Stock Exchange")]
        Boston = 5,

        [Description("National Stock Exchange")]
        National = 6,

        [Description("Chicago Stock Exchange")]
        Chicago = 7,

        [Description("Philadelphia Stock Exchange")]
        Philadelphia = 8,

        [Description("NASDAQ Small Cap / other NASDAQ segments (older datasets)")]
        NASDAQSmallCap = 9
    }
}
