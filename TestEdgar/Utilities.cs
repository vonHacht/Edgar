using Edgar.Config;

namespace TestEdgar
{
    public class Utilities
    {
        public static readonly string EdgarRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Edgar");

        public static readonly AppSettings Settings = AppSettings.Load(EdgarRoot);

        public static readonly string[] ArgsCheck = new[] { "check" };
    }
}
