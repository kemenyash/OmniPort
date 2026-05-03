using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using BusinessLogic.Enums;
using BusinessLogic.Interfaces;
using Newtonsoft.Json.Linq;
using Presentation.Models;
using System.Globalization;
using System.Net;

namespace Presentation.Services
{
    public sealed class TemplateSchemaInferenceService
    {
        private const int MaxRowsToInspect = 50;

        private readonly IImportParserFactory importParserFactory;
        private readonly IOptionsMonitor<UploadLimits> uploadLimitsMonitor;

        public TemplateSchemaInferenceService(
            IImportParserFactory importParserFactory,
            IOptionsMonitor<UploadLimits> uploadLimitsMonitor)
        {
            this.importParserFactory = importParserFactory;
            this.uploadLimitsMonitor = uploadLimitsMonitor;
        }

        public async Task<List<TemplateFieldRow>> InferFromUpload(IBrowserFile file, SourceType sourceType)
        {
            var uploadLimits = uploadLimitsMonitor.CurrentValue;
            var maxAllowedBytes = uploadLimits.GetMaxFor(sourceType);

            await using var stream = file.OpenReadStream(maxAllowedBytes);
            return await InferFromStream(stream, sourceType);
        }

        public async Task<List<TemplateFieldRow>> InferFromUrl(string url, SourceType sourceType, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return new List<TemplateFieldRow>();
            }

            var uploadLimits = uploadLimitsMonitor.CurrentValue;
            var maxAllowedBytes = uploadLimits.GetMaxFor(sourceType);

            using var socketsHttpHandler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10
            };

            using var httpClient = new HttpClient(socketsHttpHandler, disposeHandler: true);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("OmniPort/1.0");
            request.Headers.Accept.ParseAdd("*/*");

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var cappedStream = await CopyToCapped(responseStream, maxAllowedBytes, cancellationToken);

