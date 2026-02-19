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

        // ----------------------------
        // File paths
        // ----------------------------
        public string RiskPanelFilename { get; init; }
        public string CikMatchesFilename { get; init; }
        public string LogFilename { get; init; }
        public string BookToMarketFilename { get; init; }
        public string CcmFilename { get; init; }
        public string CrspFilename { get; init; }

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

        public bool LogToConsole { get; set; } = true;
        public bool LogToFile { get; set; } = true;
        public string LogLevel { get; set; } = "Verbose"; // Verbose, Debug, Information...

        // ----------------------------
        // Database options
        // ----------------------------
        public string DefaultLocalHost { get; init; } = "mongodb://localhost:27017";
        public string DefaultEdgarDbName { get; init; } = "edgar";
        public string DefaultEdgarLoggingDbName { get; init; } = "edgarLogging";
        public string DefaultEdgarMarketEquityDbName { get; init; } = "edgarMarketEquity";

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
            var outputDir = Path.Combine(projectRoot, "Output");
            var companiesDir = Path.Combine(dataDir, "companies");

            var settings = new AppSettings
            {
                ProjectRoot = projectRoot,
                DataDir = dataDir,
                RawDir = Path.Combine(dataDir, "raw"),
                ProcessedDir = Path.Combine(dataDir, "processed"),
                DictDir = Path.Combine(dataDir, "dictionaries"),
                OutputDir = outputDir,
                CompaniesDir = companiesDir,
                RiskPanelFilename = Path.Combine(outputDir, "risk_panel.csv"),
                CikMatchesFilename = Path.Combine(outputDir, "cik_matches.csv"),
                LogFilename = Path.Combine(outputDir, "edgar.log"),
                BookToMarketFilename = Path.Combine(companiesDir, "booktomarket.csv"),
                CcmFilename = Path.Combine(companiesDir, "ccm.csv"),
                CrspFilename = Path.Combine(companiesDir, "crsp.csv"),

                UserAgent = "Edgar/1.0 (contact: your.email@university.edu)"
            };

            settings.EnsureDirectories();
            settings.EnsureFiles();
            return settings;
        }

        private static string ResolveProjectRootFromBaseDirectory()
        {
#if DEBUG
            // bin/Debug/netX.Y → project root
            return Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..")
            );
#else
            // In Release, use where the program is executed from
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

