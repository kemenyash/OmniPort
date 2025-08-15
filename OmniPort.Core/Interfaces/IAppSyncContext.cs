using OmniPort.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Interfaces
{
    public interface IAppSyncContext
    {
        event Action? Changed;

        IReadOnlyList<TemplateSummaryDto> Templates { get; }
        IReadOnlyList<BasicTemplateDto> BasicTemplatesFull { get; }
        IReadOnlyList<JoinedTemplateSummaryDto> JoinedTemplates { get; }
        IReadOnlyList<FileConversionHistoryDto> FileConversions { get; }
        IReadOnlyList<UrlConversionHistoryDto> UrlConversions { get; }
        IReadOnlyList<WatchedUrlDto> WatchedUrls { get; }

        Task InitializeAsync(CancellationToken ct = default);

        // Basic templates
        Task CreateBasicTemplateAsync(CreateBasicTemplateDto dto, CancellationToken ct = default);
        Task UpdateBasicTemplateAsync(UpdateBasicTemplateDto dto, CancellationToken ct = default);
        Task DeleteBasicTemplateAsync(int id, CancellationToken ct = default);

        // Mapping templates
        Task CreateMappingTemplateAsync(CreateMappingTemplateDto dto, CancellationToken ct = default);
        Task DeleteMappingTemplateAsync(int mappingId, CancellationToken ct = default);

        // Conversions & Watchlist
        Task AddFileConversionAsync(FileConversionHistoryDto dto, CancellationToken ct = default);
        Task AddUrlConversionAsync(UrlConversionHistoryDto dto, CancellationToken ct = default);
        Task AddWatchedUrlAsync(AddWatchedUrlDto dto, CancellationToken ct = default);

        // Refresh-only (rarely needed if all writes go through this context)
        Task RefreshAllAsync(CancellationToken ct = default);
    }
}
