using CsvHelper;
using CsvHelper.Configuration;
using BusinessLogic.Interfaces;
using System.Globalization;

namespace Infrastructure.Parsers
{
    public class CsvImportParser : IImportParser
    {
        public async Task<IReadOnlyList<IDictionary<string, object?>>> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            using StreamReader reader = new StreamReader(stream);
            using CsvReader csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                IgnoreBlankLines = true
            });

            if (!await csv.ReadAsync())
            {
                return Array.Empty<IDictionary<string, object?>>();
            }

            csv.ReadHeader();
            string[] headers = csv.HeaderRecord ?? Array.Empty<string>();
            List<IDictionary<string, object?>> rows = new List<IDictionary<string, object?>>();

            while (await csv.ReadAsync())
            {
                Dictionary<string, object?> row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < headers.Length; i++)
                {
                    string header = headers[i];
                    if (string.IsNullOrWhiteSpace(header))
                    {
                        header = $"Column{i + 1}";
                    }

                    row[header] = csv.GetField(i);
                }

                rows.Add(row);
            }

            return rows;
        }
    }
}
