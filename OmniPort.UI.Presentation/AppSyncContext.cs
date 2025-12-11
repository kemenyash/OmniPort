using Microsoft.Extensions.DependencyInjection;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Records;

namespace OmniPort.UI.Presentation
{
    public class AppSyncContext : IAppSyncContext
    {
        private readonly IServiceProvider serviceProvider;
        private readonly SemaphoreSlim gate;

        public event Action? Changed;

        private List<TemplateSummaryDto> templates;
        private List<BasicTemplateDto> basicTemplatesFull;
        private List<JoinedTemplateSummaryDto> joinedTemplates;
        private List<FileConversionHistoryDto> fileConversionsHistory;
        private List<UrlConversionHistoryDto> urlConvertsionsHistory;
        private List<WatchedUrlDto> watchedUrls;

        public IReadOnlyList<TemplateSummaryDto> Templates => templates;
        public IReadOnlyList<BasicTemplateDto> BasicTemplatesFull => basicTemplatesFull;
        public IReadOnlyList<JoinedTemplateSummaryDto> JoinedTemplates => joinedTemplates;
        public IReadOnlyList<FileConversionHistoryDto> FileConversions => fileConversionsHistory;
        public IReadOnlyList<UrlConversionHistoryDto> UrlConversions => urlConvertsionsHistory;
        public IReadOnlyList<WatchedUrlDto> WatchedUrls => watchedUrls;

        public AppSyncContext(IServiceProvider serviceProvider)
        {
            gate = new(1, 1);
            this.serviceProvider = serviceProvider;
        }

        public async Task Initialize(CancellationToken ct = default)
        {
            await gate.WaitAsync(ct);
            try
            {
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
            }
            finally
            {
                gate.Release();
            }

            Changed?.Invoke();
        }
    }
}
