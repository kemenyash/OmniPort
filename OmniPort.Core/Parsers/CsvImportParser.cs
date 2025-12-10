using CsvHelper;
using CsvHelper.Configuration;
using OmniPort.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Parsers
{
    public class CsvImportParser : IImportParser
    {
        public IEnumerable<IDictionary<string, object?>> Parse(Stream stream)
        {
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                IgnoreBlankLines = true
            });

            while (csv.Read())
            {
                var dictionary = csv.GetRecord<dynamic>() as IDictionary<string, object?>;
                if (dictionary != null)
                {
                    yield return new Dictionary<string, object?>(dictionary);
                }
            }
        }
    }
}