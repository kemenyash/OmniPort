using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Models;
using OmniPort.Core.Records;
using OmniPort.Data;
using OmniPort.UI.Presentation.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Services
{
    public class TemplateManager : ITemplateManager
    {
        private readonly OmniPortDataContext dataContext;
        private readonly IMapper _mapper;

        public TemplateManager(OmniPortDataContext db, IMapper mapper)
        {
            dataContext = db;
            _mapper = mapper;
        }

        // --- Basic templates ---
        public async Task<IReadOnlyList<TemplateSummaryDto>> GetBasicTemplatesSummaryAsync()
        {
            return await dataContext.BasicTemplates
                .AsNoTracking()
                .ProjectTo<TemplateSummaryDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<BasicTemplateDto?> GetBasicTemplateAsync(int templateId)
        {
            var q = dataContext.BasicTemplates
                .AsNoTracking()
                .Include(t => t.Fields)
                .Where(t => t.Id == templateId);

            var entity = await q.FirstOrDefaultAsync();
            return entity is null ? null : _mapper.Map<BasicTemplateDto>(entity);
        }

        public async Task<int> CreateBasicTemplateAsync(CreateBasicTemplateDto dto)
        {
            var entity = _mapper.Map<BasicTemplateData>(dto);
            dataContext.BasicTemplates.Add(entity);
            await dataContext.SaveChangesAsync();
            foreach (var f in entity.Fields) f.TemplateSourceId = entity.Id;
            await dataContext.SaveChangesAsync();

            return entity.Id;
        }

        public async Task<bool> UpdateBasicTemplateAsync(UpdateBasicTemplateDto dto)
        {
            var entity = await dataContext.BasicTemplates
                .Include(t => t.Fields)
                .FirstOrDefaultAsync(t => t.Id == dto.Id);
            if (entity is null) return false;

            entity.Name = dto.Name;
            entity.SourceType = dto.SourceType;
            UpsertFields(entity, dto.Fields);

            foreach (var f in entity.Fields.Where(x => x.TemplateSourceId == 0))
                f.TemplateSourceId = entity.Id;

            await dataContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteBasicTemplateAsync(int templateId)
        {
            var e = await dataContext.BasicTemplates.FindAsync(templateId);
            if (e is null) return false;

            dataContext.BasicTemplates.Remove(e);
            await dataContext.SaveChangesAsync();
            return true;
        }

        // --- Mapping templates ---
        public async Task<IReadOnlyList<JoinedTemplateSummaryDto>> GetJoinedTemplatesAsync()
        {
            return await dataContext.MappingTemplates
                .AsNoTracking()
                .Include(m => m.SourceTemplate)
                .Include(m => m.TargetTemplate)
                .ProjectTo<JoinedTemplateSummaryDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<MappingTemplateDto?> GetMappingTemplateAsync(int mappingTemplateId)
        {
            var e = await dataContext.MappingTemplates
                .AsNoTracking()
                .Include(m => m.SourceTemplate)
                .Include(m => m.TargetTemplate)
                .Include(m => m.MappingFields)
                    .ThenInclude(f => f.SourceField)
                .Include(m => m.MappingFields)
                    .ThenInclude(f => f.TargetField)
                .FirstOrDefaultAsync(m => m.Id == mappingTemplateId);

            return e is null ? null : _mapper.Map<MappingTemplateDto>(e);
        }

        public async Task<int> CreateMappingTemplateAsync(CreateMappingTemplateDto dto)
        {
            var e = _mapper.Map<MappingTemplateData>(dto);
            dataContext.MappingTemplates.Add(e);
            await dataContext.SaveChangesAsync();

            var fields = BuildMappingFields(e.Id, dto.TargetToSource);
            if (fields.Count > 0)
            {
                dataContext.MappingFields.AddRange(fields);
                await dataContext.SaveChangesAsync();
            }

            return e.Id;
        }

        public async Task<bool> UpdateMappingTemplateAsync(UpdateMappingTemplateDto dto)
        {
            var e = await dataContext.MappingTemplates
                .Include(m => m.MappingFields)
                .FirstOrDefaultAsync(m => m.Id == dto.Id);

            if (e is null) return false;

            e.Name = dto.Name;
            e.SourceTemplateId = dto.SourceTemplateId;
            e.TargetTemplateId = dto.TargetTemplateId;

            dataContext.MappingFields.RemoveRange(e.MappingFields);
            var fields = BuildMappingFields(e.Id, dto.TargetToSource);
            if (fields.Count > 0) dataContext.MappingFields.AddRange(fields);

            await dataContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteMappingTemplateAsync(int mappingTemplateId)
        {
            var e = await dataContext.MappingTemplates.FindAsync(mappingTemplateId);
            if (e is null) return false;

            dataContext.MappingTemplates.Remove(e);
            await dataContext.SaveChangesAsync();
            return true;
        }

        // --- History / Watch ---
        public async Task<IReadOnlyList<FileConversionHistoryDto>> GetFileConversionHistoryAsync()
        {
            return await dataContext.FileConversionHistory
                .AsNoTracking()
                .Include(h => h.MappingTemplate)
                .ProjectTo<FileConversionHistoryDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<UrlConversionHistoryDto>> GetUrlConversionHistoryAsync()
        {
            return await dataContext.UrlConversionHistory
                .AsNoTracking()
                .Include(h => h.MappingTemplate)
                .ProjectTo<UrlConversionHistoryDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task AddFileConversionAsync(FileConversionHistoryDto dto)
        {
            var e = new FileConversionHistoryData
            {
                ConvertedAt = dto.ConvertedAt,
                FileName = dto.FileName,
                OutputUrl = dto.OutputLink,
                MappingTemplateId = dto.MappingTemplateId
            };
            dataContext.FileConversionHistory.Add(e);
            await dataContext.SaveChangesAsync();
        }

        public async Task AddUrlConversionAsync(UrlConversionHistoryDto dto)
        {
            var e = new UrlConversionHistoryData
            {
                ConvertedAt = dto.ConvertedAt,
                InputUrl = dto.InputUrl,
                OutputUrl = dto.OutputLink,
                MappingTemplateId = dto.MappingTemplateId
            };
            dataContext.UrlConversionHistory.Add(e);
            await dataContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<WatchedUrlDto>> GetWatchedUrlsAsync()
        {
            return await dataContext.UrlFileGetting
                .AsNoTracking()
                .ProjectTo<WatchedUrlDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<int> AddWatchedUrlAsync(string url, int intervalMinutes, int mappingTemplateId)
        {
            var existing = await dataContext.UrlFileGetting
                .FirstOrDefaultAsync(x => x.Url == url && x.MappingTemplateId == mappingTemplateId);

            if (existing is not null)
            {
                existing.CheckIntervalMinutes = intervalMinutes;
                await dataContext.SaveChangesAsync();
                return existing.Id;
            }

            var e = new UrlFileGettingData
            {
                Url = url,
                CheckIntervalMinutes = intervalMinutes,
                MappingTemplateId = mappingTemplateId
            };

            dataContext.UrlFileGetting.Add(e);
            await dataContext.SaveChangesAsync();
            return e.Id;
        }


        public async Task<bool> DeleteWatchedUrlAsync(int watchedUrlId)
        {
            var e = await dataContext.UrlFileGetting.FindAsync(watchedUrlId);
            if (e is null) return false;

            dataContext.UrlFileGetting.Remove(e);
            await dataContext.SaveChangesAsync();
            return true;
        }



        // ==========================
        //        Helpers
        // ==========================

        private static void UpsertFields(BasicTemplateData entity, IEnumerable<UpsertTemplateFieldDto> fieldsDto)
        {
            var byId = entity.Fields.ToDictionary(f => f.Id);
            var keepIds = new HashSet<int>();

            foreach (var f in fieldsDto)
            {
                if (f.Id.HasValue && byId.TryGetValue(f.Id.Value, out var existing))
                {
                    existing.Name = f.Name;
                    existing.Type = f.Type;
                    keepIds.Add(existing.Id);
                }
                else
                {
                    entity.Fields.Add(new FieldData
                    {
                        Name = f.Name,
                        Type = f.Type
                    });
                }
            }

            var toRemove = entity.Fields.Where(x => x.Id != 0 && !keepIds.Contains(x.Id)).ToList();
            foreach (var r in toRemove) entity.Fields.Remove(r);
        }

        private static List<MappingFieldData> BuildMappingFields(int mappingTemplateId, IReadOnlyDictionary<int, int?> targetToSource)
        {
            var res = new List<MappingFieldData>(targetToSource.Count);
            foreach (var kv in targetToSource)
            {
                var targetId = kv.Key;
                var sourceId = kv.Value;
                if (sourceId is null) continue; // Not mapped

                res.Add(new MappingFieldData
                {
                    MappingTemplateId = mappingTemplateId,
                    TargetFieldId = targetId,
                    SourceFieldId = sourceId.Value
                });
            }
            return res;
        }
    }
}
