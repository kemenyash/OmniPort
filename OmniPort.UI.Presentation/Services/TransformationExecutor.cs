using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using OmniPort.Core.Extensions;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Mappers;
using OmniPort.Core.Models;
using OmniPort.Core.Parsers;
using OmniPort.UI.Presentation.Interfaces;
using OmniPort.UI.Presentation.Services;
using System.Text;
using System.Text.Json;
using System.Xml;

public class TransformationExecutor : ITransformationExecutionService
{
    private readonly IOptionsMonitor<UploadLimits> limits;
    private readonly ITransformationManager manager;
    private readonly IWebHostEnvironment env;
    private readonly HttpClient httpClient;

    public TransformationExecutor(
        IOptionsMonitor<UploadLimits> limits,
        ITransformationManager manager,
        IWebHostEnvironment env,
        HttpClient http)
    {
        this.limits = limits;
        this.manager = manager;
        this.env = env;
        httpClient = http;
    }

    public async Task<string> TransformUploadedFileAsync(int templateId, object file, string outputExtension)
    {
        var (profile, importType, convertType) = await manager.GetImportProfileForJoinAsync(templateId);
        using var stream = await ResolveStreamAsync(file, importType);
        var rows = ParseAndMap(stream, importType, profile);
        var baseName = GetBaseNameFromUpload(file) ?? $"template-{templateId}";
        return await SaveTransformedAsync(rows, outputExtension, baseName);
    }

    public async Task<string> TransformFromUrlAsync(int templateId, string url, string outputExtension)
    {
        var (profile, importType, convertType) = await manager.GetImportProfileForJoinAsync(templateId);
        using var stream = await OpenHttpStreamWithCapAsync(url, importType);
        var rows = ParseAndMap(stream, importType, profile);
        var baseName = MakeSafeFileName(new Uri(url).Segments.LastOrDefault() ?? "remote");
        return await SaveTransformedAsync(rows, outputExtension, baseName);
    }

    public async Task<string> SaveTransformedAsync(IEnumerable<IDictionary<string, object?>> rows, string outputExtension, string baseName)
    {
        var safeExt = (outputExtension ?? "json").Trim('.').ToLowerInvariant();
        if (safeExt is not ("csv" or "json" or "xml")) safeExt = "json";

        var exportDir = Path.Combine(env.WebRootPath ?? "wwwroot", "exports");
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

    private static IImportParser CreateParser(SourceType sourceType) => sourceType switch
    {
        SourceType.CSV => new CsvImportParser(),
        SourceType.Excel => new ExcelImportParser(),
        SourceType.JSON => new JsonImportParser(),
        SourceType.XML => new XmlImportParser("record"),
        _ => new CsvImportParser()
    };

    private async Task<Stream> ResolveStreamAsync(object file, SourceType importType)
    {
        var cfg = limits.CurrentValue;
        var maxBytes = cfg.GetMaxFor(importType);
        var threshold = cfg.InMemoryThresholdBytes;

        switch (file)
        {
            case IBrowserFile bf:
                {
                    if (bf.Size > maxBytes)
                        throw new InvalidOperationException($"File {bf.Name} exceeds the maximum size of {maxBytes} bytes.");
                    using var src = bf.OpenReadStream(maxAllowedSize: maxBytes);
                    if (bf.Size <= threshold)
                    {
                        var ms = new MemoryStream(capacity: (int)Math.Min(bf.Size, int.MaxValue));
                        await src.CopyToAsync(ms);
                        ms.Position = 0;
                        return ms;
                    }
                    var tmp = Path.GetTempFileName();
                    using (var wr = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                        await src.CopyToAsync(wr);
                    return new TempFileStreamExtension(tmp);
                }

            case IFormFile form:
                {
                    if (form.Length > maxBytes)
                        throw new InvalidOperationException($"File {form.FileName} exceeds the maximum size of {maxBytes} bytes.");
                    using var src = form.OpenReadStream();
                    if (form.Length <= threshold)
                    {
                        var ms = new MemoryStream(capacity: (int)Math.Min(form.Length, int.MaxValue));
                        await src.CopyToAsync(ms);
                        ms.Position = 0;
                        return ms;
                    }
                    var tmp = Path.GetTempFileName();
                    using (var wr = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                        await src.CopyToAsync(wr);
                    return new TempFileStreamExtension(tmp);
                }

            case Stream s:
                {
                    return await CopyToCappedAsync(s, maxBytes, threshold);
                }

            case string path when File.Exists(path):
                {
                    var fi = new FileInfo(path);
                    if (fi.Length > maxBytes)
                        throw new InvalidOperationException($"File {fi.Name} exceeds the maximum size of {maxBytes} bytes.");
                    if (fi.Length <= threshold)
                    {
                        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                        var ms = new MemoryStream(capacity: (int)Math.Min(fi.Length, int.MaxValue));
                        await fs.CopyToAsync(ms);
                        ms.Position = 0;
                        return ms;
                    }
                    return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false);
                }

            default:
                throw new NotSupportedException($"Unsupported upload type: {file?.GetType().FullName}");
        }
    }

    private async Task<Stream> OpenHttpStreamWithCapAsync(string url, SourceType importType, CancellationToken ct = default)
    {
        var cfg = limits.CurrentValue;
        var maxBytes = cfg.GetMaxFor(importType);
        var threshold = cfg.InMemoryThresholdBytes;

        using var resp = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        var len = resp.Content.Headers.ContentLength;
        if (len.HasValue && len.Value > maxBytes)
            throw new InvalidOperationException($"File at the link exceeds the maximum size of {maxBytes} bytes.");

        using var src = await resp.Content.ReadAsStreamAsync(ct);
        return await CopyToCappedAsync(src, maxBytes, threshold);
    }

    private static async Task<Stream> CopyToCappedAsync(Stream source, long maxBytes, long threshold)
    {
        var ms = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;

        int read;
        while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
        {
            total += read;
            if (total > maxBytes)
                throw new InvalidOperationException($"The input stream exceeded the limit of {maxBytes} bytes.");
            await ms.WriteAsync(buffer.AsMemory(0, read));
            if (total > threshold && ms.Capacity >= threshold)
                break;
        }

        if (total <= threshold)
        {
            ms.Position = 0;
            return ms;
        }

        var tmp = Path.GetTempFileName();
        using (var wr = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            ms.Position = 0;
            await ms.CopyToAsync(wr);
            while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
            {
                total += read;
                if (total > maxBytes)
                    throw new InvalidOperationException($"The input stream exceeded the limit of {maxBytes} bytes.");
                await wr.WriteAsync(buffer.AsMemory(0, read));
            }
        }
        return new TempFileStreamExtension(tmp);
    }

    private static string? GetBaseNameFromUpload(object file) =>
        file is IBrowserFile bf ? Path.GetFileNameWithoutExtension(bf.Name) : null;

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
