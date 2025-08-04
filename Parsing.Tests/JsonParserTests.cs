using OmniPort.Core.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Tests
{
    public class JsonParserTests
    {
        [Fact]
        public void Should_Parse_Json_File()
        {
            var parser = new JsonImportParser();

            using var stream = File.OpenRead("TestData/sample.json");
            var records = parser.Parse(stream).ToList();

            Assert.NotEmpty(records);
            Assert.Contains("Name", records[0].Keys);
            Assert.Contains("Email", records[0].Keys);
        }
    }
}
