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
        Task<ImportTemplate?> GetTemplateAsync(int templateId);
        Task<List<TemplateSummary>> GetTemplatesSummaryAsync();
        Task<int> SaveTemplateAsync(ImportTemplate template, string sourceType, List<FieldMapping> fields);
        Task SaveTemplateMappingAsync(int sourceTemplateId, int targetTemplateId, Dictionary<int, int?> targetToSourceFieldIds);
        Task<Dictionary<string, string?>> GetFieldMappingLabelsAsync(int mappingId);
    }

}
