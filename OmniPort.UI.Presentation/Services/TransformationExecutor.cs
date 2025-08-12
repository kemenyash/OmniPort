using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Mappers;
using OmniPort.Core.Models;
using OmniPort.Core.Parsers;
using OmniPort.UI.Presentation.Interfaces;
using System.Text;
using System.Text.Json;
using System.Xml;

public class TransformationExecutor : ITransformationExecutionService
{
    private readonly ITransformationManager _manager;
    private readonly IWebHostEnvironment _env;
    private readonly HttpClient _http;

    public TransformationExecutor(
        ITransformationManager manager,
        IWebHostEnvironment env,
        HttpClient http)
    {
        _manager = manager;
        _env = env;
        _http = http;
    }

    public async Task<string> TransformUploadedFileAsync(int templateId, object file, string outputExtension)
    {
        var (profile, importType, convertType) = await _manager.GetImportProfileForJoinAsync(templateId);

        await using var stream = await ResolveStreamAsync(file);
        var rows = ParseAndMap(stream, importType, profile);

        var baseName = GetBaseNameFromUpload(file) ?? $"template-{templateId}";
        return await SaveTransformedAsync(rows, outputExtension, baseName);
    }

    public async Task<string> TransformFromUrlAsync(int templateId, string url, string outputExtension)
    {
        var (profile, importType, convertType) = await _manager.GetImportProfileForJoinAsync(templateId);

        await using var stream = await _http.GetStreamAsync(url);
        var rows = ParseAndMap(stream, importType, profile);

        var baseName = MakeSafeFileName(new Uri(url).Segments.LastOrDefault() ?? "remote");
        return await SaveTransformedAsync(rows, outputExtension, baseName);
    }

    public async Task<string> SaveTransformedAsync(IEnumerable<IDictionary<string, object?>> rows, string outputExtension, string baseName)
    {
        var safeExt = (outputExtension ?? "json").Trim('.').ToLowerInvariant();
        if (safeExt is not ("csv" or "json" or "xml")) safeExt = "json";

        var exportDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "exports");
        Directory.CreateDirectory(exportDir);

        var fileName = $"{MakeSafeFileName(baseName)}-{DateTime.UtcNow:yyyyMMddHHmmss}.{safeExt}";
        var fullPath = Path.Combine(exportDir, fileName);

        switch (safeExt)
        {
            case "csv":
                await File.WriteAllTextAsync(fullPath, ToCsv(rows), Encoding.UTF8);
                break;
            case "json":
                await File.WriteAllTextAsync(fullPath, ToJson(rows), Encoding.UTF8);
                break;
            case "xml":
                await File.WriteAllTextAsync(fullPath, ToXml(rows), Encoding.UTF8);
                break;
        }

        return $"/exports/{fileName}";
    }


    private static IEnumerable<IDictionary<string, object?>> ParseAndMap(Stream stream, SourceType sourceType, ImportProfile profile)
    {
        var parser = CreateParser(sourceType);
        var parsed = parser.Parse(stream);
        var mapper = new ImportMapper(profile);

        foreach (var row in parsed)
            yield return mapper.MapRow(row);
    }

    private static IImportParser CreateParser(SourceType sourceType)
    {
        return sourceType switch
        {
            SourceType.CSV => new CsvImportParser(),
            SourceType.Excel => new ExcelImportParser(),
            SourceType.JSON => new JsonImportParser(),
            SourceType.XML => new XmlImportParser("record"),
            _ => new CsvImportParser()
        };
    }

    private static async Task<Stream> ResolveStreamAsync(object file)
    {
        switch (file)
        {
            case IBrowserFile bf:
                return bf.OpenReadStream(long.MaxValue); 
            case Stream s:
                if (s.CanSeek) s.Position = 0;
                return s;
            case byte[] bytes:
                return new MemoryStream(bytes);
            default:
                var prop = file.GetType().GetProperty("Stream");
                if (prop?.GetValue(file) is Stream ps)
                {
                    if (ps.CanSeek) ps.Position = 0;
                    return ps;
                }
                throw new InvalidOperationException("Unsupported uploaded file type. Provide IBrowserFile/Stream/byte[].");
        }
    }

    private static string? GetBaseNameFromUpload(object file)
    {
        return file switch
        {
            IBrowserFile bf => Path.GetFileNameWithoutExtension(bf.Name),
            _ => null
        };
    }

    private static string MakeSafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "export";
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        name = name.Trim('.');
        return string.IsNullOrWhiteSpace(name) ? "export" : name;
    }

    private static string ToJson(IEnumerable<IDictionary<string, object?>> rows)
    {
        var opts = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(rows, opts);
    }

    private static string ToCsv(IEnumerable<IDictionary<string, object?>> rows)
    {
        var list = rows.ToList();
        if (list.Count == 0) return string.Empty;

        var headers = list.First().Keys.ToList();
        var sb = new StringBuilder();

        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

        foreach (var row in list)
        {
            var line = string.Join(",", headers.Select(h => EscapeCsv(row.TryGetValue(h, out var v) ? v : null)));
            sb.AppendLine(line);
        }
        return sb.ToString();

        static string EscapeCsv(object? value)
        {
            var s = value?.ToString() ?? string.Empty;
            var needsQuotes = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
            s = s.Replace("\"", "\"\"");
            return needsQuotes ? $"\"{s}\"" : s;
        }
    }

    private static string ToXml(IEnumerable<IDictionary<string, object?>> rows)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        };

        using var sw = new StringWriter();
        using var xw = XmlWriter.Create(sw, settings);

        xw.WriteStartDocument();
        xw.WriteStartElement("rows");

        foreach (var row in rows)
        {
            xw.WriteStartElement("row");
            foreach (var kv in row)
            {
                xw.WriteStartElement("field");
                xw.WriteAttributeString("name", kv.Key);
                if (kv.Value is not null)
                    xw.WriteString(kv.Value.ToString());
                xw.WriteEndElement();
            }
            xw.WriteEndElement();
        }

        xw.WriteEndElement(); 
        xw.WriteEndDocument();
        xw.Flush();

        return sw.ToString();
    }
}
