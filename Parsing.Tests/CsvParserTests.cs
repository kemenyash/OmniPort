using OmniPort.Core.Interfaces;
using OmniPort.Core.Parsers;

namespace Parsing.Tests
{
    public class CsvParserTests
    {
        [Fact]
        public void Should_Parse_Csv_File()
        {
            IImportParser parser = new CsvImportParser();
            string path = Path.Combine(AppContext.BaseDirectory, "TestData", "sample.csv");
            Assert.True(File.Exists(path), $"Файл не знайдено: {path}");

            using FileStream stream = File.OpenRead(path);

            List<IDictionary<string, object?>> records = parser.Parse(stream).ToList();

            Assert.NotEmpty(records);
            Assert.Equal("Kateryna", records[0]["FirstName"]);
            Assert.Equal("Gromovych", records[0]["LastName"]);
        }
    }
}
