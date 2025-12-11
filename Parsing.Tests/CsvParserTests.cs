using OmniPort.Core.Parsers;

namespace Parsing.Tests
{
    public class CsvParserTests
    {
        [Fact]
        public void Should_Parse_Csv_File()
        {
            CsvImportParser parser = new CsvImportParser();

            using FileStream stream = File.OpenRead("TestData/sample.csv");
            List<IDictionary<string, object?>> records = parser.Parse(stream).ToList();

            Assert.Equal("Kateryna", records[0]["FirstName"]);
            Assert.Equal("Gromovych", records[0]["LastName"]);
        }
    }
}
