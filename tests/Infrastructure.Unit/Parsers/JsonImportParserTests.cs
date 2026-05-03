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
        public async Task Parse_ShouldReturnEmpty_ForEmptyStream()
        {
            IImportParser sut = CreateSut();
            using Stream stream = new MemoryStream(Array.Empty<byte>());
            IReadOnlyList<IDictionary<string, object?>> rows = await sut.ParseAsync(stream);
            rows.Should().BeEmpty();
        }
    }
}