            cappedStream.Position = 0;
            return await InferFromStream(cappedStream, sourceType);
        }

        private async Task<List<TemplateFieldRow>> InferFromStream(Stream stream, SourceType sourceType)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            var parser = importParserFactory.Create(sourceType);
            var rows = (await parser.ParseAsync(stream)).Take(MaxRowsToInspect).ToList();

            return InferFromRows(rows);
        }

        private static List<TemplateFieldRow> InferFromRows(IEnumerable<IDictionary<string, object?>> rows)
        {
            var fieldsByName = new Dictionary<string, TemplateFieldRow>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                foreach (var (name, value) in row)
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var inferred = InferField(name.Trim(), value);

                    if (fieldsByName.TryGetValue(inferred.Name, out var existing))
                    {
                        Merge(existing, inferred);
                    }
                    else
                    {
                        fieldsByName[inferred.Name] = inferred;
                    }
                }
            }

            return fieldsByName.Values
                .OrderBy(field => field.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static TemplateFieldRow InferField(string name, object? value)
        {
            if (TryGetObjectProperties(value, out var objectProperties))
            {
                return new TemplateFieldRow
                {
                    Name = name,
                    Type = FieldDataType.Object,
                    Children = InferFromRows(new[] { objectProperties })
                };
            }

            if (TryGetArrayItems(value, out var arrayItems))
            {
                var row = new TemplateFieldRow
                {
                    Name = name,
                    Type = FieldDataType.Array
                };

                var itemSamples = arrayItems.Where(item => item is not null).ToList();
                var objectSamples = itemSamples
                    .Select(item => TryGetObjectProperties(item, out var properties) ? properties : null)
                    .Where(properties => properties is not null)
                    .Select(properties => properties!)
                    .ToList();

                if (objectSamples.Any())
                {
                    row.ItemType = FieldDataType.Object;
                    row.ChildrenItems = InferFromRows(objectSamples);
                }
                else
                {
                    row.ItemType = itemSamples.Any()
                        ? itemSamples.Select(InferScalarType).FirstOrDefault(type => type != FieldDataType.String)
                        : FieldDataType.String;
                }

                return row;
            }

            return new TemplateFieldRow
            {
                Name = name,
                Type = InferScalarType(value)
            };
        }

        private static void Merge(TemplateFieldRow target, TemplateFieldRow source)
        {
            if (target.Type != source.Type)
            {
                target.Type = PreferWider(target.Type, source.Type);
            }

            if (target.Type == FieldDataType.Object)
            {
                MergeChildren(target.Children, source.Children);
            }

            if (target.Type == FieldDataType.Array)
            {
                target.ItemType = target.ItemType == source.ItemType
                    ? target.ItemType
                    : PreferWider(target.ItemType ?? FieldDataType.String, source.ItemType ?? FieldDataType.String);

                if (target.ItemType == FieldDataType.Object)
                {
                    MergeChildren(target.ChildrenItems, source.ChildrenItems);
                }
            }
        }

        private static void MergeChildren(List<TemplateFieldRow> targetChildren, List<TemplateFieldRow> sourceChildren)
        {
            foreach (var sourceChild in sourceChildren)
            {
                var existing = targetChildren.FirstOrDefault(child =>
                    string.Equals(child.Name, sourceChild.Name, StringComparison.OrdinalIgnoreCase));

                if (existing is null)
                {
                    targetChildren.Add(sourceChild);
                }
                else
                {
                    Merge(existing, sourceChild);
                }
            }
        }

        private static FieldDataType PreferWider(FieldDataType left, FieldDataType right)
        {
            if (left == right) return left;
            if (left == FieldDataType.String || right == FieldDataType.String) return FieldDataType.String;
            if (left == FieldDataType.Decimal || right == FieldDataType.Decimal) return FieldDataType.Decimal;
            if (left == FieldDataType.Integer || right == FieldDataType.Integer) return FieldDataType.Decimal;
            return FieldDataType.String;
        }

        private static FieldDataType InferScalarType(object? value)
        {
            value = UnwrapJValue(value);

            if (value is null) return FieldDataType.String;
            if (value is bool) return FieldDataType.Boolean;
            if (value is DateTime) return FieldDataType.DateTime;
            if (value is byte or short or int or long) return FieldDataType.Integer;
            if (value is float or double or decimal) return FieldDataType.Decimal;

            var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return FieldDataType.String;
            if (bool.TryParse(text, out _)) return FieldDataType.Boolean;
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)) return FieldDataType.Integer;
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out _)) return FieldDataType.Decimal;
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out _)) return FieldDataType.DateTime;

            return FieldDataType.String;
        }

        private static object? UnwrapJValue(object? value)
        {
            return value is JValue jValue ? jValue.Value : value;
        }

        private static bool TryGetObjectProperties(object? value, out IDictionary<string, object?> properties)
        {
            value = UnwrapJValue(value);

            if (value is JObject jObject)
            {
                properties = jObject.Properties().ToDictionary(
                    property => property.Name,
                    property => (object?)property.Value,
                    StringComparer.OrdinalIgnoreCase);
                return true;
            }

            if (value is IDictionary<string, object?> dictionary)
            {
                properties = dictionary;
                return true;
            }

            properties = new Dictionary<string, object?>();
            return false;
        }

        private static bool TryGetArrayItems(object? value, out List<object?> items)
        {
            value = UnwrapJValue(value);

            if (value is JArray jArray)
            {
                items = jArray.Select(token => (object?)token).ToList();
                return true;
            }

            if (value is IEnumerable<object?> enumerable && value is not string)
            {
                items = enumerable.ToList();
                return true;
            }

            items = new List<object?>();
            return false;
        }

        private static async Task<MemoryStream> CopyToCapped(Stream source, long maxBytes, CancellationToken cancellationToken)
        {
            var memoryStream = new MemoryStream();
            var buffer = new byte[81920];
            long totalBytesRead = 0;

            while (true)
            {
                var bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (bytesRead <= 0)
                {
                    break;
                }

                totalBytesRead += bytesRead;
                if (totalBytesRead > maxBytes)
                {
                    throw new InvalidOperationException($"The input stream exceeded the limit of {maxBytes} bytes.");
                }

                await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
