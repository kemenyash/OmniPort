using FluentAssertions;
using BusinessLogic.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Unit.Contracts
{
    public abstract class ImportParserContractTests
    {
        protected abstract IImportParser CreateSut();
        protected abstract Stream CreateValidStream();

        [Fact]
        public async Task Parse_ShouldNotThrow_ForValidInput()
        {
            IImportParser sut = CreateSut();
            using Stream stream = CreateValidStream();
            
            Func<Task> act = async () => await sut.ParseAsync(stream);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Parse_ShouldReturnCollection_OfDictionaries()
        {
            IImportParser sut = CreateSut();
            using Stream stream = CreateValidStream();
            
            IReadOnlyList<IDictionary<string, object?>> rows = await sut.ParseAsync(stream);
            
            rows.Should().NotBeNull();
            rows.Should().NotBeEmpty();
            rows.All(r => r is not null).Should().BeTrue();
        }

        [Fact]
        public async Task Parse_ShouldReturnDictionaries_WithAtLeastOneKey()
        {
            IImportParser sut = CreateSut();
            using Stream stream = CreateValidStream();
            IReadOnlyList<IDictionary<string, object?>> rows = await sut.ParseAsync(stream);

            rows.Should().OnlyContain(r => r.Keys.Count > 0);
        }
    }
}
