using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Records;
using OmniPort.Core.Utilities;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Services
{
    public sealed class WatchedHashSyncService : BackgroundService
    {
        private readonly IAppSyncContext _sync;                
        private readonly IHttpClientFactory _httpFactory;       
        private readonly ISourceFingerprintStore _fingerprints; 
        private readonly IServiceProvider _root;                
        private readonly ILogger<WatchedHashSyncService> _log;

        private readonly TimeSpan _scanPeriod = TimeSpan.FromSeconds(20);
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);

        public WatchedHashSyncService(
            IAppSyncContext sync,
            IHttpClientFactory httpFactory,
            ISourceFingerprintStore fingerprints,
            IServiceProvider root,
            ILogger<WatchedHashSyncService> log)
        {
            _sync = sync;
            _httpFactory = httpFactory;
            _fingerprints = fingerprints;
            _root = root;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try { await _sync.InitializeAsync(stoppingToken); }
            catch (Exception ex) { _log.LogError(ex, "AppSyncContext initialization failed"); }

            while (!stoppingToken.IsCancellationRequested)
            {
                try { await TickAsync(stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
                catch (Exception ex) { _log.LogError(ex, "Unhandled error in watch loop"); }

                try { await Task.Delay(_scanPeriod, stoppingToken); }
                catch (OperationCanceledException) { }
            }
        }

        private async Task TickAsync(CancellationToken ct)
        {
            var watched = _sync.WatchedUrls;
            if (watched.Count == 0) return;

            var urls = watched
                .GroupBy(w => (w.Url ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => new { Url = g.Key, Interval = TimeSpan.FromMinutes(g.Min(x => x.IntervalMinutes)) })
                .ToList();

            var now = DateTime.UtcNow;

            foreach (var w in urls)
            {
                var lastConv = _sync.UrlConversions
                    .Where(x => string.Equals(x.InputUrl, w.Url, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.ConvertedAt)
                    .FirstOrDefault();

                if (lastConv == null)
                {
                    var mappingId = ResolveMappingTemplateId(w.Url, null);
                    if (mappingId is not null)
                        _ = Task.Run(() => ProcessOneAsync(w.Url, mappingId.Value, ct), ct);

                    continue;
                }

                var lastAt = lastConv.ConvertedAt;
                if (now - lastAt < w.Interval) continue;

                var mappingIdExisting = ResolveMappingTemplateId(w.Url, lastConv);
                if (mappingIdExisting is not null)
                    _ = Task.Run(() => ProcessOneAsync(w.Url, mappingIdExisting.Value, ct), ct);
            }
        }


        private int? ResolveMappingTemplateId(string url, UrlConversionHistoryDto? lastConv)
        {
            if (lastConv != null) return lastConv.MappingTemplateId;
            if (_sync.JoinedTemplates.Count == 1) return _sync.JoinedTemplates[0].Id;
            return null; 
        }

        private async Task ProcessOneAsync(string url, int mappingTemplateId, CancellationToken ct)
        {
            var gate = _locks.GetOrAdd(url, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(ct);
            try
            {
                var http = _httpFactory.CreateClient();
                using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    _log.LogError("Error fetching {Url}: {StatusCode} {ReasonPhrase}",
                        url, resp.StatusCode, resp.ReasonPhrase);
                    return;
                }

                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                var currentHash = await ComputeSha256HexAsync(stream, ct);

                var prevHash = await _fingerprints.GetHashAsync(url, mappingTemplateId, ct);
                if (string.Equals(prevHash, currentHash, StringComparison.Ordinal))
                {
                    _log.LogDebug("No changes by hash for {Url}", url);
                    return;
                }

                var mapping = _sync.JoinedTemplates.FirstOrDefault(x => x.Id == mappingTemplateId);
                if (mapping is null)
                {
                    _log.LogWarning("Mapping {MappingId} not found for {Url}", mappingTemplateId, url);
                    return;
                }

                var ext = FileToFormatConverter.ToExtension(mapping.OutputFormat);

                using var scope = _root.CreateScope();
                var executor = scope.ServiceProvider.GetRequiredService<ITransformationExecutionService>();

                var output = await executor.TransformFromUrlAsync(mappingTemplateId, url, ext);

                await _sync.AddUrlConversionAsync(new UrlConversionHistoryDto(
                    Id: 0,
                    ConvertedAt: DateTime.UtcNow,
                    InputUrl: url,
                    OutputLink: output,
                    MappingTemplateId: mappingTemplateId,
                    MappingTemplateName: string.Empty
                ), ct);

                await _fingerprints.SetHashAsync(url, currentHash, mappingTemplateId, ct);

                _log.LogInformation("Updated from {Url} (hash changed)", url);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error processing {Url}", url);
            }
            finally
            {
                gate.Release();
            }
        }

        private static async Task<string> ComputeSha256HexAsync(Stream stream, CancellationToken ct)
        {
            using var sha = SHA256.Create();
            var buffer = ArrayPool<byte>.Shared.Rent(81920);
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
    }
}
