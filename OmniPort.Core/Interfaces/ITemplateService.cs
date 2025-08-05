using OmniPort.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Interfaces
{
    public interface ITemplateService
    {
        Task<List<string>> GetTemplateNamesAsync();
        Task SaveMappingAsync(ImportProfile profile, int sourceTemplateId);
        Task<List<JoinedTemplateSummary>> GetJoinedTemplatesAsync();
        Task<List<ImportTemplate>> GetTemplatesAsync();
        Task DeleteJoinTemplateAsync(int joinTemplateId);
        Task<ImportTemplate?> GetTemplateAsync(int templateId);
        Task<List<ImportProfile>> GetImportProfilesByTemplateIdAsync(int templateId);
        Task<bool> DeleteTemplateByIdAsync(int templateId);
        Task<bool> UpdateTemplateByIdAsync(int templateId, ImportTemplate updatedTemplate, List<FieldMapping> fields);
        Task<ImportTemplate>GetTemplateByIdAsync(int templateId);

        Task<List<TemplateSummary>> GetTemplatesSummaryAsync();
        Task<int> CreateTemplateAsync(ImportTemplate template, SourceType sourceType, List<FieldMapping> fields);
        Task SaveTemplateMappingAsync(int sourceTemplateId, int targetTemplateId, Dictionary<int, int?> targetToSourceFieldIds);
        Task<Dictionary<string, string?>> GetFieldMappingLabelsAsync(int mappingId);
        Task<List<FieldMapping>> GetMappingsByTemplateIdAsync(int templateId);
    }

}
