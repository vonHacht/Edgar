using Edgar.Config;
using Edgar.Import;

namespace TestEdgar
{
    public class TestDictionaryImporter
    {
        [Fact]
        public async Task TestImport()
        {
            AppSettings settings = AppSettings.Load(Utilities.EdgarRoot);

            DictionaryImporter importer = new DictionaryImporter(settings);

            var result = importer.ReadAllDictionaries();

            Console.WriteLine(result);
        }
    }
}
