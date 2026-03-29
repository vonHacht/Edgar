namespace Edgar.Utilities
{
    public static class Maths
    {
        private const int DefaultDecimals = 4;

        public static double Round(double value, int decimals = DefaultDecimals)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0.0;

            return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        }

        public static decimal Round(decimal value, int decimals = DefaultDecimals)
        {
            return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        }
    }
}
