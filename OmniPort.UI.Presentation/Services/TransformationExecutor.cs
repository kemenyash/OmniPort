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
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml;

public class TransformationExecutor : ITransformationExecutionService
{
    private readonly IOptionsMonitor<UploadLimits> uploadLimitsMonitor;
    private readonly ITransformationManager transformationManager;
    private readonly IWebHostEnvironment webHostEnvironment;

    public TransformationExecutor(
        IOptionsMonitor<UploadLimits> uploadLimitsMonitor,
        ITransformationManager transformationManager,
        IWebHostEnvironment webHostEnvironment)
    {
        this.uploadLimitsMonitor = uploadLimitsMonitor;
        this.transformationManager = transformationManager;
        this.webHostEnvironment = webHostEnvironment;
    }

    public async Task<string> TransformUploadedFile(int templateId, object file, string outputExtension)
    {
        var join = await transformationManager.GetImportProfileForJoin(templateId);
        using var inputStream = await ResolveStream(file, join.ImportSourceType);
        var mappedRows = ParseAndMap(inputStream, join.ImportSourceType, join.Profile);
        var baseFileName = GetBaseNameFromUpload(file) ?? $"template-{templateId}";

        return await SaveTransformed(mappedRows, outputExtension, baseFileName);
    }

    public async Task<string> TransformFromUrl(int templateId, string url, string outputExtension)
    {
        var join = await transformationManager.GetImportProfileForJoin(templateId);
        using var inputStream = await OpenHttpStreamWithCap(url, join.ImportSourceType);
        var mappedRows = ParseAndMap(inputStream, join.ImportSourceType, join.Profile);
        var baseFileName = MakeSafeFileName(new Uri(url).Segments.LastOrDefault() ?? "remote");

        return await SaveTransformed(mappedRows, outputExtension, baseFileName);
    }


    public async Task<string> SaveTransformed(
        IEnumerable<IDictionary<string, object?>> rows,
        string outputExtension,
        string baseFileName)
    {
        var safeOutputExtension = NormalizeOutputExtension(outputExtension);

        var exportsDirectoryPath = Path.Combine(webHostEnvironment.WebRootPath ?? "wwwroot", "exports");
        Directory.CreateDirectory(exportsDirectoryPath);

        var outputFileName = $"{MakeSafeFileName(baseFileName)}-{DateTime.UtcNow:yyyyMMddHHmmss}.{safeOutputExtension}";
        var outputFilePath = Path.Combine(exportsDirectoryPath, outputFileName);

        switch (safeOutputExtension)
        {
            case "csv":
                {
                    await File.WriteAllTextAsync(outputFilePath, ToCsv(rows), Encoding.UTF8);
                    break;
                }
            case "json":
                {
                    await File.WriteAllTextAsync(outputFilePath, ToJson(rows), Encoding.UTF8);
                    break;
                }
            case "xml":
                {
                    await File.WriteAllTextAsync(outputFilePath, ToXml(rows), Encoding.UTF8);
                    break;
                }
        }

        return $"/exports/{outputFileName}";
    }

    private static string NormalizeOutputExtension(string outputExtension)
    {
        var safeExtension = (outputExtension ?? "json").Trim('.').ToLowerInvariant();

        if (safeExtension != "csv" && safeExtension != "json" && safeExtension != "xml")
        {
            safeExtension = "json";
        }

        return safeExtension;
    }

    private static IEnumerable<IDictionary<string, object?>> ParseAndMap(
        Stream stream,
        SourceType sourceType,
        ImportProfile importProfile)
    {
        var importParser = CreateParser(sourceType);

        var parsedRows = importParser.Parse(stream);

        var importMapper = new ImportMapper(importProfile);

        foreach (var parsedRow in parsedRows)
        {
            yield return importMapper.MapRow(parsedRow);
        }
    }

