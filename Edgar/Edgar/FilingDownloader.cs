using System.Text;

using Edgar.Config;
using Edgar.Models;

using Edgar.Utilities;

namespace Edgar.Edgar
{
    /// <summary>
    /// Downloads and caches the primary document for a filing from EDGAR Archives.
    /// Saves to: Data/raw/{cik}/{accessionNoNoDashes}/{primaryDocument}
    /// </summary>
    public class FilingDownloader
    {
        private readonly EdgarClient _client;
        private readonly AppSettings _settings;

        public FilingDownloader(EdgarClient client, AppSettings settings)
        {
            _client = client;
            _settings = settings;
        }

        /// <summary>
        /// Returns the local file path to the primary doc HTML. Downloads it if not cached.
        /// </summary>
        public async Task<string> GetOrDownloadPrimaryDocAsync(Filing filing, CancellationToken ct = default)
        {
            if (filing == null) throw new ArgumentNullException(nameof(filing));
            if (string.IsNullOrWhiteSpace(filing.CIK)) throw new ArgumentException("Filing.CIK is required.");

            var cikNoZeros = NormalizeCikForArchivePath(filing.CIK);
            
            var accNumNoDashes = Accession.GetAccessionFromFilename(filing.Filename, true);
            var accNumDashed = Accession.GetAccessionFile(filing.Filename);

            var localDir = Path.Combine(_settings.RawDir, filing.CIK, accNumNoDashes);

            Directory.CreateDirectory(localDir);

            var localPath = Path.Combine(localDir, accNumDashed);

            if (File.Exists(localPath) && !_settings.OverwriteRawFiles)
                return localPath;

            var url = BuildPrimaryDocUrl(cikNoZeros, accNumNoDashes, accNumDashed);

            // Fetch bytes and write
            var bytes = await _client.GetBytesAsync(url, ct);
            await File.WriteAllBytesAsync(localPath, bytes, ct);

            return localPath;
        }

        // https://www.sec.gov/Archives/edgar/data/1385329/00010629931000002/0001062993-10-000002.txt
        public static string BuildPrimaryDocUrl(string cikNoZeros, string accNumNoDashes, string accNumDashed)
        {
            // primaryDocument often already safe, but we avoid encoding changes; EDGAR paths are literal.
            return $"https://www.sec.gov/Archives/edgar/data/{cikNoZeros}/{accNumNoDashes}/{accNumDashed}";
        }

        public static string GetAccessionWithoutDashes(string fileName)
            => Path.GetFileNameWithoutExtension(fileName).Replace("-", "");

        public static string NormalizeCikForArchivePath(string cik10)
        {
            // EDGAR archive path uses integer-like CIK without leading zeros
            var trimmed = cik10.Trim();
            trimmed = trimmed.TrimStart('0');
            return string.IsNullOrEmpty(trimmed) ? "0" : trimmed;
        }

        /*private static (string Directory, string FileName) NormalizePathAndDirectoryPath(string path) {

            string normalized = path.Replace('/', Path.DirectorySeparatorChar);

            string directory = Path.GetDirectoryName(normalized)!;
            string fileName = Path.GetFileName(normalized);

            return (directory, fileName);
        }*/

        private static string SanitizeFileName(string fileName)
        {
            // EDGAR filenames are usually safe already. This is belt-and-suspenders.
            // Replace any invalid characters to keep Windows happy.
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(fileName.Length);
            foreach (var ch in fileName)
            {
                sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
            }
            return sb.ToString();
        }
    }
}
