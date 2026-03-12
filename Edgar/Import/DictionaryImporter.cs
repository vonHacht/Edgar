using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using Edgar.Config;
using Edgar.Models;

namespace Edgar.Import
{
    public sealed class DictionaryImporter
    {
        private readonly string _loughranMcDonaldMasterDictionaryFilename;
        private readonly CsvConfiguration _csvConfig;

        // Column indices
        private int _wordIdx, _negativeIdx, _positiveIdx, _uncertantyIdx;

        public DictionaryImporter(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _loughranMcDonaldMasterDictionaryFilename = settings.LoughranMcDonaldMasterDictionaryFilename
                ?? throw new ArgumentException("LoughranMcDonaldMasterDictionaryFilename is null.", nameof(settings));

            EnsureFileExists(_loughranMcDonaldMasterDictionaryFilename);

            _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                PrepareHeaderForMatch = args => args.Header.Replace("\uFEFF", "").Trim(),
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null
            };

            LoadIndices(); // load once, up-front
        }

        public LmDictionaries ReadAllDictionaries()
        {
            var data = new LmDictionaries();

            using var reader = new StreamReader(_loughranMcDonaldMasterDictionaryFilename);
            using var parser = new CsvParser(reader, _csvConfig);

            // Consume header row
            var header = parser.Read();
            if (!parser.Read())
                return data;

            while (parser.Read())
            {

                var fields = parser.Record;
                if (fields == null || fields.Length <= _wordIdx)
                    continue;

                var rawWord = GetField(fields, _wordIdx);
                var word = NormalizeWord(rawWord);

                if (string.IsNullOrEmpty(word))
                    continue;

                if (ReadInt(fields, _positiveIdx) > 0)
                    data.Positive.Add(word);

                if (ReadInt(fields, _negativeIdx) > 0)
                    data.Negative.Add(word);

                if (ReadInt(fields, _uncertantyIdx) > 0)
                    data.Uncertainty.Add(word);
            }

            return data;
        }

        private void EnsureFileExists(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"LoughranMcDonaldMasterDictionary path not found: {path}", path);
        }

        private void LoadIndices()
        {
            using var reader = new StreamReader(_loughranMcDonaldMasterDictionaryFilename);
            using var parser = new CsvParser(reader, _csvConfig);

            if (!parser.Read())
                throw new InvalidDataException("LoughranMcDonaldMasterDictionary CSV appears empty (no header).");

            var header = parser.Record ?? Array.Empty<string>();

            _wordIdx = IndexOf(header, "Word");
            _negativeIdx = IndexOf(header, "Negative");
            _positiveIdx = IndexOf(header, "Positive");
            _uncertantyIdx = IndexOf(header, "Uncertainty");
        }

        private static int IndexOf(string[] header, string name)
        {
            for (int i = 0; i < header.Length; i++)
                if (string.Equals(header[i], name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        private static string NormalizeWord(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return new string(input
                .Trim()
                .ToUpperInvariant()
                .Where(char.IsLetter)
                .ToArray());
        }

        private static string? GetField(string[] row, int index)
        {
            if (index < 0 || index >= row.Length)
                return null;

            return row[index];
        }

        private static int ReadInt(string[] row, int index)
        {
            if (index < 0 || index >= row.Length)
                return 0;

            var value = row[index]?.Trim();

            return int.TryParse(value, out var result) ? result : 0;
        }
    }
}


