using ClosedXML.Excel;
using OmniPort.Core.Interfaces;

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
                    MemoryStream memoryStream = new MemoryStream();
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

                using XLWorkbook workbook = new XLWorkbook(stream);

                IXLWorksheet worksheet = workbook.Worksheets.FirstOrDefault() ?? throw new InvalidOperationException("Workbook has no worksheets.");

                IXLRange? range = worksheet.RangeUsed();
                if (range is null)
                {
                    return Enumerable.Empty<IDictionary<string, object?>>();
                }

                List<IXLRangeRow> rows = range.RowsUsed().ToList();
                if (rows.Count < 2)
                {
                    return Enumerable.Empty<IDictionary<string, object?>>();
                }

                IXLRangeRow headerRow = rows[0];
                List<string> headers = headerRow.CellsUsed()
                                       .Select(c => c.GetString())
                                       .ToList();

                for (int i = 0; i < headers.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(headers[i]))
                    {
                        headers[i] = $"Column{i + 1}";
                    }
                }

                List<IDictionary<string, object?>> result = new List<IDictionary<string, object?>>(rows.Count - 1);

                for (int i = 1; i < rows.Count; i++)
                {
                    IXLRangeRow row = rows[i];
                    Dictionary<string, object?> dict = new Dictionary<string, object?>(headers.Count, StringComparer.OrdinalIgnoreCase);

                    for (int j = 0; j < headers.Count; j++)
                    {
                        string header = headers[j];
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
