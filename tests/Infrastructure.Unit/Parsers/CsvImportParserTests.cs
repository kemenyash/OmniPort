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
    public sealed class CsvImportParserTests : ImportParserContractTests
    {
        protected override IImportParser CreateSut() => new CsvImportParser();

        protected override Stream CreateValidStream()
        {
            string csv = "Name,Age\nAlice,30\nBob,25\n";
            return new MemoryStream(Encoding.UTF8.GetBytes(csv));
        }

        [Fact]
        public async Task Parse_ShouldExposeHeaders_AsKeys()
        {
            IImportParser sut = CreateSut();
            using Stream stream = CreateValidStream();

            IReadOnlyList<IDictionary<string, object?>> rows = await sut.ParseAsync(stream);

            rows.Should().HaveCount(2);
            rows[0].Should().ContainKey("Name");
            rows[0].Should().ContainKey("Age");
            rows[0]["Name"]?.ToString().Should().Be("Alice");
            rows[0]["Age"]?.ToString().Should().Be("30");
        }
    }
}
