using CsvHelper;
using CsvHelper.Configuration;
using Edgar.Config;
using Edgar.Edgar;
using Edgar.Export;
using Edgar.Models;
using Edgar.Parsing;
using Edgar.TextMeasures;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Runtime;
using System.Text;

namespace Edgar.Companies
{
    public class CompaniesService
    {
        private readonly string filename = "samples_2010_2023.csv";

        private List<Firm> _edgarCompanies;

        //private List<...> _crspCompanies;

        public CompaniesService(AppSettings _settings)
        {
            var companiesFilepath = Path.Combine(_settings.CompaniesDir, filename);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                PrepareHeaderForMatch = args => args.Header.Replace("\uFEFF", "").Trim(),
                MissingFieldFound = null,
                HeaderValidated = null
            };

            using var reader = new StreamReader(companiesFilepath);
            using var csv = new CsvReader(reader, config);

            _edgarCompanies = csv.GetRecords<Firm>().ToList();
        }
    

    public List<Firm> LoadFirms()
        {
            return _edgarCompanies;
        }
    }
}
