using OmniPort.Core.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Tests
{
    public class CsvParserTests
    {
        [Fact]
        public void Should_Parse_Csv_File()
        {
            var parser = new CsvImportParser();

            using var stream = File.OpenRead("TestData/sample.csv");
            var records = parser.Parse(stream).ToList();

            Assert.Equal("Kateryna", records[0]["FirstName"]);
            Assert.Equal("Gromovych", records[0]["LastName"]);
        }
    }
}
