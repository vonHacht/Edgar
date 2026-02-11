namespace Edgar.Config
{
    public class AppSettings
    {
        // ----------------------------
        // Project paths
        // ----------------------------
        public string ProjectRoot { get; init; }
        public string DataDir { get; init; }
        public string RawDir { get; init; }
        public string ProcessedDir { get; init; }
        public string DictDir { get; init; }
        public string OutputDir { get; init; }
        public string CompaniesDir { get; init; }
        public string MappingsDir { get; init; }

        // ----------------------------
        // Sample period
        // ----------------------------
        public int StartYear { get; init; } = 2010;
        public int EndYear { get; init; } = 2023;

        // ----------------------------
        // SEC / EDGAR settings
        // ----------------------------
        public string UserAgent { get; init; }
        public int RequestDelayMs { get; init; } = 200;

        // ----------------------------
        // Extraction options
        // ----------------------------
        public bool ExtractItem7 { get; init; } = false;
        public bool OverwriteRawFiles { get; init; } = false;

        // ----------------------------
        // Quality thresholds
        // ----------------------------
        public int MinItem1AWordCount { get; init; } = 200;

        // ✅ Production-friendly entry point
        public static AppSettings Load()
        {
            var projectRoot = ResolveProjectRootFromBaseDirectory();
            return Load(projectRoot);
        }

        // ✅ Test-friendly / explicit entry point
        public static AppSettings Load(string projectRoot)
        {
            projectRoot = Path.GetFullPath(projectRoot);
            var dataDir = Path.Combine(projectRoot, "Data");

            var settings = new AppSettings
            {
                ProjectRoot = projectRoot,
                DataDir = dataDir,
                RawDir = Path.Combine(dataDir, "raw"),
                ProcessedDir = Path.Combine(dataDir, "processed"),
                DictDir = Path.Combine(dataDir, "dictionaries"),
                OutputDir = Path.Combine(projectRoot, "output"),
                CompaniesDir = Path.Combine(dataDir, "companies"),
                //MappingsDir = Path.Combine(dataDir, "mappings"),

                UserAgent = "Edgar/1.0 (contact: your.email@university.edu)"
            };

            settings.EnsureDirectories();
            return settings;
        }

        private static string ResolveProjectRootFromBaseDirectory()
        {
            // bin/Debug/netX.Y → project root
            return Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..")
            );
        }

        private void EnsureDirectories()
        {
            Directory.CreateDirectory(DataDir);
            Directory.CreateDirectory(RawDir);
            Directory.CreateDirectory(ProcessedDir);
            Directory.CreateDirectory(DictDir);
            Directory.CreateDirectory(OutputDir);
            Directory.CreateDirectory(CompaniesDir);
            //Directory.CreateDirectory(MappingsDir);
        }
    }
}

