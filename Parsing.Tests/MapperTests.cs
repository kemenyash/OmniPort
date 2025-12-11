using OmniPort.Core.Enums;
using OmniPort.Core.Mappers;
using OmniPort.Core.Models;

namespace Parsing.Tests
{
    public class MapperTests
    {
        [Fact]
        public void Should_Map_Correctly_With_Type_Conversion()
        {
            ImportProfile profile = new ImportProfile
            {
                Template = new ImportTemplate
                {
                    Fields = new List<TemplateField>
                    {
                        new() { Name = "Name", Type = FieldDataType.String },
                        new() { Name = "Age", Type = FieldDataType.Integer }
                    }
                },
                Mappings = new List<FieldMapping>
            {
                new() { SourceField = "full_name", TargetField = "Name", TargetType = FieldDataType.String },
                new() { SourceField = "years", TargetField = "Age", TargetType = FieldDataType.Integer }
            }
            };

            Dictionary<string, object?> sourceRow = new Dictionary<string, object?>
            {
                ["full_name"] = "Kateryna Gromovych",
                ["years"] = "42"
            };

            ImportMapper mapper = new ImportMapper(profile);
            IDictionary<string, object?> result = mapper.MapRow(sourceRow);

            Assert.Equal("Kateryna Gromovych", result["Name"]);
            Assert.Equal(42, result["Age"]);
        }
    }
}
