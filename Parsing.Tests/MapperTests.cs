using OmniPort.Core.Mappers;
using OmniPort.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Tests
{
    public class MapperTests
    {
        [Fact]
        public void Should_Map_Correctly_With_Type_Conversion()
        {
            var profile = new ImportProfile
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

            var sourceRow = new Dictionary<string, object?>
            {
                ["full_name"] = "Kateryna Gromovych",
                ["years"] = "42"
            };

            var mapper = new ImportMapper(profile);
            var result = mapper.MapRow(sourceRow);

            Assert.Equal("Kateryna Gromovych", result["Name"]);
            Assert.Equal(42, result["Age"]);
        }
    }
}
