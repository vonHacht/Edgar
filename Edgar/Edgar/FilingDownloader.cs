using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Edgar.Config;
using Edgar.Models;

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
            if (string.IsNullOrWhiteSpace(filing.Cik10)) throw new ArgumentException("Filing.Cik10 is required.");
            if (string.IsNullOrWhiteSpace(filing.AccessionNumber)) throw new ArgumentException("Filing.AccessionNumber is required.");
            if (string.IsNullOrWhiteSpace(filing.PrimaryDocument)) throw new ArgumentException("Filing.PrimaryDocument is required.");

            var cikNoZeros = NormalizeCikForArchivePath(filing.Cik10);
            var accessionNoDashes = AccessionNoNoDashes(filing.AccessionNumber);

            // Local cache path
            var localDir = Path.Combine(_settings.RawDir, filing.Cik10, accessionNoDashes);
            Directory.CreateDirectory(localDir);

            var safeDocName = SanitizeFileName(filing.PrimaryDocument);
            var localPath = Path.Combine(localDir, safeDocName);

            if (File.Exists(localPath) && !_settings.OverwriteRawFiles)
                return localPath;

            // Download URL (primary doc)
            var url = BuildPrimaryDocUrl(cikNoZeros, accessionNoDashes, filing.PrimaryDocument);

            // Fetch bytes and write
            var bytes = await _client.GetBytesAsync(url, ct);
            await File.WriteAllBytesAsync(localPath, bytes, ct);

            return localPath;
        }

        /// <summary>
        /// Builds: https://www.sec.gov/Archives/edgar/data/{cikNoZeros}/{accessionNoNoDashes}/{primaryDocument}
        /// </summary>
        public static string BuildPrimaryDocUrl(string cikNoZeros, string accessionNoNoDashes, string primaryDocument)
        {
            // primaryDocument often already safe, but we avoid encoding changes; EDGAR paths are literal.
            return $"https://www.sec.gov/Archives/edgar/data/{cikNoZeros}/{accessionNoNoDashes}/{primaryDocument}";
        }

        public static string AccessionNoNoDashes(string accessionNumber)
            => accessionNumber.Replace("-", "", StringComparison.Ordinal);

        public static string NormalizeCikForArchivePath(string cik10)
        {
            // EDGAR archive path uses integer-like CIK without leading zeros
            var trimmed = cik10.Trim();
            trimmed = trimmed.TrimStart('0');
            return string.IsNullOrEmpty(trimmed) ? "0" : trimmed;
        }

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
