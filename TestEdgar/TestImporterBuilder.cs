using Edgar.Config;
using Edgar.Pipeline;

namespace TestEdgar
{
    public class TestImporterBuilder
    {
        private readonly string _edgarRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Edgar");

        [Fact]
        public async Task TestLoadDataToMemory()
        {
            AppSettings settings = AppSettings.Load(_edgarRoot);

            ImporterBuilder importer = new ImporterBuilder(settings);

            //importer.LoadDataToMemory();
        }

        [Fact]
        public async Task TestEnsureConsistentData()
        {
            AppSettings settings = AppSettings.Load(_edgarRoot);

            ImporterBuilder importer = new ImporterBuilder(settings);

            //importer.LoadDataToMemory();

            //importer.EnsureConsistentData();
        }
    }
}
