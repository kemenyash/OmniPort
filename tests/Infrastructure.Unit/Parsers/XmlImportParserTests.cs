using FluentAssertions;
using BusinessLogic.Interfaces;
using Infrastructure.Parsers;
using Infrastructure.Unit.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Unit.Parsers
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
        public async Task Parse_ShouldMapChildNodes_ToKeys()
        {
            IImportParser sut = CreateSut();
            using Stream stream = CreateValidStream();

            IReadOnlyList<IDictionary<string, object?>> rows = await sut.ParseAsync(stream);

            rows.Should().HaveCount(2);
            rows[1]["Name"]?.ToString().Should().Be("Bob");
            rows[1]["Age"]?.ToString().Should().Be("25");
        }
    }
}
