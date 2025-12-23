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
    public sealed class CsvImportParserTests : ImportParserContractTests
    {
        protected override IImportParser CreateSut() => new CsvImportParser();

        protected override Stream CreateValidStream()
        {
            string csv = "Name,Age\nAlice,30\nBob,25\n";
            return new MemoryStream(Encoding.UTF8.GetBytes(csv));
        }

        [Fact]
        public void Parse_ShouldExposeHeaders_AsKeys()
        {
            IImportParser sut = CreateSut();
            using Stream stream = CreateValidStream();

            List<IDictionary<string, object?>> rows = sut.Parse(stream).ToList();

            rows.Should().HaveCount(2);
            rows[0].Should().ContainKey("Name");
            rows[0].Should().ContainKey("Age");
            rows[0]["Name"]?.ToString().Should().Be("Alice");
            rows[0]["Age"]?.ToString().Should().Be("30");
        }
    }
}
