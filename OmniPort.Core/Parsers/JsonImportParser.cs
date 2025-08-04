using Newtonsoft.Json;
using OmniPort.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Parsers
{
    public class JsonImportParser : IImportParser
    {
        public IEnumerable<IDictionary<string, object?>> Parse(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            string json = reader.ReadToEnd();

            var list = JsonConvert.DeserializeObject<List<Dictionary<string, object?>>>(json);
            return list ?? Enumerable.Empty<IDictionary<string, object?>>();
        }
    }
}
