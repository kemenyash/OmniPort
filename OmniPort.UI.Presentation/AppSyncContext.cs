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
    public sealed class AppSyncContext : IAppSyncContext
    {
        private readonly IServiceProvider _root;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public event Action? Changed;

        // Backing stores (mutable only inside the lock)
        private List<TemplateSummaryDto> _templates = new();
        private List<BasicTemplateDto> _basicTemplatesFull = new();
        private List<JoinedTemplateSummaryDto> _joined = new();
        private List<FileConversionHistoryDto> _fileHist = new();
        private List<UrlConversionHistoryDto> _urlHist = new();
        private List<WatchedUrlDto> _watched = new();

        // Read-only views for UI binding
        public IReadOnlyList<TemplateSummaryDto> Templates => _templates;
        public IReadOnlyList<BasicTemplateDto> BasicTemplatesFull => _basicTemplatesFull;
        public IReadOnlyList<JoinedTemplateSummaryDto> JoinedTemplates => _joined;
        public IReadOnlyList<FileConversionHistoryDto> FileConversions => _fileHist;
        public IReadOnlyList<UrlConversionHistoryDto> UrlConversions => _urlHist;
        public IReadOnlyList<WatchedUrlDto> WatchedUrls => _watched;

        public AppSyncContext(IServiceProvider root) => _root = root;

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                using var scope = _root.CreateScope();
                var tm = scope.ServiceProvider.GetRequiredService<ITemplateManager>();

                var templates = (await tm.GetBasicTemplatesSummaryAsync()).ToList();
                var full = new List<BasicTemplateDto>();
                foreach (var t in templates) // fan-out kept to preserve existing API
                {
                    var dto = await tm.GetBasicTemplateAsync(t.Id);
                    if (dto != null) full.Add(dto);
                }

                var joined = (await tm.GetJoinedTemplatesAsync()).ToList();
                var fileH = (await tm.GetFileConversionHistoryAsync()).OrderByDescending(x => x.ConvertedAt).ToList();
                var urlH = (await tm.GetUrlConversionHistoryAsync()).OrderByDescending(x => x.ConvertedAt).ToList();
                var watched = (await tm.GetWatchedUrlsAsync()).ToList();

                _templates = templates;
                _basicTemplatesFull = full;
                _joined = joined;
                _fileHist = fileH;
                _urlHist = urlH;
                _watched = watched;
            }
            finally
            {
                _gate.Release();
            }
            Changed?.Invoke();
        }

        public async Task RefreshAllAsync(CancellationToken ct = default) => await InitializeAsync(ct);

        // ---------- Basic Templates ----------

        public async Task CreateBasicTemplateAsync(CreateBasicTemplateDto dto, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                using var scope = _root.CreateScope();
                var tm = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await tm.CreateBasicTemplateAsync(dto);

                // refresh minimal sets affected
                var summaries = (await tm.GetBasicTemplatesSummaryAsync()).ToList();
                var newFull = new List<BasicTemplateDto>();
                foreach (var s in summaries)
                {
                    var one = await tm.GetBasicTemplateAsync(s.Id);
                    if (one != null) newFull.Add(one);
                }
                _templates = summaries;
                _basicTemplatesFull = newFull;
            }
            finally { _gate.Release(); }
            Changed?.Invoke();
        }

        public async Task UpdateBasicTemplateAsync(UpdateBasicTemplateDto dto, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                using var scope = _root.CreateScope();
                var tm = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await tm.UpdateBasicTemplateAsync(dto);

                // update caches
                var updated = await tm.GetBasicTemplateAsync(dto.Id);
                if (updated != null)
                {
                    var idx = _basicTemplatesFull.FindIndex(x => x.Id == dto.Id);
                    if (idx >= 0) _basicTemplatesFull[idx] = updated;
                    else _basicTemplatesFull.Add(updated);
                }
                var summaries = (await tm.GetBasicTemplatesSummaryAsync()).ToList();
                _templates = summaries;
            }
            finally { _gate.Release(); }
            Changed?.Invoke();
        }

        public async Task DeleteBasicTemplateAsync(int id, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                using var scope = _root.CreateScope();
                var tm = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await tm.DeleteBasicTemplateAsync(id);

                _basicTemplatesFull.RemoveAll(x => x.Id == id);
                _templates = (await tm.GetBasicTemplatesSummaryAsync()).ToList();

                _joined = (await tm.GetJoinedTemplatesAsync()).ToList();
                _fileHist = (await tm.GetFileConversionHistoryAsync()).OrderByDescending(x => x.ConvertedAt).ToList();
                _urlHist = (await tm.GetUrlConversionHistoryAsync()).OrderByDescending(x => x.ConvertedAt).ToList();
            }
            finally { _gate.Release(); }
            Changed?.Invoke();
        }

        // ---------- Mapping Templates ----------

        public async Task CreateMappingTemplateAsync(CreateMappingTemplateDto dto, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                using var scope = _root.CreateScope();
                var tm = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await tm.CreateMappingTemplateAsync(dto);

                _joined = (await tm.GetJoinedTemplatesAsync()).ToList();
            }
            finally { _gate.Release(); }
            Changed?.Invoke();
        }

        public async Task DeleteMappingTemplateAsync(int mappingId, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                using var scope = _root.CreateScope();
                var tm = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await tm.DeleteMappingTemplateAsync(mappingId);

                _joined = (await tm.GetJoinedTemplatesAsync()).ToList();
            }
            finally { _gate.Release(); }
            Changed?.Invoke();
        }

        // ---------- Conversions & Watchlist ----------

        public async Task AddFileConversionAsync(FileConversionHistoryDto dto, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                using var scope = _root.CreateScope();
                var tm = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await tm.AddFileConversionAsync(dto);
                _fileHist = (await tm.GetFileConversionHistoryAsync()).OrderByDescending(x => x.ConvertedAt).ToList();
            }
            finally { _gate.Release(); }
            Changed?.Invoke();
        }

        public async Task AddUrlConversionAsync(UrlConversionHistoryDto dto, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                using var scope = _root.CreateScope();
                var tm = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await tm.AddUrlConversionAsync(dto);
                _urlHist = (await tm.GetUrlConversionHistoryAsync()).OrderByDescending(x => x.ConvertedAt).ToList();
            }
            finally { _gate.Release(); }
            Changed?.Invoke();
        }

        public async Task AddWatchedUrlAsync(AddWatchedUrlDto dto, CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                using var scope = _root.CreateScope();
                var tm = scope.ServiceProvider.GetRequiredService<ITemplateManager>();
                await tm.AddWatchedUrlAsync(dto);
                _watched = (await tm.GetWatchedUrlsAsync()).ToList();
            }
            finally { _gate.Release(); }
            Changed?.Invoke();
        }
    }
}
