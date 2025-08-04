using OmniPort.Core.Models;
using OmniPort.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Mappers
{
    public class ImportMapper
    {
        private readonly ImportProfile profile;

        public ImportMapper(ImportProfile profile)
        {
            this.profile = profile;
        }

        public IDictionary<string, object?> MapRow(IDictionary<string, object?> sourceRow)
        {
            var result = new Dictionary<string, object?>();

            foreach (var mapping in profile.Mappings)
            {
                if (!sourceRow.TryGetValue(mapping.SourceField, out var value))
                    continue;

                object? transformed = mapping.CustomTransform != null
                    ? mapping.CustomTransform(value)
                    : DataTypeConverter.ConvertToType(value, mapping);

                result[mapping.TargetField] = transformed;
            }

            return result;
        }
    }
}
