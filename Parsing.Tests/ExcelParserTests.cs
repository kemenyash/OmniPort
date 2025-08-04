using OmniPort.Core.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Tests
{
    public class ExcelParserTests
    {
        [Fact]
        public void Should_Parse_Excel_File()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "TestData", "sample.xlsx");
            Assert.True(File.Exists(path), $"Файл не знайдено: {path}");

            using var stream = File.OpenRead(path);
            var parser = new ExcelImportParser();

            var records = parser.Parse(stream).ToList();
            Assert.NotEmpty(records);

            string? actualName = records[0]["Name"]?.ToString()?.Trim();

            Assert.Equal("Vlad", actualName);
            Assert.Equal("35", records[0]["Age"]?.ToString()?.Trim());
        }

    }
}
