using Edgar.Config;
using Edgar.Pipeline;

namespace TestEdgar
{
    public class TestEdgarBuilder
    {
        private readonly string _edgarRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Edgar");

        [Fact]
        public async Task Test()
        {
            AppSettings settings = AppSettings.Load(_edgarRoot);

            EdgarBuilder edgarBuilder = new EdgarBuilder(settings);

            var result = await edgarBuilder.DownloadFilingsForYear(2020);

            Console.WriteLine(result);
        }
    }
}
