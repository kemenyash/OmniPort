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
        private readonly IAppSyncContext applicationSyncContext;
        private readonly ISourceFingerprintStore sourceFingerprintStore;
        private readonly IServiceProvider rootServiceProvider;
        private readonly ILogger<WatchedHashSyncService> serviceLogger;

        private readonly TimeSpan scanPeriod;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> urlLocksByEffectiveUrl;
        private readonly SemaphoreSlim parallelProcessingGate;

        private readonly UrlContentFetcher urlContentFetcher;

        public WatchedHashSyncService(
            IAppSyncContext applicationSyncContext,
            ISourceFingerprintStore sourceFingerprintStore,
            IServiceProvider rootServiceProvider,
            ILogger<WatchedHashSyncService> serviceLogger)
        {
            this.applicationSyncContext = applicationSyncContext;
            this.sourceFingerprintStore = sourceFingerprintStore;
            this.rootServiceProvider = rootServiceProvider;
            this.serviceLogger = serviceLogger;

            scanPeriod = TimeSpan.FromSeconds(20);

            urlLocksByEffectiveUrl = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);
            parallelProcessingGate = new SemaphoreSlim(4, 4);

            urlContentFetcher = new UrlContentFetcher("OmniPort/1.0");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await applicationSyncContext.Initialize(stoppingToken);
            }
            catch (Exception exception)
            {
                serviceLogger.LogError(exception, "AppSyncContext initialization failed");
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
                catch (Exception exception)
                {
                    serviceLogger.LogError(exception, "Unhandled error in watch loop");
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

        private async Task Tick(CancellationToken cancellationToken)
        {
            var watchedUrls = applicationSyncContext.WatchedUrls;
            if (watchedUrls.Count == 0)
            {
                return;
            }

            var utcNow = DateTime.UtcNow;

            foreach (var watchedUrl in watchedUrls)
            {
                var storedUrl = (watchedUrl.Url ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(storedUrl))
                {
                    continue;
                }

                var watchInterval = TimeSpan.FromMinutes(watchedUrl.IntervalMinutes);
                var mappingTemplateId = watchedUrl.MappingTemplateId;

                var lastConversion = applicationSyncContext.UrlConversions
                    .Where(urlConversion =>
                        string.Equals(urlConversion.InputUrl, storedUrl, StringComparison.OrdinalIgnoreCase) &&
                        urlConversion.MappingTemplateId == mappingTemplateId)
                    .OrderByDescending(urlConversion => urlConversion.ConvertedAt)
                    .FirstOrDefault();

                if (lastConversion == null || utcNow - lastConversion.ConvertedAt >= watchInterval)
                {
                    _ = ProcessScheduledUrl(storedUrl, mappingTemplateId, cancellationToken);
                }
            }
        }

        private async Task ProcessScheduledUrl(string storedUrl, int mappingTemplateId, CancellationToken cancellationToken)
        {
            await parallelProcessingGate.WaitAsync(cancellationToken);
            try
            {
                await ProcessUrl(storedUrl, mappingTemplateId, cancellationToken);
            }
            finally
            {
                parallelProcessingGate.Release();
            }
        }

        private async Task ProcessUrl(string storedUrl, int mappingTemplateId, CancellationToken cancellationToken)
        {
            var effectiveUrl = UrlWatchTagger.StripTag(storedUrl);

            var effectiveUrlLock = urlLocksByEffectiveUrl.GetOrAdd(
                effectiveUrl,
                _ => new SemaphoreSlim(1, 1));

            await effectiveUrlLock.WaitAsync(cancellationToken);

            try
            {
                var contentSnapshot = urlContentFetcher.DownloadEffectiveContent(effectiveUrl, cancellationToken);
                if (contentSnapshot.IsEmpty)
                {
                    serviceLogger.LogDebug("Empty content for {Url}", effectiveUrl);
                    return;
                }

                var currentContentHash = Sha256HexGenerator.Compute(contentSnapshot.Bytes);

                var previousContentHash = await sourceFingerprintStore.GetHash(effectiveUrl, mappingTemplateId);
                if (string.Equals(previousContentHash, currentContentHash, StringComparison.Ordinal))
                {
                    serviceLogger.LogDebug("No changes by hash for {Url}", effectiveUrl);
                    return;
                }

                var mappingTemplate = applicationSyncContext.JoinedTemplates.FirstOrDefault(template => template.Id == mappingTemplateId);
                if (mappingTemplate is null)
                {
                    serviceLogger.LogWarning("Mapping {MappingId} not found for {Url}", mappingTemplateId, storedUrl);
                    return;
                }

                var outputExtension = FileToFormatConverter.ToExtension(mappingTemplate.OutputFormat);

                using var serviceScope = rootServiceProvider.CreateScope();
                var transformationExecutionService = serviceScope.ServiceProvider.GetRequiredService<ITransformationExecutionService>();

                var outputLink = await transformationExecutionService.TransformFromUrl(mappingTemplateId, storedUrl, outputExtension);

                var urlConversionHistory = new UrlConversionHistoryDto(
                    Id: 0,
                    ConvertedAt: DateTime.UtcNow,
                    InputUrl: storedUrl,
                    OutputLink: outputLink,
                    MappingTemplateId: mappingTemplateId,
                    MappingTemplateName: string.Empty);

                await applicationSyncContext.AddUrlConversion(urlConversionHistory, cancellationToken);

                await sourceFingerprintStore.SetHash(effectiveUrl, currentContentHash, mappingTemplateId);

                serviceLogger.LogInformation("Updated from {Url} (hash changed)", effectiveUrl);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                serviceLogger.LogError(exception, "Error processing {Url}", storedUrl);
            }
            finally
            {
                effectiveUrlLock.Release();
            }
        }
    }
}
