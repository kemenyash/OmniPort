using OmniPort.Core.Mappers;
using OmniPort.Core.Models;

namespace OmniPort.UI.Services
{
    public class MappingService
    {
        public List<Dictionary<string, object?>> MapData(IEnumerable<IDictionary<string, object?>> input, ImportProfile profile)
        {
            var mapper = new ImportMapper(profile);
            var result = new List<Dictionary<string, object?>>();

            foreach (var row in input)
            {
                var mapped = mapper.MapRow(row);
                result.Add(new Dictionary<string, object?>(mapped));
            }

            return result;
        }


    }
}
