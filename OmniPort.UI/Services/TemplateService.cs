using AutoMapper;
using AutoMapper.QueryableExtensions;
using DocumentFormat.OpenXml.InkML;
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

        public async Task<List<ImportTemplate>> GetTemplatesAsync()
        {
            var entities = await dataContext.Templates
                .Include(t => t.Fields)
                .ToListAsync();

            return mapper.Map<List<ImportTemplate>>(entities);
        }

        public async Task<List<JoinedTemplateSummary>> GetJoinedTemplatesAsync()
        {
            return await dataContext.TemplateMappings
                .Include(m => m.SourceTemplate)
                .Include(m => m.TargetTemplate)
                .ProjectTo<JoinedTemplateSummary>(mapper.ConfigurationProvider)
                .ToListAsync();
        }


        public async Task<List<ImportProfile>> GetImportProfilesByTemplateIdAsync(int templateId)
        {
            var mappings = await dataContext.TemplateMappings
                .Include(m => m.SourceTemplate)
                .Include(m => m.TargetTemplate)
                .Where(m => m.SourceTemplateId == templateId || m.TargetTemplateId == templateId)
                .ToListAsync();

            var profiles = new List<ImportProfile>();

            foreach (var mapping in mappings)
            {
                var profile = mapper.Map<ImportProfile>(mapping);

                var fields = await dataContext.TemplateMappingFields
                    .Include(f => f.SourceField)
                    .Include(f => f.TargetField)
                    .Where(f => f.MappingId == mapping.Id)
                    .ToListAsync();

                profile.Mappings = mapper.Map<List<FieldMapping>>(fields);
                profiles.Add(profile);
            }

            return profiles;
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

        public async Task<ImportTemplate> GetTemplateByIdAsync(int templateId)
        {
            var entity = await dataContext.Templates
                .Include(t => t.Fields)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (entity is null) throw new InvalidOperationException($"Template with ID {templateId} not found");

            return mapper.Map<ImportTemplate>(entity);
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

        public async Task SaveMappingAsync(ImportProfile profile, int sourceTemplateId)
        {
            var target = await dataContext.Templates
                .Include(t => t.Fields)
                .FirstOrDefaultAsync(t => t.Id == profile.Template.Id);

            if (target == null)
                throw new InvalidOperationException("Target template not found.");

            var source = await dataContext.Templates
                .Include(t => t.Fields)
                .FirstOrDefaultAsync(t => t.Id == sourceTemplateId);

            if (source == null)
                throw new InvalidOperationException("Source template not found.");

            var mapping = new TemplateMappingData
            {
                SourceTemplateId = sourceTemplateId,
                TargetTemplateId = profile.Template.Id,
            };

            await dataContext.TemplateMappings.AddAsync(mapping);
            await dataContext.SaveChangesAsync();

            var fields = new List<TemplateMappingFieldData>();

            foreach (var field in profile.Mappings)
            {
                var targetField = target.Fields.FirstOrDefault(f => f.Name == field.TargetField);
                if (targetField == null) continue;

                var sourceField = source.Fields.FirstOrDefault(f => f.Name == field.SourceField);

                fields.Add(new TemplateMappingFieldData
                {
                    MappingId = mapping.Id,
                    TargetFieldId = targetField.Id,
                    SourceFieldId = sourceField?.Id
                });
            }

            dataContext.TemplateMappingFields.AddRange(fields);
            await dataContext.SaveChangesAsync();
        }

        public async Task DeleteJoinTemplateAsync(int joinTemplateId)
        {
            var mapping = await dataContext.TemplateMappings
                .FirstOrDefaultAsync(m => m.Id == joinTemplateId);

            if (mapping is null)
                return;

            dataContext.TemplateMappings.Remove(mapping);
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


        public async Task<List<ConversionHistory>> GetFileConversionHistoryAsync()
        {
            var entities = await dataContext.FileConversions
                .Include(f => f.TemplateMap)
                    .ThenInclude(m => m.SourceField)
                .Include(f => f.TemplateMap)
                    .ThenInclude(m => m.TargetField)
                .OrderByDescending(f => f.ConvertedAt)
                .ToListAsync();

            return mapper.Map<List<ConversionHistory>>(entities);
        }


        public async Task<List<UrlConversionHistory>> GetUrlConversionHistoryAsync()
        {
            var entities = await dataContext.UrlConversions
                .Include(u => u.TemplateMap)
                    .ThenInclude(m => m.SourceField)
                .Include(u => u.TemplateMap)
                    .ThenInclude(m => m.TargetField)
                .OrderByDescending(u => u.ConvertedAt)
                .ToListAsync();

            return mapper.Map<List<UrlConversionHistory>>(entities);
        }


        public async Task<List<WatchedUrl>> GetWatchedUrlsAsync()
        {
            var entities = await dataContext.WatchedUrls
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            return mapper.Map<List<WatchedUrl>>(entities);
        }

        public async Task AddFileConversionAsync(ConversionHistory model)
        {
            var entity = mapper.Map<FileConversionData>(model);
            entity.ConvertedAt = DateTime.UtcNow;

            await dataContext.FileConversions.AddAsync(entity);
            await dataContext.SaveChangesAsync();
        }

        public async Task AddUrlConversionAsync(UrlConversionHistory model)
        {
            var entity = mapper.Map<UrlConversionData>(model);
            entity.ConvertedAt = DateTime.UtcNow;
            entity.TemplateMapId = model.TemplateMapId;

            await dataContext.UrlConversions.AddAsync(entity);
            try { await dataContext.SaveChangesAsync(); }
            catch(Exception error)
            {

            }
        }

        public async Task AddWatchedUrlAsync(WatchedUrl model)
        {
            var entity = mapper.Map<WatchedUrlData>(model);
            entity.CreatedAt = DateTime.UtcNow;

            await dataContext.WatchedUrls.AddAsync(entity);
            await dataContext.SaveChangesAsync();
        }

        public async Task DeleteWatchedUrlAsync(string url)
        {
            var entity = await dataContext.WatchedUrls
                .FirstOrDefaultAsync(w => w.Url == url);

            if (entity is not null)
            {
                dataContext.WatchedUrls.Remove(entity);
                await dataContext.SaveChangesAsync();
            }
        }


    }
}
