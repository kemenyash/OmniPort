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

        public async Task<int> SaveTemplateAsync(ImportTemplate template, string sourceType, List<FieldMapping> fields)
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

        public async Task<List<TemplateSummary>> GetTemplatesSummaryAsync()
        {
            return await dataContext.Templates
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TemplateSummary
                {
                    Name = t.Name,
                    SourceType = t.SourceType,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
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
