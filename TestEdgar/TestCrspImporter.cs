using Edgar.Companies;
using Edgar.Config;

namespace TestEdgar
{
    public class TestCrspImporter
    {
        private readonly string _edgarRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Edgar");

        [Fact]
        public async Task TestReadAllCrsp()
        {
            AppSettings settings = AppSettings.Load(_edgarRoot);

            CrspImporter importer = new CrspImporter(settings);

            var result = importer.ReadAllCrsp();

            Console.WriteLine(result);

            var averageTurnover = result.AverageTurnover;


        }
    }
}
