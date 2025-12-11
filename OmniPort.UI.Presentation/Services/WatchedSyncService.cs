using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Records;
using OmniPort.Core.Utilities;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace OmniPort.UI.Presentation.Services
{
    public sealed class WatchedHashSyncService : BackgroundService
    {
        private readonly IAppSyncContext syncContext;
        private readonly IHttpClientFactory httpFactory;
        private readonly ISourceFingerprintStore fingerprints;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<WatchedHashSyncService> logger;

        private readonly TimeSpan scanPeriod = TimeSpan.FromSeconds(20);
        private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new(StringComparer.OrdinalIgnoreCase);

        public WatchedHashSyncService(
            IAppSyncContext sync,
            IHttpClientFactory httpFactory,
            ISourceFingerprintStore fingerprints,
            IServiceProvider root,
            ILogger<WatchedHashSyncService> log)
        {
            syncContext = sync;
            this.httpFactory = httpFactory;
            this.fingerprints = fingerprints;
            serviceProvider = root;
            logger = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try { await syncContext.Initialize(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "AppSyncContext initialization failed"); }

            while (!stoppingToken.IsCancellationRequested)
            {
                try { await TickAsync(stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
                catch (Exception ex) { logger.LogError(ex, "Unhandled error in watch loop"); }

                try { await Task.Delay(scanPeriod, stoppingToken); }
                catch (OperationCanceledException) { }
            }
        }

        private async Task TickAsync(CancellationToken ct)
        {
            IReadOnlyList<WatchedUrlDto> watched = syncContext.WatchedUrls;
            if (watched.Count == 0) return;

            DateTime now = DateTime.UtcNow;

            foreach (WatchedUrlDto w in watched)
            {
                string storedUrl = (w.Url ?? string.Empty).Trim();
                TimeSpan interval = TimeSpan.FromMinutes(w.IntervalMinutes);

                int mapId = w.MappingTemplateId;

                UrlConversionHistoryDto? lastConv = syncContext.UrlConversions
                    .Where(x =>
                        string.Equals(x.InputUrl, storedUrl, StringComparison.OrdinalIgnoreCase) &&
                        x.MappingTemplateId == mapId)
                    .OrderByDescending(x => x.ConvertedAt)
                    .FirstOrDefault();

                if (lastConv == null || now - lastConv.ConvertedAt >= interval)
                {
                    _ = Task.Run(() => ProcessOneAsync(storedUrl, mapId, ct), ct);
                }
            }
        }

        private async Task ProcessOneAsync(string storedUrl, int mappingTemplateId, CancellationToken ct)
        {
            string effectiveUrl = UrlWatchTagger.StripTag(storedUrl);
            string gateKey = effectiveUrl;
            SemaphoreSlim gate = locks.GetOrAdd(gateKey, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(ct);

            try
            {
                using SocketsHttpHandler handler = new SocketsHttpHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 10,
                    UseCookies = true,
                    CookieContainer = new CookieContainer()
                };
                using HttpClient http = new HttpClient(handler, disposeHandler: true);

                (Stream? contentStream, string? contentType) = await DownloadEffectiveContentAsync(http, effectiveUrl, ct);
                await using (contentStream)
                {
                    string currentHash = await ComputeSha256HexAsync(contentStream, ct);

                    string? prevHash = await fingerprints.GetHash(effectiveUrl, mappingTemplateId, ct);
                    if (string.Equals(prevHash, currentHash, StringComparison.Ordinal))
                    {
                        logger.LogDebug("No changes by hash for {Url}", effectiveUrl);
                        return;
                    }
                }

                JoinedTemplateSummaryDto? mapping = syncContext.JoinedTemplates.FirstOrDefault(x => x.Id == mappingTemplateId);
                if (mapping is null)
                {
                    logger.LogWarning("Mapping {MappingId} not found for {Url}", mappingTemplateId, storedUrl);
                    return;
                }

                string ext = FileToFormatConverter.ToExtension(mapping.OutputFormat);

                using IServiceScope scope = serviceProvider.CreateScope();
                ITransformationExecutionService executor = scope.ServiceProvider.GetRequiredService<ITransformationExecutionService>();

                string output = await executor.TransformFromUrlAsync(mappingTemplateId, storedUrl, ext);

                await syncContext.AddUrlConversion(new UrlConversionHistoryDto(
                    Id: 0,
                    ConvertedAt: DateTime.UtcNow,
                    InputUrl: storedUrl,
                    OutputLink: output,
                    MappingTemplateId: mappingTemplateId,
                    MappingTemplateName: string.Empty
                ), ct);

                string hash = await RecomputeHashForUrlAsync(http, effectiveUrl, ct);
                await fingerprints.SetHash(effectiveUrl, hash, mappingTemplateId, ct);

                logger.LogInformation("Updated from {Url} (hash changed)", effectiveUrl);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing {Url}", storedUrl);
            }
            finally
            {
                gate.Release();
            }
        }

        private static async Task<(Stream stream, string contentType)> DownloadEffectiveContentAsync(HttpClient http, string effectiveUrl, CancellationToken ct)
        {
            using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, effectiveUrl);
            req.Headers.UserAgent.ParseAdd("OmniPort/1.0");
            req.Headers.Accept.ParseAdd("*/*");

            using HttpResponseMessage resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            string ctHeader = resp.Content.Headers.ContentType?.MediaType ?? string.Empty;
            Stream s1 = await resp.Content.ReadAsStreamAsync(ct);

            Stream buffered = await BufferToSeekableAsync(s1, ct);

            if (IsHtml(ctHeader, await PeekTextAsync(buffered, 4096)))
            {
                Uri? maybeLink = TryFindDirectFileLink(await PeekTextAsync(buffered, 512 * 1024), new Uri(effectiveUrl));
                if (maybeLink is not null)
                {
                    using HttpRequestMessage req2 = new HttpRequestMessage(HttpMethod.Get, maybeLink);
                    req2.Headers.UserAgent.ParseAdd("OmniPort/1.0");
                    req2.Headers.Referrer = new Uri(effectiveUrl);
                    req2.Headers.Accept.ParseAdd("*/*");

                    using HttpResponseMessage resp2 = await http.SendAsync(req2, HttpCompletionOption.ResponseHeadersRead, ct);
                    resp2.EnsureSuccessStatusCode();

                    Stream s2 = await resp2.Content.ReadAsStreamAsync(ct);
                    Stream buffered2 = await BufferToSeekableAsync(s2, ct);
                    return (buffered2, resp2.Content.Headers.ContentType?.MediaType ?? string.Empty);
                }
            }

            return (buffered, ctHeader);

            static bool IsHtml(string contentType, string head)
                => contentType.Contains("html", StringComparison.OrdinalIgnoreCase)
                   || head.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase)
                   || head.Contains("<html", StringComparison.OrdinalIgnoreCase);
        }

        private static Uri? TryFindDirectFileLink(string html, Uri baseUri)
        {
            string? href = null;
            if (IndexOfHrefWithExtensions(html, out href, new[] { ".xlsx", ".csv", ".json", ".xml" }) < 0)
                return null;

            return Uri.TryCreate(baseUri, href, out Uri? abs) ? abs : null;

            static int IndexOfHrefWithExtensions(string html, out string? href, string[] exts)
            {
                href = null;
                string lower = html.ToLowerInvariant();
                string key = "href";
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

                    if (exts.Any(e => candidate.Contains(e, StringComparison.OrdinalIgnoreCase)))
                    {
                        href = candidate.Trim();
                        return pos;
                    }
                }
                return -1;
            }
        }

        private static async Task<Stream> BufferToSeekableAsync(Stream src, CancellationToken ct)
        {
            MemoryStream ms = new MemoryStream();
            await src.CopyToAsync(ms, ct);
            ms.Position = 0;
            return ms;
        }

        private static async Task<string> RecomputeHashForUrlAsync(HttpClient http, string effectiveUrl, CancellationToken ct)
        {
            (Stream? stream, string _) = await DownloadEffectiveContentAsync(http, effectiveUrl, ct);
            await using (stream)
            {
                return await ComputeSha256HexAsync(stream, ct);
            }
        }

        private static async Task<string> ComputeSha256HexAsync(Stream stream, CancellationToken ct)
        {
            using SHA256 sha = SHA256.Create();
            byte[] buffer = ArrayPool<byte>.Shared.Rent(81920);
            try
            {
                int read;
                while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                    sha.TransformBlock(buffer, 0, read, null, 0);

                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return Convert.ToHexString(sha.Hash!);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static async Task<string> PeekTextAsync(Stream s, int maxBytes)
        {
            if (!s.CanSeek) return "(non-seekable)";
            long pos = s.Position;
            int toRead = (int)Math.Min(maxBytes, s.Length - s.Position);
            byte[] buf = new byte[toRead];
            int read = await s.ReadAsync(buf.AsMemory(0, toRead));
            s.Position = pos;
            try { return Encoding.UTF8.GetString(buf, 0, read).Replace("\0", "").Trim(); }
            catch { return "(binary or non-UTF8)"; }
        }
    }
}
