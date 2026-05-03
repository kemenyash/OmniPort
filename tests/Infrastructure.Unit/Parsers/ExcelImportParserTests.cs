using ClosedXML.Excel;
using FluentAssertions;
using BusinessLogic.Interfaces;
using Infrastructure.Parsers;
using Infrastructure.Unit.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Unit.Parsers
{
    public sealed class ExcelImportParserTests : ImportParserContractTests
    {
        protected override IImportParser CreateSut() => new ExcelImportParser();

        protected override Stream CreateValidStream()
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Sheet1");

            ws.Cell(1, 1).Value = "Name";
            ws.Cell(1, 2).Value = "Age";
            ws.Cell(2, 1).Value = "Alice";
            ws.Cell(2, 2).Value = 30;

            var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;
            return ms;
        }

        [Fact]
        public async Task Parse_ShouldThrow_InvalidOperationException_ForNonXlsx()
        {
            IImportParser sut = CreateSut();
            using Stream stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

            Func<Task> act = async () => await sut.ParseAsync(stream);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
