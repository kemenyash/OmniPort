using OmniPort.Core.Interfaces;
using OmniPort.Core.Models;
using OmniPort.UI.Presentation.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Services
{
    public class TemplateManager : ITemplateManager
    {
        private readonly ICRUDService crudService;

        public TemplateManager(ICRUDService templateService)
        {
            crudService = templateService;
        }

        public async Task<List<TemplateSummary>> GetTemplatesSummaryAsync()
            => await crudService.GetTemplatesSummaryAsync();

        public async Task<List<ImportTemplate>> GetTemplatesAsync()
            => await crudService.GetTemplatesAsync();

        public async Task<ImportTemplate?> GetTemplateByIdAsync(int id)
            => await crudService.GetTemplateByIdAsync(id);

        public async Task<ImportTemplate?> GetTemplateAsync(int id)
            => await crudService.GetTemplateAsync(id);

        public async Task<List<FieldMapping>> GetMappingsByTemplateIdAsync(int id)
            => await crudService.GetMappingsByTemplateIdAsync(id);

        public Task<bool> DeleteTemplateByIdAsync(int id)
            => crudService.DeleteTemplateByIdAsync(id);

        public async Task CreateTemplateAsync(ImportTemplate template, SourceType sourceType, List<FieldMapping> fields)
            => await crudService.CreateTemplateAsync(template, sourceType, fields);

        public Task UpdateTemplateByIdAsync(int id, ImportTemplate template, List<FieldMapping> fields)
            => crudService.UpdateTemplateByIdAsync(id, template, fields);

        public async Task UpdateTemplateByIdAsync(int id, ImportTemplate template, IEnumerable<FieldMapping> fields)
        {
            await crudService.UpdateTemplateByIdAsync(id, template, fields.ToList());
        }
    }
}
