using ClosedXML.Excel;
using FluentAssertions;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Parsers;
using OmniPort.Core.Tests.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Tests.Parsers
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
        public void Parse_ShouldThrow_InvalidOperationException_ForNonXlsx()
        {
            IImportParser sut = CreateSut();
            using Stream stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

            Action act = () => sut.Parse(stream).ToList();

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
