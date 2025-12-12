using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Records;
using OmniPort.Core.Utilities;
using System.Collections.Concurrent;

namespace OmniPort.UI.Presentation.Services
{
    public class WatchedHashSyncService : BackgroundService
    {
        private readonly IAppSyncContext syncContext;
        private readonly ISourceFingerprintStore fingerprints;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<WatchedHashSyncService> logger;

        private readonly TimeSpan scanPeriod;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> urlLocks;
        private readonly SemaphoreSlim parallelGate;

        private readonly UrlContentFetcher fetcher;

        public WatchedHashSyncService(
            IAppSyncContext sync,
            ISourceFingerprintStore fingerprints,
            IServiceProvider root,
            ILogger<WatchedHashSyncService> log)
        {
            syncContext = sync;
            this.fingerprints = fingerprints;
            serviceProvider = root;
            logger = log;

            scanPeriod = TimeSpan.FromSeconds(20);

            urlLocks = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);
            parallelGate = new SemaphoreSlim(4, 4);

            fetcher = new UrlContentFetcher("OmniPort/1.0");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await syncContext.Initialize(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AppSyncContext initialization failed");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Tick(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unhandled error in watch loop");
                }

                try
                {
                    await Task.Delay(scanPeriod, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private async Task Tick(CancellationToken ct)
        {
            var watchedUrls = syncContext.WatchedUrls;
            if (watchedUrls.Count == 0) return;

            var dateTimeNow = DateTime.UtcNow;

            foreach (WatchedUrlDto watched in watchedUrls)
            {
                string storedUrl = (watched.Url ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(storedUrl)) continue;

                var interval = TimeSpan.FromMinutes(watched.IntervalMinutes);
                int mapId = watched.MappingTemplateId;

                var last = syncContext.UrlConversions
                    .Where(x => string.Equals(x.InputUrl, storedUrl, StringComparison.OrdinalIgnoreCase)
                             && x.MappingTemplateId == mapId)
                    .OrderByDescending(x => x.ConvertedAt)
                    .FirstOrDefault();

                if (last == null || dateTimeNow - last.ConvertedAt >= interval)
                {
                    _ = ProcessOneScheduled(storedUrl, mapId, ct);
                }
            }
        }

        private async Task ProcessOneScheduled(string storedUrl, int mappingTemplateId, CancellationToken ct)
        {
            await parallelGate.WaitAsync(ct);
            try
            {
                await ProcessOne(storedUrl, mappingTemplateId, ct);
            }
            finally
            {
                parallelGate.Release();
            }
        }

        private async Task ProcessOne(string storedUrl, int mappingTemplateId, CancellationToken ct)
        {
            string effectiveUrl = UrlWatchTagger.StripTag(storedUrl);

            var gate = urlLocks.GetOrAdd(effectiveUrl, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(ct);

            try
            {
                var snap = fetcher.DownloadEffectiveContent(effectiveUrl, ct);
                if (snap.IsEmpty)
                {
                    logger.LogDebug("Empty content for {Url}", effectiveUrl);
                    return;
                }

                string currentHash = Sha256HexGenerator.Compute(snap.Bytes);

                string? prevHash = await fingerprints.GetHash(effectiveUrl, mappingTemplateId, ct);
                if (string.Equals(prevHash, currentHash, StringComparison.Ordinal))
                {
                    logger.LogDebug("No changes by hash for {Url}", effectiveUrl);
                    return;
                }

                var mapping = syncContext.JoinedTemplates.FirstOrDefault(x => x.Id == mappingTemplateId);
                if (mapping is null)
                {
                    logger.LogWarning("Mapping {MappingId} not found for {Url}", mappingTemplateId, storedUrl);
                    return;
                }

                var extension = FileToFormatConverter.ToExtension(mapping.OutputFormat);

                using IServiceScope scope = serviceProvider.CreateScope();
                var executor = scope.ServiceProvider.GetRequiredService<ITransformationExecutionService>();

                var output = await executor.TransformFromUrlAsync(mappingTemplateId, storedUrl, extension);

                await syncContext.AddUrlConversion(new UrlConversionHistoryDto(
                    Id: 0,
                    ConvertedAt: DateTime.UtcNow,
                    InputUrl: storedUrl,
                    OutputLink: output,
                    MappingTemplateId: mappingTemplateId,
                    MappingTemplateName: string.Empty
                ), ct);

                await fingerprints.SetHash(effectiveUrl, currentHash, mappingTemplateId, ct);

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
    }
}
