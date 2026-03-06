using Edgar.Import;

namespace TestEdgar
{
    public class TestBookToMarketImport
    {
        [Fact]
        public async Task TestCcmImport()
        {

            BookToMarketImporter importer = new BookToMarketImporter(Utilities.Settings);

            CcmImporter ccmImporter = new CcmImporter(Utilities.Settings);

            BookToMarketData result = importer.ReadAllBookToMarket();

            CikPermnoMap cikPermnoMap = ccmImporter.ReadAllYearsUniqueCcms();

            var cik = cikPermnoMap.GetCiks(10001);

            foreach (var c in cik)
            {
                var a = result.HaveCik(2009, c);

                Console.WriteLine(result);

            }

        }

        [Fact]
        public async Task TestImport()
        {

            BookToMarketImporter importer = new BookToMarketImporter(Utilities.Settings);

            BookToMarketData result = importer.ReadAllBookToMarket();

            Console.WriteLine(result);
        }
    }
}
