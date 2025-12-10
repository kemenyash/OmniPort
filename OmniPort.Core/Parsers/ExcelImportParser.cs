using ClosedXML.Excel;
using OmniPort.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace OmniPort.Core.Parsers
{
    public class ExcelImportParser : IImportParser
    {
        public IEnumerable<IDictionary<string, object?>> Parse(Stream stream)
        {
            try
            {
                if (!stream.CanSeek)
                {
                    var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    memoryStream.Position = 0;
                    stream = memoryStream;
                }
                else if (stream.Position != 0)
                {
                    stream.Position = 0;
                }

                Span<byte> spanByte = stackalloc byte[4];
                int read = stream.Read(spanByte);
                stream.Position = 0;

                bool looksZip = read == 4 &&
                                spanByte[0] == (byte)'P' &&
                                spanByte[1] == (byte)'K' &&
                                spanByte[2] == 3 &&
                                spanByte[3] == 4;

                if (!looksZip)
                {
                    throw new InvalidOperationException("Remote content is not a valid XLSX (ZIP header missing).");
                }

                using var workbook = new XLWorkbook(stream);

                var worksheet = workbook.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("Workbook has no worksheets.");

                var range = worksheet.RangeUsed();
                if (range is null)
                {
                    return Enumerable.Empty<IDictionary<string, object?>>();
                }

                var rows = range.RowsUsed().ToList();
                if (rows.Count < 2)
                {
                    return Enumerable.Empty<IDictionary<string, object?>>();
                }

                var headerRow = rows[0];
                var headers = headerRow.CellsUsed()
                                       .Select(c => c.GetString())
                                       .ToList();

                for (int i = 0; i < headers.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(headers[i]))
                    {
                        headers[i] = $"Column{i + 1}";
                    }
                }

                var result = new List<IDictionary<string, object?>>(rows.Count - 1);

                for (int i = 1; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var dict = new Dictionary<string, object?>(headers.Count, StringComparer.OrdinalIgnoreCase);

                    for (int j = 0; j < headers.Count; j++)
                    {
                        var header = headers[j];
                        dict[header] = row.Cell(j + 1).Value;
                    }

                    result.Add(dict);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse Excel file", ex);
            }
        }
    }
}
