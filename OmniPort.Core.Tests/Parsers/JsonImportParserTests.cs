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
    public sealed class JsonImportParserTests : ImportParserContractTests
    {
        protected override IImportParser CreateSut() => new JsonImportParser();

        protected override Stream CreateValidStream()
        {
            string json = """
                        [
                          { "Name": "Alice", "Age": 30 },
                          { "Name": "Bob", "Age": 25 }
                        ]
                        """;
            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }

        [Fact]
        public void Parse_ShouldReturnEmpty_ForEmptyStream()
        {
            IImportParser sut = CreateSut();
            using Stream stream = new MemoryStream(Array.Empty<byte>());
            List<IDictionary<string, object?>> rows = sut.Parse(stream).ToList();
            rows.Should().BeEmpty();
        }
    }
}
