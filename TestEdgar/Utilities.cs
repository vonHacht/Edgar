using Edgar.Config;

namespace TestEdgar
{
    public class Utilities
    {
        public static readonly string EdgarRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Edgar");

        public static readonly AppSettings Settings = AppSettings.Load(EdgarRoot);

        public static readonly string[] ArgsCheck = new[] { "check" };

        public static readonly string EasyLmText = """
The company faces significant uncertainty regarding future demand, regulatory developments,
and market volatility. Management expects some improvement in margins, but adverse conditions
may continue to affect earnings.
""";
        public static readonly string NegationLmText = """
The company does not expect any significant losses and sees no material uncertainty going forward.
""";

        public static readonly string PositiveInNegativeLmText = """
Despite strong revenue growth, the firm experienced a severe deterioration in cash flow and increasing financial distress.
""";

        public static readonly string SubtleLmText = """
The firm recognized a one-time impairment charge, which management believes does not reflect ongoing performance.
""";




    }
}
