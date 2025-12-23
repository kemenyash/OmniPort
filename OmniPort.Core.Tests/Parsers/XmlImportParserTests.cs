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
    public class XmlImportParserTests : ImportParserContractTests
    {
        protected override IImportParser CreateSut() => new XmlImportParser("record");

        protected override Stream CreateValidStream()
        {
            string xml = """
                        <root>
                          <record><Name>Alice</Name><Age>30</Age></record>
                          <record><Name>Bob</Name><Age>25</Age></record>
                        </root>
                        """;
            return new MemoryStream(Encoding.UTF8.GetBytes(xml));
        }

        [Fact]
        public void Parse_ShouldMapChildNodes_ToKeys()
        {
            IImportParser sut = CreateSut();
            using Stream stream = CreateValidStream();

            List<IDictionary<string, object?>> rows = sut.Parse(stream).ToList();

            rows.Should().HaveCount(2);
            rows[1]["Name"]?.ToString().Should().Be("Bob");
            rows[1]["Age"]?.ToString().Should().Be("25");
        }
    }
}
