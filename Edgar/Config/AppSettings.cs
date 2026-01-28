using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

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

        // ----------------------------
        // Sample period
        // ----------------------------
        public int StartYear { get; init; } = 2010;
        public int EndYear { get; init; } = 2023;

        // ----------------------------
        // SEC / EDGAR settings
        // ----------------------------
        public string UserAgent { get; init; }
        public int RequestDelayMs { get; init; } = 200; // be polite to SEC

        // ----------------------------
        // Extraction options
        // ----------------------------
        public bool ExtractItem7 { get; init; } = false;
        public bool OverwriteRawFiles { get; init; } = false;

        // ----------------------------
        // Quality thresholds
        // ----------------------------
        public int MinItem1AWordCount { get; init; } = 200;

        private AppSettings() { }

        public static AppSettings Load()
        {
            // Assume project is run from /bin/Debug/... → go up to project root
            var projectRoot = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..")
            );

            var dataDir = Path.Combine(projectRoot, "Data");

            var settings = new AppSettings
            {
                ProjectRoot = projectRoot,
                DataDir = dataDir,
                RawDir = Path.Combine(dataDir, "raw"),
                ProcessedDir = Path.Combine(dataDir, "processed"),
                DictDir = Path.Combine(dataDir, "dictionaries"),
                OutputDir = Path.Combine(projectRoot, "output"),

                // IMPORTANT: replace with your real contact info
                UserAgent = "Edgar/1.0 (contact: your.email@university.edu)"
            };

            settings.EnsureDirectories();
            return settings;
        }

        private void EnsureDirectories()
        {
            Directory.CreateDirectory(DataDir);
            Directory.CreateDirectory(RawDir);
            Directory.CreateDirectory(ProcessedDir);
            Directory.CreateDirectory(DictDir);
            Directory.CreateDirectory(OutputDir);
        }
    }
}

