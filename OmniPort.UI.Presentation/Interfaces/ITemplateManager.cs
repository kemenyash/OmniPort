using OmniPort.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Interfaces
{
    public interface ITemplateManager
    {
        Task<List<TemplateSummary>> GetTemplatesSummaryAsync();
        Task<List<ImportTemplate>> GetTemplatesAsync();
        Task<ImportTemplate?> GetTemplateByIdAsync(int id);
        Task<ImportTemplate?> GetTemplateAsync(int id);
        Task<List<FieldMapping>> GetMappingsByTemplateIdAsync(int id);

        Task<bool> DeleteTemplateByIdAsync(int id);
        Task CreateTemplateAsync(ImportTemplate template, SourceType sourceType, List<FieldMapping> fields);
        Task UpdateTemplateByIdAsync(int id, ImportTemplate template, List<FieldMapping> fields);
    }
}
