using Edgar.Config;
using Edgar.Pipeline;

namespace TestEdgar
{
    public class TestPanelBuilder
    {
        private readonly string _repoRoot =
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        // adjust ".." count if needed

        private readonly string _envPath;

        private readonly string _edgarRoot;

        public TestPanelBuilder()
        {
            _edgarRoot = Path.Combine(_repoRoot, "Edgar", "Edgar");
            _envPath = Path.Combine(_repoRoot, "Edgar", "Edgar", ".env"); // or Path.Combine(_edgarRoot, ".env")
        }

        [Fact]
        public async Task Test1()
        {
            // local db test (no env required)
            AppSettings settings = AppSettings.Load(_edgarRoot);
            var pipeline = new PanelBuilder(settings);
            await pipeline.RunAsync();
        }

        [Fact]
        public async Task TestWithRemoteDb()
        {
            DotNetEnv.Env.Load(_envPath);

            // sanity check so failures are obvious
            var host = Environment.GetEnvironmentVariable("DB_HOST");
            Assert.False(string.IsNullOrWhiteSpace(host), "DB_HOST was not loaded from .env");

            AppSettings settings = AppSettings.Load(_edgarRoot);
            var pipeline = new PanelBuilder(settings);
            await pipeline.RunAsync();
        }
    }
}

