using System.Diagnostics;

using Edgar.Companies;
using Edgar.Config;
using Edgar.Filter;

namespace TestEdgar
{
    public class TestIsStockPriceBelow3OnDayBeforeFiling
    {
        private readonly string _edgarRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Edgar");

        [Fact]
        public async Task Test()
        {
            AppSettings settings = AppSettings.Load(_edgarRoot);

            CrspImporter importer = new CrspImporter(settings);

            int permno = 84210;

            var result = importer.ReadByPermno(permno, "2020");

            Console.WriteLine(result);

            DateTime filingDate = result[4].Date;

            Debug.Assert(Filter.IsStockPriceBelow3OnDayBeforeFiling(filingDate, result) == false);
        }
    }
}
