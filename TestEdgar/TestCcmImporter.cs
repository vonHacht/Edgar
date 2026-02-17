using Edgar.Config;
using Edgar.Companies;
using Edgar.Import;

namespace TestEdgar
{
    public class TestCcmImporter
    {
        private readonly string _edgarRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Edgar");

        [Fact]
        public async Task TestImport()
        {
            AppSettings settings = AppSettings.Load(_edgarRoot);

            CcmImporter importer = new CcmImporter(settings);

            string cik = "0001069878";

            var result = importer.ReadByCik(cik, "2020");

            Console.WriteLine(result);
        }
    }
}
