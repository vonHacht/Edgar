using Edgar.Models;

namespace Edgar.Utilities
{
    public class Paths
    {
        public static PathParts SplitPath(string path)
        {
            string normalized = path.Replace('/', Path.DirectorySeparatorChar);

            return new PathParts
            {
                Directory = Path.GetDirectoryName(normalized)!,
                FileName = Path.GetFileName(normalized)
            };
        }
    }
}
