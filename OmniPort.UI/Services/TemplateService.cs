using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Models;
using OmniPort.Data;

namespace OmniPort.UI.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly OmniPortDataContext dataContext;
        private readonly IMapper mapper;

        public TemplateService(OmniPortDataContext db, IMapper mapper)
        {
            dataContext = db;
            this.mapper = mapper;
        }

        public async Task<List<string>> GetTemplateNamesAsync()
        {
            return await dataContext.Templates
                .Select(t => t.Name)
                .ToListAsync();
        }

        public async Task<ImportTemplate?> GetTemplateAsync(int templateId)
        {
            var template = await dataContext.Templates
                .Include(t => t.Fields)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            return template is null ? null : mapper.Map<ImportTemplate>(template);
        }

        public async Task<List<FieldMapping>> GetMappingsByTemplateIdAsync(int templateId)
        {
            var fields = await dataContext.TemplateFields
                .Where(f => f.TemplateId == templateId)
                .ToListAsync();

            return mapper.Map<List<FieldMapping>>(fields);
        }

        public async Task<int> CreateTemplateAsync(ImportTemplate template, SourceType sourceType, List<FieldMapping> fields)
        {
            var entity = mapper.Map<TemplateData>(template);
            entity.CreatedAt = DateTime.UtcNow;
            entity.SourceType = sourceType;
            entity.Fields = mapper.Map<List<TemplateFieldData>>(fields);

            await dataContext.Templates.AddAsync(entity);
            await dataContext.SaveChangesAsync();

            return entity.Id;
        }

        public async Task<bool> UpdateTemplateByIdAsync(int templateId, ImportTemplate updatedTemplate, List<FieldMapping> fields)
        {
            var existing = await dataContext.Templates
                .Include(t => t.Fields)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (existing is null)
                return false;

            existing.Name = updatedTemplate.TemplateName;
            existing.SourceType = updatedTemplate.SourceType;

            dataContext.TemplateFields.RemoveRange(existing.Fields);
            existing.Fields = mapper.Map<List<TemplateFieldData>>(fields);

            await dataContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteTemplateByIdAsync(int templateId)
        {
            var template = await dataContext.Templates
                .Include(t => t.Fields)
                .Include(t => t.SourceMappings)
                .Include(t => t.TargetMappings)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (template is null)
                return false;

            if (template.Fields?.Any() == true)
                dataContext.TemplateFields.RemoveRange(template.Fields);

            var mappingIds = template.SourceMappings.Select(m => m.Id)
                .Concat(template.TargetMappings.Select(m => m.Id))
                .Distinct()
                .ToList();

            if (mappingIds.Any())
            {
                var mappingFields = await dataContext.TemplateMappingFields
                    .Where(f => mappingIds.Contains(f.MappingId))
                    .ToListAsync();

                dataContext.TemplateMappingFields.RemoveRange(mappingFields);
            }

            if (template.SourceMappings?.Any() == true)
                dataContext.TemplateMappings.RemoveRange(template.SourceMappings);

            if (template.TargetMappings?.Any() == true)
                dataContext.TemplateMappings.RemoveRange(template.TargetMappings);

            dataContext.Templates.Remove(template);
            await dataContext.SaveChangesAsync();

            return true;
        }

        public async Task<List<TemplateSummary>> GetTemplatesSummaryAsync()
        {
            return await dataContext.Templates
                .OrderByDescending(t => t.CreatedAt)
                .ProjectTo<TemplateSummary>(mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task SaveTemplateMappingAsync(int sourceTemplateId, int targetTemplateId, Dictionary<int, int?> targetToSourceFieldIds)
        {
            var mapping = new TemplateMappingData
            {
                SourceTemplateId = sourceTemplateId,
                TargetTemplateId = targetTemplateId
            };

            dataContext.TemplateMappings.Add(mapping);
            await dataContext.SaveChangesAsync();

            var mappingFields = mapper.Map<List<TemplateMappingFieldData>>(targetToSourceFieldIds);
            foreach (var field in mappingFields)
                field.MappingId = mapping.Id;

            dataContext.TemplateMappingFields.AddRange(mappingFields);
            await dataContext.SaveChangesAsync();
        }

        public async Task<Dictionary<string, string?>> GetFieldMappingLabelsAsync(int mappingId)
        {
            var mappingFields = await dataContext.TemplateMappingFields
                .Include(f => f.TargetField)
                .Include(f => f.SourceField)
                .Where(f => f.MappingId == mappingId)
                .ToListAsync();

            return mappingFields.ToDictionary(
                f => f.TargetField.Name,
                f => f.SourceField?.Name
            );
        }
    }
}
