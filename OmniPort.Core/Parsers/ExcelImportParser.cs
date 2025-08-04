using ClosedXML.Excel;
using OmniPort.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Parsers
{
    public class ExcelImportParser : IImportParser
    {
        public IEnumerable<IDictionary<string, object?>> Parse(Stream stream)
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();
            var rows = worksheet.RangeUsed().RowsUsed().ToList();

            if (rows.Count < 2) yield break;

            var headers = rows[0].Cells().Select(c => c.Value.ToString() ?? "").ToList();

            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                var dict = new Dictionary<string, object?>();

                for (int j = 0; j < headers.Count; j++)
                {
                    dict[headers[j]] = row.Cell(j + 1).Value;
                }

                yield return dict;
            }
        }
    }
}
