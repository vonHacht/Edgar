using System.Globalization;
using System.Text;

namespace Edgar.Export
{
    public abstract class CsvExporter<TRow>
    {
        private readonly bool _endcontent;

        public CsvExporter(bool endcontent = false)
        {
            _endcontent = endcontent;
        }

        public async Task WriteAsync(IEnumerable<TRow> rows, string outputPath, bool overwrite = true)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows));

            var list = rows.ToList();
            if (list.Count == 0)
                return;

            OnBeforeWrite(list);

            var writeHeader = overwrite || !File.Exists(outputPath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using var stream = new FileStream(
                outputPath,
                overwrite ? FileMode.Create : FileMode.Append,
                FileAccess.Write,
                FileShare.Read
            );

            using var writer = new StreamWriter(stream, Encoding.UTF8);

            if (writeHeader)
                await writer.WriteLineAsync(BuildHeader());

            foreach (var row in list)
                await writer.WriteLineAsync(BuildRow(row));

            if (_endcontent)
            {
                var end = EndContent();
                if (!string.IsNullOrEmpty(end))
                    await writer.WriteLineAsync(end);
            }
        }

        protected virtual void OnBeforeWrite(IReadOnlyList<TRow> rows) { }

        public abstract string BuildHeader();
        public abstract string BuildRow(TRow row);
        public virtual string? EndContent() => null;

        /// <summary>
        /// Escapes CSV fields according to RFC 4180.
        /// </summary>
        public static string Esc(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var needsQuotes =
                value.Contains(',') ||
                value.Contains('"') ||
                value.Contains('\n') ||
                value.Contains('\r');

            if (!needsQuotes)
                return value;

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}

