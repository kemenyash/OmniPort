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
            using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            string text = streamReader.ReadToEnd();

            if (string.IsNullOrWhiteSpace(text))
            {
                return Enumerable.Empty<IDictionary<string, object?>>();
            }

            text = text.Trim();

            if (text.StartsWith("["))
            {
                try
                {
                    JArray jsonArray = JArray.Parse(text);
                    List<IDictionary<string, object?>> rows = new List<IDictionary<string, object?>>(jsonArray.Count);
                    foreach (JToken token in jsonArray)
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
                    JObject jsonObject = JObject.Parse(text);
                    return new[]
                    {
                        jsonObject.ToObject<Dictionary<string, object?>>()!
                    };
                }
                catch (JsonException)
                {
                }
            }

            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            List<IDictionary<string, object?>> list = new List<IDictionary<string, object?>>();
            foreach (string line in lines)
            {
                string? trimedLine = line?.Trim();
                if (string.IsNullOrEmpty(trimedLine)) continue;
                if (!trimedLine.StartsWith("{")) continue;

                try
                {
                    JObject jsonObject = JObject.Parse(trimedLine);
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
