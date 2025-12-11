using OmniPort.Core.Records;

namespace OmniPort.Core.Interfaces
{
    public interface ITemplateManager
    {
        Task<IReadOnlyList<TemplateSummaryDto>> GetBasicTemplatesSummary();
        Task<BasicTemplateDto?> GetBasicTemplate(int templateId);
        Task<int> CreateBasicTemplate(CreateBasicTemplateDto dto);
        Task<bool> UpdateBasicTemplate(UpdateBasicTemplateDto dto);
        Task<bool> DeleteBasicTemplate(int templateId);

        Task<IReadOnlyList<JoinedTemplateSummaryDto>> GetJoinedTemplates();
        Task<MappingTemplateDto?> GetMappingTemplate(int mappingTemplateId);
        Task<int> CreateMappingTemplate(CreateMappingTemplateDto dto);
        Task<bool> UpdateMappingTemplate(UpdateMappingTemplateDto dto);
        Task<bool> DeleteMappingTemplate(int mappingTemplateId);

        Task<IReadOnlyList<FileConversionHistoryDto>> GetFileConversionHistory();
        Task<IReadOnlyList<UrlConversionHistoryDto>> GetUrlConversionHistory();
        Task AddFileConversion(FileConversionHistoryDto dto);
        Task AddUrlConversion(UrlConversionHistoryDto dto);

        Task<IReadOnlyList<WatchedUrlDto>> GetWatchedUrls();
        Task<int> AddWatchedUrl(string url, int intervalMinutes, int mappingTemplateId);
        Task<bool> DeleteWatchedUrl(int watchedUrlId);
    }
}