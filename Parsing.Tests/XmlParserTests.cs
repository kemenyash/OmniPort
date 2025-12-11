using OmniPort.Core.Parsers;

namespace Parsing.Tests
{
    public class XmlParserTests
    {
        [Fact]
        public void Should_Parse_Xml_File()
        {
            XmlImportParser parser = new XmlImportParser("Person");

            using FileStream stream = File.OpenRead("TestData/sample.xml");
            List<IDictionary<string, object?>> records = parser.Parse(stream).ToList();

            Assert.Equal("Kateryna", records[0]["FirstName"]);
            Assert.Equal("Gromovych", records[0]["LastName"]);
        }
    }
}
