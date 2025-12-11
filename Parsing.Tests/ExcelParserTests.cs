using OmniPort.Core.Parsers;

namespace Parsing.Tests
{
    public class ExcelParserTests
    {
        [Fact]
        public void Should_Parse_Excel_File()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "TestData", "sample.xlsx");
            Assert.True(File.Exists(path), $"Файл не знайдено: {path}");

            using FileStream stream = File.OpenRead(path);
            ExcelImportParser parser = new ExcelImportParser();

            List<IDictionary<string, object?>> records = parser.Parse(stream).ToList();
            Assert.NotEmpty(records);

            string? actualName = records[0]["Name"]?.ToString()?.Trim();

            Assert.Equal("Vlad", actualName);
            Assert.Equal("35", records[0]["Age"]?.ToString()?.Trim());
        }

    }
}
