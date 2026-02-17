using Edgar.Models;

namespace Edgar.Utilities
{
    public class Accession
    {
        public static string GetAccessionFromFilename(string filepath, bool withoutdashes = false)
        {
            PathParts pp = Paths.SplitPath(filepath);
            string accessionNumber = Path.GetFileNameWithoutExtension(pp.FileName);

            if (withoutdashes)
            {
                accessionNumber = GetAccessionWithoutDashes(accessionNumber);
            }

            return accessionNumber;
        }

        public static string GetAccessionWithoutDashes(string fileName)
            => Path.GetFileNameWithoutExtension(fileName).Replace("-", "");

        public static string GetAccessionFile(string filepath)
            => Paths.SplitPath(filepath).FileName;
    }
}
