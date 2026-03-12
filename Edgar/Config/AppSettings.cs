using DotNetEnv;

namespace Edgar.Config
{
    public class AppSettings
    {
        // ----------------------------
        // Project paths
        // ----------------------------
        public required string ProjectRoot { get; init; }
        public required string DataDir { get; init; }
        public required string RawDir { get; init; }
        public required string ProcessedDir { get; init; }
        public required string DictDir { get; init; }
        public required string OutputDir { get; init; }
        public required string CompaniesDir { get; init; }

        // ----------------------------
        // File paths
        // ----------------------------
        public required string RiskPanelFilename { get; init; }
        public required string CikMatchesFilename { get; init; }

        public string LogFilename =>
                    $"{_logFilenameWithoutTimestamp}_{DateTime.Now:yyyyMMdd_HHmmss}.log";

        private string _logFilenameWithoutTimestamp = String.Empty;
        public required string BookToMarketFilename { get; init; }
        public required string CcmFilename { get; init; }
        public required string CrspFilename { get; init; }
        public required string LoughranMcDonaldMasterDictionaryFilename { get; init; }

        // ----------------------------
        // SEC / EDGAR settings
        // ----------------------------
        public required string UserAgent { get; init; }
        public int RequestDelayMs { get; init; } = 200;

        // ----------------------------
        // Extraction options
        // ----------------------------
        public bool ExtractItem7 { get; init; } = false;
        public bool OverwriteRawFiles { get; init; } = false;

        public bool LogToConsole { get; set; } = true;
        public bool LogToFile { get; set; } = true;
        public string LogLevel { get; set; } = "Verbose";

        // ----------------------------
        // Database options
        // ----------------------------
        public string DefaultLocalHost { get; init; } = "";
        public string DefaultEdgarDbName { get; init; } = "";

        public static AppSettings Load()
        {
            var projectRoot = ResolveProjectRootFromBaseDirectory();
            return Load(projectRoot);
        }

        public static AppSettings Load(string projectRoot)
        {
            projectRoot = Path.GetFullPath(projectRoot);

            LoadEnvFile(projectRoot);

            var dataDir = Path.Combine(projectRoot, "Data");
            var outputDir = Path.Combine(projectRoot, "Output");
            var companiesDir = Path.Combine(dataDir, "companies");
            var dictDir = Path.Combine(dataDir, "dictionaries");

            var settings = new AppSettings
            {
                ProjectRoot = projectRoot,
                DataDir = dataDir,
                RawDir = Path.Combine(dataDir, "raw"),
                ProcessedDir = Path.Combine(dataDir, "processed"),
                DictDir = dictDir,
                OutputDir = outputDir,
                CompaniesDir = companiesDir,

                RiskPanelFilename = Path.Combine(outputDir, "risk_panel.csv"),
                CikMatchesFilename = Path.Combine(outputDir, "cik_matches.csv"),
                LoughranMcDonaldMasterDictionaryFilename = Path.Combine(dictDir, "Loughran-McDonald_MasterDictionary_1993-2024.csv"),
                _logFilenameWithoutTimestamp = Path.Combine(outputDir, "edgar"),

                BookToMarketFilename = Path.Combine(companiesDir, "booktomarket.csv"),
                CcmFilename = Path.Combine(companiesDir, "ccm.csv"),
                CrspFilename = Path.Combine(companiesDir, "crsp.csv"),

                UserAgent = "Edgar/1.0 (contact: your.email@university.edu)",

                DefaultLocalHost = GetRequiredEnvironmentVariable("DEFAULT_LOCAL_HOST"),
                DefaultEdgarDbName = GetRequiredEnvironmentVariable("DEFAULT_EDGAR_DB_NAME")
            };

            settings.EnsureDirectories();
            settings.EnsureFiles();

            return settings;
        }

        private static void LoadEnvFile(string projectRoot)
        {
            var envPath = Path.Combine(projectRoot, ".env");

            if (File.Exists(envPath))
            {
                Env.Load(envPath);
            }
        }

        private static string GetRequiredEnvironmentVariable(string key)
        {
            var value = Environment.GetEnvironmentVariable(key);

            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Missing required environment variable: {key}");

            return value;
        }

        private static string ResolveProjectRootFromBaseDirectory()
        {
#if DEBUG
            return Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..")
            );
#else
            return Directory.GetCurrentDirectory();
#endif
        }

        private void EnsureDirectories()
        {
            Directory.CreateDirectory(DataDir);
            Directory.CreateDirectory(RawDir);
            Directory.CreateDirectory(ProcessedDir);
            Directory.CreateDirectory(DictDir);
            Directory.CreateDirectory(OutputDir);
            Directory.CreateDirectory(CompaniesDir);
        }

        private void EnsureFiles()
        {
            EnsureFileExists(BookToMarketFilename);
            EnsureFileExists(CcmFilename);
            EnsureFileExists(CrspFilename);
        }

        private static void EnsureFileExists(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Required file not found: {path}", path);
        }
    }
}
