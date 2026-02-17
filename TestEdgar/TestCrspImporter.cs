using Edgar.Config;
using Edgar.Companies;

namespace TestEdgar
{
    public class TestCrspImporter
    {
        private readonly string _edgarRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Edgar");

        [Fact]
        public async Task TestImport()
        {
            AppSettings settings = AppSettings.Load(_edgarRoot);

            CrspImporter importer = new CrspImporter(settings);

            int permno = 84210;

            var result = importer.ReadByPermno(permno, "2020");

            Console.WriteLine(result);
        }
    }
}
