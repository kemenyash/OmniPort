using AutoMapper;
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

            return template != null ? mapper.Map<ImportTemplate>(template) : null;
        }

        public async Task<List<FieldMapping>> GetMappingsByTemplateIdAsync(int templateId)
        {
            var template = await dataContext.Templates
                .Include(t => t.Fields)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (template == null)
                return new List<FieldMapping>();

            return template.Fields.Select(f => new FieldMapping
            {
                SourceField = f.Name,
                TargetField = f.Name,
                TargetType = f.Type
            }).ToList();
        }

        public async Task<int> CreateTemplateAsync(ImportTemplate template, SourceType sourceType, List<FieldMapping> fields)
        {
            var entity = mapper.Map<TemplateData>(template);
            entity.CreatedAt = DateTime.UtcNow;
            entity.SourceType = sourceType;
            entity.Fields = fields.Select(f => new TemplateFieldData
            {
                Name = f.TargetField,
                Type = f.TargetType
            }).ToList();

            dataContext.Templates.Add(entity);
            await dataContext.SaveChangesAsync();

            return entity.Id;
        }

        public async Task<bool> UpdateTemplateByIdAsync(int templateId, ImportTemplate updatedTemplate, List<FieldMapping> fields)
        {
            var existing = await dataContext.Templates
                .Include(t => t.Fields)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (existing == null)
                return false;

            existing.Name = updatedTemplate.TemplateName;
            existing.SourceType = updatedTemplate.SourceType;

            dataContext.TemplateFields.RemoveRange(existing.Fields);
            existing.Fields = fields.Select(f => new TemplateFieldData
            {
                Name = f.TargetField,
                Type = f.TargetType
            }).ToList();

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

            if (template == null)
                return false;

            if (template.Fields?.Any() == true)
                dataContext.TemplateFields.RemoveRange(template.Fields);

            var sourceMappingIds = template.SourceMappings.Select(m => m.Id).ToList();
            var targetMappingIds = template.TargetMappings.Select(m => m.Id).ToList();

            var allMappingIds = sourceMappingIds.Concat(targetMappingIds).Distinct().ToList();
            if (allMappingIds.Any())
            {
                var mappingFields = await dataContext.TemplateMappingFields
                    .Where(f => allMappingIds.Contains(f.MappingId))
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
                .Select(t => new TemplateSummary
                {
                    Id = t.Id,
                    Name = t.Name,
                    SourceType = t.SourceType,
                    CreatedAt = t.CreatedAt
                })
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

            var mappingFields = targetToSourceFieldIds.Select(kv => new TemplateMappingFieldData
            {
                MappingId = mapping.Id,
                TargetFieldId = kv.Key,
                SourceFieldId = kv.Value
            });

            dataContext.TemplateMappingFields.AddRange(mappingFields);
            await dataContext.SaveChangesAsync();
        }

        public async Task<Dictionary<string, string?>> GetFieldMappingLabelsAsync(int mappingId)
        {
            var mapping = await dataContext.TemplateMappingFields
                .Include(f => f.TargetField)
                .Include(f => f.SourceField)
                .Where(f => f.MappingId == mappingId)
                .ToListAsync();

            return mapping.ToDictionary(
                f => f.TargetField.Name,
                f => f.SourceField?.Name
            );
        }
    }
}