    private static IImportParser CreateParser(SourceType sourceType)
    {
        switch (sourceType)
        {
            case SourceType.CSV:
                {
                    return new CsvImportParser();
                }
            case SourceType.Excel:
                {
                    return new ExcelImportParser();
                }
            case SourceType.JSON:
                {
                    return new JsonImportParser();
                }
            case SourceType.XML:
                {
                    return new XmlImportParser("record");
                }
            default:
                {
                    return new CsvImportParser();
                }
        }
    }

    private async Task<Stream> ResolveStream(object file, SourceType importSourceType)
    {
        var uploadLimits = uploadLimitsMonitor.CurrentValue;
        var maxAllowedBytes = uploadLimits.GetMaxFor(importSourceType);
        var inMemoryThresholdBytes = uploadLimits.InMemoryThresholdBytes;

        switch (file)
        {
            case IBrowserFile browserFile:
                {
                    if (browserFile.Size > maxAllowedBytes)
                    {
                        throw new InvalidOperationException(
                            $"File {browserFile.Name} exceeds the maximum size of {maxAllowedBytes} bytes.");
                    }

                    using var sourceStream = browserFile.OpenReadStream(maxAllowedSize: maxAllowedBytes);
                    return await CopyToCapped(sourceStream, maxAllowedBytes, inMemoryThresholdBytes);
                }

            case IFormFile formFile:
                {
                    if (formFile.Length > maxAllowedBytes)
                    {
                        throw new InvalidOperationException(
                            $"File {formFile.FileName} exceeds the maximum size of {maxAllowedBytes} bytes.");
                    }

                    using var sourceStream = formFile.OpenReadStream();
                    return await CopyToCapped(sourceStream, maxAllowedBytes, inMemoryThresholdBytes);
                }

            case Stream inputStream:
                {
                    return await CopyToCapped(inputStream, maxAllowedBytes, inMemoryThresholdBytes);
                }

            case string filePath when File.Exists(filePath):
                {
                    var fileInfo = new FileInfo(filePath);

                    if (fileInfo.Length > maxAllowedBytes)
                    {
                        throw new InvalidOperationException(
                            $"File {fileInfo.Name} exceeds the maximum size of {maxAllowedBytes} bytes.");
                    }

                    using var fileStream = new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        4096,
                        useAsync: true);

                    return await CopyToCapped(fileStream, maxAllowedBytes, inMemoryThresholdBytes);
                }

            default:
                {
                    throw new NotSupportedException($"Unsupported upload type: {file?.GetType().FullName}");
                }
        }
    }

    private async Task<Stream> OpenHttpStreamWithCap(
        string url,
        SourceType importSourceType,
        CancellationToken cancellationToken = default)
    {
        var uploadLimits = uploadLimitsMonitor.CurrentValue;
        var maxAllowedBytes = uploadLimits.GetMaxFor(importSourceType);
        var inMemoryThresholdBytes = uploadLimits.InMemoryThresholdBytes;

        using var socketsHttpHandler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10,
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        using var httpClient = new HttpClient(socketsHttpHandler, disposeHandler: true);

        using var initialRequest = new HttpRequestMessage(HttpMethod.Get, url);
        initialRequest.Headers.UserAgent.ParseAdd("OmniPort/1.0");
        initialRequest.Headers.Accept.ParseAdd("*/*");

        using var initialResponse = await httpClient.SendAsync(
            initialRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        initialResponse.EnsureSuccessStatusCode();

        var responseContentType = initialResponse.Content.Headers.ContentType?.MediaType ?? string.Empty;

        await using var initialResponseStream = await initialResponse.Content.ReadAsStreamAsync(cancellationToken);

        var cappedStream = await CopyToCapped(initialResponseStream, maxAllowedBytes, inMemoryThresholdBytes);
        var seekableStream = await EnsureSeekableAtZero(cappedStream);

        if (importSourceType == SourceType.Excel && !LooksLikeZip(seekableStream))
        {
            var htmlHead = await PeekText(seekableStream, 512 * 1024);

            if (IsHtml(responseContentType, htmlHead))
            {
                var baseUri = new Uri(url, UriKind.Absolute);

                if (TryExtractXlsxHref(htmlHead, baseUri, out var directUri))
                {
                    using var fileRequest = new HttpRequestMessage(HttpMethod.Get, directUri);
                    fileRequest.Headers.UserAgent.ParseAdd("OmniPort/1.0");
                    fileRequest.Headers.Referrer = baseUri;
                    fileRequest.Headers.Accept.ParseAdd(
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,*/*");

                    using var fileResponse = await httpClient.SendAsync(
                        fileRequest,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                    fileResponse.EnsureSuccessStatusCode();

                    await using var fileResponseStream = await fileResponse.Content.ReadAsStreamAsync(cancellationToken);

                    var cappedFileStream = await CopyToCapped(fileResponseStream, maxAllowedBytes, inMemoryThresholdBytes);
                    var seekableFileStream = await EnsureSeekableAtZero(cappedFileStream);

                    if (!LooksLikeZip(seekableFileStream))
                    {
                        var responseHeadPreview = await PeekText(seekableFileStream, 1024);

                        throw new InvalidOperationException(
                            $"Direct link did not return XLSX/ZIP. Content-Type: '{fileResponse.Content.Headers.ContentType?.MediaType}'. Head: {responseHeadPreview}");
                    }

                    return seekableFileStream;
                }
            }

            throw new InvalidOperationException(
                $"Remote content is not a valid XLSX/ZIP. Content-Type: '{responseContentType}'. " +
                $"Head: {TrimPreview(htmlHead)}");
        }

        return seekableStream;
    }

    private static bool IsHtml(string contentType, string head)
    {
        if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (head.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (head.Contains("<html", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    // === Helpers ===

    private static bool TryExtractXlsxHref(string html, Uri baseUri, out Uri directUri)
    {
        directUri = null!;

        var index = IndexOfXlsxHref(html, out var href);
        if (index < 0)
        {
            return false;
        }

        if (!Uri.TryCreate(baseUri, href, out var absoluteUri))
        {
            return false;
        }

        directUri = absoluteUri;
        return true;
    }

    private static int IndexOfXlsxHref(string html, out string href)
    {
        href = string.Empty;

        var lower = html.ToLowerInvariant();
        var key = "href";
        var position = 0;

        while ((position = lower.IndexOf(key, position, StringComparison.Ordinal)) >= 0)
        {
            var equalsIndex = lower.IndexOf('=', position + key.Length);
            if (equalsIndex < 0)
            {
                position += key.Length;
                continue;
            }

            var i = equalsIndex + 1;
            while (i < html.Length && char.IsWhiteSpace(html[i]))
            {
                i++;
            }

            if (i >= html.Length)
            {
                break;
            }

            var quote = html[i];
            string candidate;

            if (quote == '"' || quote == '\'')
            {
                i++;
                var j = html.IndexOf(quote, i);
                if (j < 0)
                {
                    break;
                }

                candidate = html.Substring(i, j - i);
                position = j + 1;
            }
            else
            {
                var j = i;

                while (j < html.Length && !char.IsWhiteSpace(html[j]) && html[j] != '>')
                {
                    j++;
                }

                candidate = html.Substring(i, j - i);
                position = j;
            }

            if (candidate.Contains(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                href = candidate.Trim();
                return position;
            }
        }

        return -1;
    }

    private static string TrimPreview(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "(empty)";
        }

        var normalized = text.Replace("\r", " ").Replace("\n", " ").Trim();

        if (normalized.Length > 300)
        {
            normalized = normalized.Substring(0, 300) + "…";
        }

        return normalized;
    }

    private static async Task<Stream> CopyToCapped(Stream source, long maxBytes, long inMemoryThresholdBytes)
    {
        var memoryStream = new MemoryStream();
        var buffer = new byte[81920];
        long totalBytesRead = 0;

        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length));
            if (bytesRead <= 0)
            {
                break;
            }

            totalBytesRead += bytesRead;

            if (totalBytesRead > maxBytes)
            {
                throw new InvalidOperationException($"The input stream exceeded the limit of {maxBytes} bytes.");
            }

            if (totalBytesRead <= inMemoryThresholdBytes)
            {
                await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                continue;
            }

            var tempFilePath = Path.GetTempFileName();

            await using (var writeStream = new FileStream(
                tempFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true))
            {
                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(writeStream);

                await writeStream.WriteAsync(buffer.AsMemory(0, bytesRead));

                while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead > maxBytes)
                    {
                        throw new InvalidOperationException($"The input stream exceeded the limit of {maxBytes} bytes.");
                    }

                    await writeStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                }
            }

            return new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: false);
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    private static async Task<Stream> EnsureSeekableAtZero(Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
            return stream;
        }

        var memoryStream = new MemoryStream();

        await stream.CopyToAsync(memoryStream);

        memoryStream.Position = 0;
        stream.Dispose();

        return memoryStream;
    }

    private static bool LooksLikeZip(Stream stream)
    {
        if (!stream.CanSeek)
        {
            return false;
        }

        var position = stream.Position;

        Span<byte> magic = stackalloc byte[4];
        var bytesRead = stream.Read(magic);

        stream.Position = position;

        return bytesRead == 4 &&
               magic[0] == (byte)'P' &&
               magic[1] == (byte)'K' &&
               magic[2] == 3 &&
               magic[3] == 4;
    }

    private static async Task<string> PeekText(Stream stream, int maxBytes)
    {
        if (!stream.CanSeek)
        {
            return "(non-seekable)";
        }

        var position = stream.Position;
        var toRead = (int)Math.Min(maxBytes, stream.Length - stream.Position);

        var buffer = new byte[toRead];
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, toRead));

        stream.Position = position;

        try
        {
            return Encoding.UTF8.GetString(buffer, 0, bytesRead).Replace("\0", "").Trim();
        }
        catch
        {
            return "(binary or non-UTF8)";
        }
    }

    private static string? GetBaseNameFromUpload(object file)
    {
        if (file is IBrowserFile browserFile)
        {
            return Path.GetFileNameWithoutExtension(browserFile.Name);
        }

        return null;
    }

    private static string MakeSafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "export";
        }

        var safeName = name;

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(invalidCharacter, '_');
        }

        safeName = safeName.Trim('.');

        if (string.IsNullOrWhiteSpace(safeName))
        {
            return "export";
        }

        return safeName;
    }

    private static string ToJson(IEnumerable<IDictionary<string, object?>> rows)
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        return JsonSerializer.Serialize(rows, jsonSerializerOptions);
    }

    private static string ToCsv(IEnumerable<IDictionary<string, object?>> rows)
    {
        var rowList = rows.ToList();

        if (rowList.Count == 0)
        {
            return string.Empty;
        }

        var headers = rowList.First().Keys.ToList();

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(string.Join(",", headers.Select(EscapeCsvValue)));

        foreach (var row in rowList)
        {
            var line = string.Join(
                ",",
                headers.Select(header =>
                    EscapeCsvValue(row.TryGetValue(header, out var value) ? value : null)));

            stringBuilder.AppendLine(line);
        }

        return stringBuilder.ToString();
    }

    private static string EscapeCsvValue(object? value)
    {
        var text = value?.ToString() ?? string.Empty;

        var needsQuotes = text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r');

        text = text.Replace("\"", "\"\"");

        if (needsQuotes)
        {
            return $"\"{text}\"";
        }

        return text;
    }

    private static string ToXml(IEnumerable<IDictionary<string, object?>> rows)
    {
        var xmlWriterSettings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        };

        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, xmlWriterSettings);

        xmlWriter.WriteStartDocument();
        xmlWriter.WriteStartElement("rows");

        foreach (var row in rows)
        {
            xmlWriter.WriteStartElement("row");

            foreach (var keyValuePair in row)
            {
                xmlWriter.WriteStartElement("field");
                xmlWriter.WriteAttributeString("name", keyValuePair.Key);

                if (keyValuePair.Value is not null)
                {
                    xmlWriter.WriteString(keyValuePair.Value.ToString());
                }

                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
        }

        xmlWriter.WriteEndElement();
        xmlWriter.WriteEndDocument();
        xmlWriter.Flush();

        return stringWriter.ToString();
    }
}
