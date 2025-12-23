using FluentAssertions;
using OmniPort.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Tests.Contracts
{
    public abstract class ImportParserContractTests
    {
        protected abstract IImportParser CreateSut();
        protected abstract Stream CreateValidStream();

        [Fact]
        public void Parse_ShouldNotThrow_ForValidInput()
        {
            IImportParser sut = CreateSut();
            using Stream stream = CreateValidStream();
            
            Action act = () => sut.Parse(stream).ToList();
            act.Should().NotThrow();
        }

        [Fact]
        public void Parse_ShouldReturnCollection_OfDictionaries()
        {
            IImportParser sut = CreateSut();
            using Stream stream = CreateValidStream();
            
            List<IDictionary<string, object?>> rows = sut.Parse(stream).ToList();
            
            rows.Should().NotBeNull();
            rows.Should().NotBeEmpty();
            rows.All(r => r is not null).Should().BeTrue();
        }

        [Fact]
        public void Parse_ShouldReturnDictionaries_WithAtLeastOneKey()
        {
            IImportParser sut = CreateSut();
            using Stream stream = CreateValidStream();   
            List<IDictionary<string, object?>> rows = sut.Parse(stream).ToList();

            rows.Should().OnlyContain(r => r.Keys.Count > 0);
        }
    }
}
