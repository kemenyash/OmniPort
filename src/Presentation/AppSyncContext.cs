using Microsoft.Extensions.DependencyInjection;
using BusinessLogic.Interfaces;
using BusinessLogic.Records;
using Microsoft.Extensions.Logging;

namespace Presentation
{
    public class AppSyncContext : IAppSyncContext
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<AppSyncContext> logger;
        private readonly SemaphoreSlim gate;

        public event Action? Changed;

        private List<TemplateSummaryDto> templates = new();
        private List<BasicTemplateDto> basicTemplatesFull = new();
        private List<JoinedTemplateSummaryDto> joinedTemplates = new();
        private List<FileConversionHistoryDto> fileConversionsHistory = new();
        private List<UrlConversionHistoryDto> urlConvertsionsHistory = new();
        private List<WatchedUrlDto> watchedUrls = new();

        public IReadOnlyList<TemplateSummaryDto> Templates => templates;
        public IReadOnlyList<BasicTemplateDto> BasicTemplatesFull => basicTemplatesFull;
        public IReadOnlyList<JoinedTemplateSummaryDto> JoinedTemplates => joinedTemplates;
        public IReadOnlyList<FileConversionHistoryDto> FileConversions => fileConversionsHistory;
        public IReadOnlyList<UrlConversionHistoryDto> UrlConversions => urlConvertsionsHistory;
        public IReadOnlyList<WatchedUrlDto> WatchedUrls => watchedUrls;

        public AppSyncContext(
            IServiceProvider serviceProvider,
            ILogger<AppSyncContext> logger)
        {
            gate = new(1, 1);
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }

        public async Task Initialize(CancellationToken ct = default)
        {
            await gate.WaitAsync(ct);
            try
            {
                logger.LogInformation("Refreshing application sync context");
                using IServiceScope scope = serviceProvider.CreateScope();
                ITemplateManager templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();

                templates = (await templateManager.GetBasicTemplatesSummary()).ToList() ?? new List<TemplateSummaryDto>();
                basicTemplatesFull = new List<BasicTemplateDto>();

                foreach (TemplateSummaryDto template in templates)
                {
                    BasicTemplateDto? basicTemplate = await templateManager.GetBasicTemplate(template.Id);
                    if (basicTemplate != null)
                    {
                        basicTemplatesFull.Add(basicTemplate);
                    }
                }

                joinedTemplates = (await templateManager.GetJoinedTemplates()).ToList() ?? new List<JoinedTemplateSummaryDto>();
                fileConversionsHistory = (await templateManager.GetFileConversionHistory()).OrderByDescending(x => x.ConvertedAt).ToList() ?? new List<FileConversionHistoryDto>();
                urlConvertsionsHistory = (await templateManager.GetUrlConversionHistory()).OrderByDescending(x => x.ConvertedAt).ToList() ?? new List<UrlConversionHistoryDto>();
                watchedUrls = (await templateManager.GetWatchedUrls()).ToList();
                logger.LogInformation(
                    "Application sync context refreshed: {TemplateCount} templates, {MappingCount} mappings, {WatchedUrlCount} watched URLs",
                    templates.Count,
                    joinedTemplates.Count,
                    watchedUrls.Count);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Application sync context refresh failed");
                throw;
            }
            finally
            {
                gate.Release();
            }
            Changed?.Invoke();
        }
        public async Task RefreshAll(CancellationToken ct = default)
        {
            await Initialize(ct);
        }

        public async Task CreateBasicTemplate(CreateBasicTemplateDto basicTemplateCreation, CancellationToken ct = default)
        {
            await gate.WaitAsync(ct);
            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                ITemplateManager templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await templateManager.CreateBasicTemplate(basicTemplateCreation);

                List<TemplateSummaryDto> summaries = (await templateManager.GetBasicTemplatesSummary()).ToList();
                List<BasicTemplateDto> newFull = new List<BasicTemplateDto>();
                foreach (TemplateSummaryDto? s in summaries)
                {
                    BasicTemplateDto? one = await templateManager.GetBasicTemplate(s.Id);
                    if (one != null) newFull.Add(one);
                }
                templates = summaries;
                basicTemplatesFull = newFull;
                logger.LogInformation("Application sync context created basic template {TemplateName}", basicTemplateCreation.Name);
            }
            finally
            {
                gate.Release();
            }

