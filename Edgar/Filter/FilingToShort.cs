namespace Edgar.Filter
{
    public partial class Filter
    {
        private static readonly int minLengthOfFiling = 2000;

        public static bool FilingToShort(string filingText)
        {
            return filingText.Count() < minLengthOfFiling;
        }
    }
}
