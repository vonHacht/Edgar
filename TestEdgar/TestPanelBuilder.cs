using Edgar.Config;
using Edgar.Pipeline;

namespace TestEdgar
{
    public class TestPanelBuilder
    {
        private readonly string _edgarRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Edgar");

        [Fact]
        public async Task Test1()
        {
            AppSettings settings = AppSettings.Load(_edgarRoot);
            var pipeline = new PanelBuilder(settings);
            await pipeline.RunAsync();
        }
    }
}
