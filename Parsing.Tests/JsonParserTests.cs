using OmniPort.Core.Parsers;

namespace Parsing.Tests
{
    public class JsonParserTests
    {
        [Fact]
        public void Should_Parse_Json_File()
        {
            JsonImportParser parser = new JsonImportParser();

            using FileStream stream = File.OpenRead("TestData/sample.json");
            List<IDictionary<string, object?>> records = parser.Parse(stream).ToList();

            Assert.NotEmpty(records);
            Assert.Contains("Name", records[0].Keys);
            Assert.Contains("Email", records[0].Keys);
        }
    }
}
