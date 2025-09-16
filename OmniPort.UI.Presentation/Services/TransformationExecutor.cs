using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using OmniPort.Core.Enums;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Mappers;
using OmniPort.Core.Models;
using OmniPort.Core.Parsers;
using OmniPort.UI.Presentation.Services;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Net.Http;
using System.Net;
using System.IO;

public class TransformationExecutor : ITransformationExecutionService
{
    private readonly IOptionsMonitor<UploadLimits> limits;
    private readonly ITransformationManager manager;
    private readonly IWebHostEnvironment env;

    public TransformationExecutor(
        IOptionsMonitor<UploadLimits> limits,
        ITransformationManager manager,
        IWebHostEnvironment env)
    {
        this.limits = limits;
        this.manager = manager;
        this.env = env;
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
                    return await CopyToCappedAsync(src, maxBytes, threshold);
                }

            case IFormFile form:
                {
                    if (form.Length > maxBytes)
                        throw new InvalidOperationException($"File {form.FileName} exceeds the maximum size of {maxBytes} bytes.");
                    using var src = form.OpenReadStream();
                    return await CopyToCappedAsync(src, maxBytes, threshold);
                }

            case Stream s:
                return await CopyToCappedAsync(s, maxBytes, threshold);

            case string path when File.Exists(path):
                {
                    var fi = new FileInfo(path);
                    if (fi.Length > maxBytes)
                        throw new InvalidOperationException($"File {fi.Name} exceeds the maximum size of {maxBytes} bytes.");

                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                    return await CopyToCappedAsync(fs, maxBytes, threshold);
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

        using var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        using var client = new HttpClient(handler, disposeHandler: true);

        using var firstReq = new HttpRequestMessage(HttpMethod.Get, url);
        firstReq.Headers.UserAgent.ParseAdd("OmniPort/1.0");
        firstReq.Headers.Accept.ParseAdd("*/*");

        using var firstResp = await client.SendAsync(firstReq, HttpCompletionOption.ResponseHeadersRead, ct);
        firstResp.EnsureSuccessStatusCode();

        var contentType = firstResp.Content.Headers.ContentType?.MediaType ?? string.Empty;
        await using var firstStream = await firstResp.Content.ReadAsStreamAsync(ct);

        var raw = await CopyToCappedAsync(firstStream, maxBytes, threshold);
        var seekable = await EnsureSeekableAtZeroAsync(raw);

        if (importType == SourceType.Excel && !LooksLikeZip(seekable))
        {
            string htmlHead = await PeekTextAsync(seekable, 512 * 1024);
            if (IsHtml(contentType, htmlHead))
            {
                var baseUri = new Uri(url, UriKind.Absolute);
                if (TryExtractXlsxHref(htmlHead, baseUri, out var directUri))
                {
                    using var fileReq = new HttpRequestMessage(HttpMethod.Get, directUri);
                    fileReq.Headers.UserAgent.ParseAdd("OmniPort/1.0");
                    fileReq.Headers.Referrer = baseUri;
                    fileReq.Headers.Accept.ParseAdd("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,*/*");

                    using var fileResp = await client.SendAsync(fileReq, HttpCompletionOption.ResponseHeadersRead, ct);
                    fileResp.EnsureSuccessStatusCode();

                    await using var fileStream = await fileResp.Content.ReadAsStreamAsync(ct);
                    var fileRaw = await CopyToCappedAsync(fileStream, maxBytes, threshold);
                    var fileSeekable = await EnsureSeekableAtZeroAsync(fileRaw);

                    if (!LooksLikeZip(fileSeekable))
                    {
                        string head2 = await PeekTextAsync(fileSeekable, 1024);
                        throw new InvalidOperationException(
                            $"Direct link did not return XLSX/ZIP. Content-Type: '{fileResp.Content.Headers.ContentType?.MediaType}'. Head: {head2}");
                    }

                    return fileSeekable;
                }
            }

            throw new InvalidOperationException(
                $"Remote content is not a valid XLSX/ZIP. Content-Type: '{contentType}'. " +
                $"Head: {TrimPreview(htmlHead)}");
        }

        return seekable;

        static bool IsHtml(string contentType, string head)
            => contentType.Contains("html", StringComparison.OrdinalIgnoreCase)
               || head.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase)
               || head.Contains("<html", StringComparison.OrdinalIgnoreCase);
    }


    // === Helpers ===


    private static bool TryExtractXlsxHref(string html, Uri baseUri, out Uri directUri)
    {
        directUri = null!;

        var idx = IndexOfXlsxHref(html, out string href);
        if (idx < 0) return false;

        if (!Uri.TryCreate(baseUri, href, out var abs))
            return false;

        directUri = abs;
        return true;

        static int IndexOfXlsxHref(string html, out string href)
        {
            href = string.Empty;
            var lower = html.ToLowerInvariant();
            var key = "href";
            int pos = 0;
            while ((pos = lower.IndexOf(key, pos, StringComparison.Ordinal)) >= 0)
            {
                int eq = lower.IndexOf('=', pos + key.Length);
                if (eq < 0) { pos += key.Length; continue; }

                int i = eq + 1;
                while (i < html.Length && char.IsWhiteSpace(html[i])) i++;

                if (i >= html.Length) break;

                char quote = html[i];
                string candidate;
                if (quote == '"' || quote == '\'')
                {
                    i++;
                    int j = html.IndexOf(quote, i);
                    if (j < 0) break;
                    candidate = html.Substring(i, j - i);
                    pos = j + 1;
                }
                else
                {
                    int j = i;
                    while (j < html.Length && !char.IsWhiteSpace(html[j]) && html[j] != '>')
                        j++;
                    candidate = html.Substring(i, j - i);
                    pos = j;
                }

                if (candidate.Contains(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    href = candidate.Trim();
                    return pos;
                }
            }
            return -1;
        }
    }

    private static string TrimPreview(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "(empty)";
        s = s.Replace("\r", " ").Replace("\n", " ").Trim();
        if (s.Length > 300) s = s.Substring(0, 300) + "…";
        return s;
    }


    private static async Task<Stream> CopyToCappedAsync(Stream source, long maxBytes, long threshold)
    {
        var ms = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;

        while (true)
        {
            int read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length));
            if (read <= 0) break;

            total += read;
            if (total > maxBytes)
                throw new InvalidOperationException($"The input stream exceeded the limit of {maxBytes} bytes.");

            if (total <= threshold)
            {
                await ms.WriteAsync(buffer.AsMemory(0, read));
                continue;
            }

            var tmp = Path.GetTempFileName();
            await using (var wr = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                ms.Position = 0;
                await ms.CopyToAsync(wr);
                await wr.WriteAsync(buffer.AsMemory(0, read));

                while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                {
                    total += read;
                    if (total > maxBytes)
                        throw new InvalidOperationException($"The input stream exceeded the limit of {maxBytes} bytes.");
                    await wr.WriteAsync(buffer.AsMemory(0, read));
                }
            }

            return new FileStream(tmp, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false);
        }

        ms.Position = 0;
        return ms;
    }

    private static async Task<Stream> EnsureSeekableAtZeroAsync(Stream s)
    {
        if (s.CanSeek)
        {
            s.Position = 0;
            return s;
        }
        var ms = new MemoryStream();
        await s.CopyToAsync(ms);
        ms.Position = 0;
        s.Dispose();
        return ms;
    }

    private static bool LooksLikeZip(Stream s)
    {
        if (!s.CanSeek) return false;
        long pos = s.Position;
        Span<byte> magic = stackalloc byte[4];
        int read = s.Read(magic);
        s.Position = pos;
        return read == 4 && magic[0] == (byte)'P' && magic[1] == (byte)'K' && magic[2] == 3 && magic[3] == 4;
    }

    private static async Task<string> PeekTextAsync(Stream s, int maxBytes)
    {
        if (!s.CanSeek) return "(non-seekable)";
        long pos = s.Position;
        var toRead = (int)Math.Min(maxBytes, s.Length - s.Position);
        var buf = new byte[toRead];
        int read = await s.ReadAsync(buf.AsMemory(0, toRead));
        s.Position = pos;
        try { return Encoding.UTF8.GetString(buf, 0, read).Replace("\0", "").Trim(); }
        catch { return "(binary or non-UTF8)"; }
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
