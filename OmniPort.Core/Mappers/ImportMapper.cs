using OmniPort.Core.Models;
using OmniPort.Core.Utilities;

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
            Dictionary<string, object?> result = new Dictionary<string, object?>();

            foreach (FieldMapping mapping in profile.Mappings)
            {
                if (!sourceRow.TryGetValue(mapping.SourceField, out object? value))
                {
                    continue;
                }

                object? transformed = mapping.CustomTransform != null
                    ? mapping.CustomTransform(value)
                    : DataTypeConverter.ConvertToType(value, mapping);

                result[mapping.TargetField] = transformed;
            }

            return result;
        }
    }
}