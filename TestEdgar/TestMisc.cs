namespace TestEdgar
{
    public class TestMisc
    {
        private readonly string _repoRoot =
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        // adjust ".." count if needed

        private readonly string _envPath;

        public TestMisc()
        {
            _envPath = Path.Combine(_repoRoot, "Edgar", "Edgar", ".env");
        }

        [Fact]
        public async Task TestEnvPath()
        {
            DotNetEnv.Env.Load(_envPath);

            var dataFolder = Environment.GetEnvironmentVariable("DATA_FOLDER");

            System.Console.WriteLine(dataFolder);

            var realPath = Path.GetFullPath(dataFolder);

            System.Console.WriteLine(realPath);




        }
    }
}

