using Edgar.Models;

namespace Edgar.Utilities
{
    public static class CliParser
    {
        public static CliOptions Parse(string[] args)
        {
            var dict = args
                .Select(arg => arg.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

            var options = new CliOptions();

            if (dict.TryGetValue("skipDb", out var skipDbValue) &&
                bool.TryParse(skipDbValue, out var skipDb))
            {
                options.SkipDb = skipDb;
            }

            if (dict.TryGetValue("extractFromFile", out var extractFromFileValue) &&
               bool.TryParse(extractFromFileValue, out var extractFromFile))
            {
                options.extractFromFile = extractFromFile;
            }

            if (dict.TryGetValue("projectRoot", out var projectRoot))
            {
                options.ProjectRoot = projectRoot;
            }

            return options;
        }
    }
}
