using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniPort.Core.Interfaces;
using System.Text;

namespace OmniPort.Core.Parsers
{
    public class JsonImportParser : IImportParser
    {
        public IEnumerable<IDictionary<string, object?>> Parse(Stream stream)
        {
            using var streamReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var text = streamReader.ReadToEnd();
            
            if (string.IsNullOrWhiteSpace(text))
            {
                return Enumerable.Empty<IDictionary<string, object?>>();
            }

            text = text.Trim();

            if (text.StartsWith("["))
            {
                try
                {
                    var jsonArray = JArray.Parse(text);
                    var rows = new List<IDictionary<string, object?>>(jsonArray.Count);
                    foreach (var token in jsonArray)
                    {
                        if (token is JObject jsonObject)
                        {
                            rows.Add(jsonObject.ToObject<Dictionary<string, object?>>()!);
                        }
                    }
                    return rows;
                }
                catch (JsonException)
                {
                    
                }
            }

            if (text.StartsWith("{"))
            {
                try
                {
                    var jsonObject = JObject.Parse(text);
                    return new[]
                    {
                        jsonObject.ToObject<Dictionary<string, object?>>()!
                    };
                }
                catch (JsonException)
                {
                }
            }

            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var list = new List<IDictionary<string, object?>>();
            foreach (var line in lines)
            {
                var trimedLine = line?.Trim();
                if (string.IsNullOrEmpty(trimedLine)) continue;
                if (!trimedLine.StartsWith("{")) continue; 

                try
                {
                    var jsonObject = JObject.Parse(trimedLine);
                    list.Add(jsonObject.ToObject<Dictionary<string, object?>>()!);
                }
                catch (JsonException)
                {
                }
            }

            return list;
        }
    }
}
