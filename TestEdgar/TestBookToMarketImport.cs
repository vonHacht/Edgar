using Edgar.Config;
using Edgar.Import;

namespace TestEdgar
{
    public class TestBookToMarketImport
    {
        private readonly string _edgarRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Edgar");

        [Fact]
        public async Task TestImport()
        {
            AppSettings settings = AppSettings.Load(_edgarRoot);

            BookToMarketImporter importer = new BookToMarketImporter(settings);

            string cik = "0000001750";

            DateTime date = new DateTime(2021, 05, 31);

            var result = importer.ReadByCik(cik, date);

            Console.WriteLine(result);
        }
    }
}