            Changed?.Invoke();
        }
        public async Task UpdateBasicTemplate(UpdateBasicTemplateDto basicTemplateUpdating, CancellationToken ct = default)
        {
            await gate.WaitAsync(ct);
            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                ITemplateManager templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await templateManager.UpdateBasicTemplate(basicTemplateUpdating);

                BasicTemplateDto? updated = await templateManager.GetBasicTemplate(basicTemplateUpdating.Id);
                if (updated != null)
                {
                    int index = basicTemplatesFull.FindIndex(x => x.Id == basicTemplateUpdating.Id);
                    if (index >= 0)
                    {
                        basicTemplatesFull[index] = updated;
                    }
                    else
                    {
                        basicTemplatesFull.Add(updated);
                    }
                }

                List<TemplateSummaryDto> summaries = (await templateManager.GetBasicTemplatesSummary()).ToList();
                templates = summaries;
                logger.LogInformation("Application sync context updated basic template {TemplateId}", basicTemplateUpdating.Id);
            }
            finally
            {
                gate.Release();
            }

            Changed?.Invoke();
        }
        public async Task DeleteBasicTemplate(int id, CancellationToken ct = default)
        {
            await gate.WaitAsync(ct);
            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                ITemplateManager templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await templateManager.DeleteBasicTemplate(id);

                basicTemplatesFull.RemoveAll(x => x.Id == id);
                templates = (await templateManager.GetBasicTemplatesSummary()).ToList();

                joinedTemplates = (await templateManager.GetJoinedTemplates()).ToList();
                fileConversionsHistory = (await templateManager.GetFileConversionHistory()).OrderByDescending(x => x.ConvertedAt).ToList();
                urlConvertsionsHistory = (await templateManager.GetUrlConversionHistory()).OrderByDescending(x => x.ConvertedAt).ToList();
                logger.LogInformation("Application sync context deleted basic template {TemplateId}", id);
            }
            finally
            {
                gate.Release();
            }

            Changed?.Invoke();
        }

        public async Task CreateMappingTemplate(CreateMappingTemplateDto mapingTemplateCreating, CancellationToken ct = default)
        {
            await gate.WaitAsync(ct);

            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                ITemplateManager templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await templateManager.CreateMappingTemplate(mapingTemplateCreating);

                joinedTemplates = (await templateManager.GetJoinedTemplates()).ToList();
                logger.LogInformation("Application sync context created mapping template {TemplateName}", mapingTemplateCreating.Name);
            }
            finally
            {
                gate.Release();
            }

            Changed?.Invoke();
        }
        public async Task DeleteMappingTemplate(int mappingId, CancellationToken ct = default)
        {
            await gate.WaitAsync(ct);

            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                ITemplateManager templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await templateManager.DeleteMappingTemplate(mappingId);

                joinedTemplates = (await templateManager.GetJoinedTemplates()).ToList();
                logger.LogInformation("Application sync context deleted mapping template {MappingTemplateId}", mappingId);
            }
            finally
            {
                gate.Release();
            }

            Changed?.Invoke();
        }

        public async Task AddFileConversion(FileConversionHistoryDto fileConversionHistory, CancellationToken ct = default)
        {
            await gate.WaitAsync(ct);
            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                ITemplateManager templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await templateManager.AddFileConversion(fileConversionHistory);
                fileConversionsHistory = (await templateManager.GetFileConversionHistory()).OrderByDescending(x => x.ConvertedAt).ToList();
                logger.LogInformation("Application sync context added file conversion for {FileName}", fileConversionHistory.FileName);
            }
            finally
            {
                gate.Release();
            }

            Changed?.Invoke();
        }
        public async Task AddUrlConversion(UrlConversionHistoryDto urlConversionHistory, CancellationToken ct = default)
        {
            await gate.WaitAsync(ct);

            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                ITemplateManager tm = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await tm.AddUrlConversion(urlConversionHistory);
                urlConvertsionsHistory = (await tm.GetUrlConversionHistory()).OrderByDescending(x => x.ConvertedAt).ToList();
                logger.LogInformation("Application sync context added URL conversion for {InputUrl}", urlConversionHistory.InputUrl);
            }
            finally
            {
                gate.Release();
            }

            Changed?.Invoke();
        }

        public async Task AddWatchedUrl(AddWatchedUrlDto watchedUrlAdding, CancellationToken ct = default)
        {
            await gate.WaitAsync(ct);
            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                ITemplateManager templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await templateManager.AddWatchedUrl(watchedUrlAdding.Url, watchedUrlAdding.IntervalMinutes, watchedUrlAdding.MappingTemplateId);
                watchedUrls = (await templateManager.GetWatchedUrls()).ToList();
                logger.LogInformation("Application sync context added watched URL {Url}", watchedUrlAdding.Url);
            }
            finally
            {
                gate.Release();
            }

            Changed?.Invoke();
        }
    }
}
