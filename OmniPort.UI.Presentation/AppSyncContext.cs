using Microsoft.Extensions.DependencyInjection;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                using var scope = serviceProvider.CreateScope();
                var templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();

                templates = (await templateManager.GetBasicTemplatesSummaryAsync()).ToList() ?? new List<TemplateSummaryDto>();
                basicTemplatesFull = new List<BasicTemplateDto>();
                
                foreach (var template in templates)
                {
                    var basicTemplate = await templateManager.GetBasicTemplateAsync(template.Id);
                    if (basicTemplate != null)
                    {
                        basicTemplatesFull.Add(basicTemplate);
                    }
                }

                joinedTemplates = (await templateManager.GetJoinedTemplatesAsync()).ToList() ?? new List<JoinedTemplateSummaryDto>();
                fileConversionsHistory = (await templateManager.GetFileConversionHistoryAsync()).OrderByDescending(x => x.ConvertedAt).ToList() ?? new List<FileConversionHistoryDto>();
                urlConvertsionsHistory = (await templateManager.GetUrlConversionHistoryAsync()).OrderByDescending(x => x.ConvertedAt).ToList() ?? new List<UrlConversionHistoryDto>();
                watchedUrls = (await templateManager.GetWatchedUrlsAsync()).ToList();
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
                using var scope = serviceProvider.CreateScope();
                var templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await templateManager.CreateBasicTemplateAsync(basicTemplateCreation);

                var summaries = (await templateManager.GetBasicTemplatesSummaryAsync()).ToList();
                var newFull = new List<BasicTemplateDto>();
                foreach (var s in summaries)
                {
                    var one = await templateManager.GetBasicTemplateAsync(s.Id);
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
                using var scope = serviceProvider.CreateScope();
                var templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await templateManager.UpdateBasicTemplateAsync(basicTemplateUpdating);

                var updated = await templateManager.GetBasicTemplateAsync(basicTemplateUpdating.Id);
                if (updated != null)
                {
                    var index = basicTemplatesFull.FindIndex(x => x.Id == basicTemplateUpdating.Id);
                    if (index >= 0)
                    {
                        basicTemplatesFull[index] = updated;
                    }
                    else
                    {
                        basicTemplatesFull.Add(updated);
                    }
                }
                
                var summaries = (await templateManager.GetBasicTemplatesSummaryAsync()).ToList();
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
                using var scope = serviceProvider.CreateScope();
                var templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await templateManager.DeleteBasicTemplateAsync(id);

                basicTemplatesFull.RemoveAll(x => x.Id == id);
                templates = (await templateManager.GetBasicTemplatesSummaryAsync()).ToList();

                joinedTemplates = (await templateManager.GetJoinedTemplatesAsync()).ToList();
                fileConversionsHistory = (await templateManager.GetFileConversionHistoryAsync()).OrderByDescending(x => x.ConvertedAt).ToList();
                urlConvertsionsHistory = (await templateManager.GetUrlConversionHistoryAsync()).OrderByDescending(x => x.ConvertedAt).ToList();
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
                using var scope = serviceProvider.CreateScope();
                var templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await templateManager.CreateMappingTemplateAsync(mapingTemplateCreating);

                joinedTemplates = (await templateManager.GetJoinedTemplatesAsync()).ToList();
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
                using var scope = serviceProvider.CreateScope();
                var templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await templateManager.DeleteMappingTemplateAsync(mappingId);

                joinedTemplates = (await templateManager.GetJoinedTemplatesAsync()).ToList();
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
                using var scope = serviceProvider.CreateScope();
                var templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await templateManager.AddFileConversionAsync(fileConversionHistory);
                fileConversionsHistory = (await templateManager.GetFileConversionHistoryAsync()).OrderByDescending(x => x.ConvertedAt).ToList();
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
                using var scope = serviceProvider.CreateScope();
                var tm = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await tm.AddUrlConversionAsync(urlConversionHistory);
                urlConvertsionsHistory = (await tm.GetUrlConversionHistoryAsync()).OrderByDescending(x => x.ConvertedAt).ToList();
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
                using var scope = serviceProvider.CreateScope();
                var templateManager = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await templateManager.AddWatchedUrlAsync(watchedUrlAdding.Url, watchedUrlAdding.IntervalMinutes, watchedUrlAdding.MappingTemplateId);
                watchedUrls = (await templateManager.GetWatchedUrlsAsync()).ToList();
            }
            finally 
            { 
                gate.Release(); 
            }

            Changed?.Invoke();
        }
    }
}
