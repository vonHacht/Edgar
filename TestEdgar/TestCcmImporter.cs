using Edgar.Config;
using Edgar.Import;

namespace TestEdgar
{
    public class TestCcmImporter
    {
        private readonly string _edgarRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Edgar");

        [Fact]
        public async Task TestReadAllYearsUniqueCcms()
        {
            AppSettings settings = AppSettings.Load(_edgarRoot);

            CcmImporter importer = new CcmImporter(settings);

            var result = importer.ReadAllYearsUniqueCcms();

            Console.WriteLine(result);
        }
    }
}
