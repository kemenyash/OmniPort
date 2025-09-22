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
            using var sr = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var text = sr.ReadToEnd();
            if (string.IsNullOrWhiteSpace(text))
                return Enumerable.Empty<IDictionary<string, object?>>();

            text = text.Trim();

            if (text.StartsWith("["))
            {
                try
                {
                    var arr = JArray.Parse(text);
                    var rows = new List<IDictionary<string, object?>>(arr.Count);
                    foreach (var token in arr)
                    {
                        if (token is JObject obj)
                            rows.Add(obj.ToObject<Dictionary<string, object?>>()!);
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
                    var obj = JObject.Parse(text);
                    return new[]
                    {
                        obj.ToObject<Dictionary<string, object?>>()!
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
                var l = line?.Trim();
                if (string.IsNullOrEmpty(l)) continue;
                if (!l.StartsWith("{")) continue; 

                try
                {
                    var obj = JObject.Parse(l);
                    list.Add(obj.ToObject<Dictionary<string, object?>>()!);
                }
                catch (JsonException)
                {
                }
            }

            return list;
        }
    }
}
