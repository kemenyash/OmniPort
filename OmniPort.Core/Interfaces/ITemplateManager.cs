using OmniPort.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Interfaces
{
    public interface ITemplateManager
    {
        Task<IReadOnlyList<TemplateSummaryDto>> GetBasicTemplatesSummaryAsync();
        Task<BasicTemplateDto?> GetBasicTemplateAsync(int templateId);
        Task<int> CreateBasicTemplateAsync(CreateBasicTemplateDto dto);
        Task<bool> UpdateBasicTemplateAsync(UpdateBasicTemplateDto dto);
        Task<bool> DeleteBasicTemplateAsync(int templateId);

        Task<IReadOnlyList<JoinedTemplateSummaryDto>> GetJoinedTemplatesAsync();
        Task<MappingTemplateDto?> GetMappingTemplateAsync(int mappingTemplateId);
        Task<int> CreateMappingTemplateAsync(CreateMappingTemplateDto dto);
        Task<bool> UpdateMappingTemplateAsync(UpdateMappingTemplateDto dto);
        Task<bool> DeleteMappingTemplateAsync(int mappingTemplateId);

        Task<IReadOnlyList<FileConversionHistoryDto>> GetFileConversionHistoryAsync();
        Task<IReadOnlyList<UrlConversionHistoryDto>> GetUrlConversionHistoryAsync();
        Task AddFileConversionAsync(FileConversionHistoryDto dto);
        Task AddUrlConversionAsync(UrlConversionHistoryDto dto);

        Task<IReadOnlyList<WatchedUrlDto>> GetWatchedUrlsAsync();
        Task<int> AddWatchedUrlAsync(AddWatchedUrlDto dto);
        Task<bool> DeleteWatchedUrlAsync(int watchedUrlId);
    }
}