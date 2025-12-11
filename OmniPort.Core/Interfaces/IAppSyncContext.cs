using OmniPort.Core.Records;

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

        Task Initialize(CancellationToken ct = default);

        Task CreateBasicTemplate(CreateBasicTemplateDto dto, CancellationToken ct = default);
        Task UpdateBasicTemplate(UpdateBasicTemplateDto dto, CancellationToken ct = default);
        Task DeleteBasicTemplate(int id, CancellationToken ct = default);

        Task CreateMappingTemplate(CreateMappingTemplateDto dto, CancellationToken ct = default);
        Task DeleteMappingTemplate(int mappingId, CancellationToken ct = default);

        Task AddFileConversion(FileConversionHistoryDto dto, CancellationToken ct = default);
        Task AddUrlConversion(UrlConversionHistoryDto dto, CancellationToken ct = default);
        Task AddWatchedUrl(AddWatchedUrlDto dto, CancellationToken ct = default);

        Task RefreshAll(CancellationToken ct = default);
    }
}