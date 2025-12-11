using CsvHelper;
using CsvHelper.Configuration;
using OmniPort.Core.Interfaces;
using System.Globalization;

namespace OmniPort.Core.Parsers
{
    public class CsvImportParser : IImportParser
    {
        public IEnumerable<IDictionary<string, object?>> Parse(Stream stream)
        {
            using StreamReader reader = new StreamReader(stream);
            using CsvReader csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                IgnoreBlankLines = true
            });

            while (csv.Read())
            {
                IDictionary<string, object?>? dictionary = csv.GetRecord<dynamic>() as IDictionary<string, object?>;
                if (dictionary != null)
                {
                    yield return new Dictionary<string, object?>(dictionary);
                }
            }
        }
    }
}