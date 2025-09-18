using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Enums;
using OmniPort.Core.Records;
using OmniPort.Data;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<IReadOnlyList<TemplateSummaryDto>> GetBasicTemplatesSummaryAsync()
        {
            return await dataContext.BasicTemplates
                .AsNoTracking()
                .ProjectTo<TemplateSummaryDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<BasicTemplateDto?> GetBasicTemplateAsync(int templateId)
        {
            var entity = await dataContext.BasicTemplates
                .AsNoTracking()
                .Include(t => t.Fields)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            return entity is null ? null : _mapper.Map<BasicTemplateDto>(entity);
        }

        public async Task<int> CreateBasicTemplateAsync(CreateBasicTemplateDto dto)
        {
            var entity = new BasicTemplateData
            {
                Name = dto.Name,
                SourceType = dto.SourceType
            };
            dataContext.BasicTemplates.Add(entity);
            await dataContext.SaveChangesAsync(); 

            foreach (var f in dto.Fields)
                AddFieldRecursive(entity.Id, parentId: null, isArrayItem: false, f);

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

            var roots = entity.Fields.Where(x => x.ParentFieldId == null && !x.IsArrayItem).ToList();
            UpsertChildren(entity.Id, parentId: null, isArrayItem: false, dto.Fields, roots);

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
                .Include(m => m.MappingFields).ThenInclude(f => f.SourceField)
                .Include(m => m.MappingFields).ThenInclude(f => f.TargetField)
                .FirstOrDefaultAsync(m => m.Id == mappingTemplateId);

            return e is null ? null : _mapper.Map<MappingTemplateDto>(e);
        }

        public async Task<int> CreateMappingTemplateAsync(CreateMappingTemplateDto dto)
        {
            var e = new MappingTemplateData
            {
                Name = dto.Name,
                SourceTemplateId = dto.SourceTemplateId,
                TargetTemplateId = dto.TargetTemplateId
            };
            dataContext.MappingTemplates.Add(e);
            await dataContext.SaveChangesAsync();

            await RebuildMappingFieldsFromPathsAsync(e.Id, dto.SourceTemplateId, dto.TargetTemplateId, dto.Mappings);

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
            await dataContext.SaveChangesAsync();

            await RebuildMappingFieldsFromPathsAsync(e.Id, dto.SourceTemplateId, dto.TargetTemplateId, dto.Mappings);

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


        private void AddFieldRecursive(int templateId, int? parentId, bool isArrayItem, UpsertTemplateFieldDto f)
        {
            var entity = new FieldData
            {
                TemplateSourceId = templateId,
                ParentFieldId = parentId,
                IsArrayItem = isArrayItem,
                Name = f.Name,
                Type = f.Type,
                ItemType = f.ItemType
            };
            dataContext.Fields.Add(entity);
            dataContext.SaveChanges(); 

            if (f.Type == FieldDataType.Object)
            {
                foreach (var c in f.Children ?? Enumerable.Empty<UpsertTemplateFieldDto>())
                    AddFieldRecursive(templateId, entity.Id, false, c);
            }
            else if (f.Type == FieldDataType.Array && f.ItemType == FieldDataType.Object)
            {
                foreach (var c in f.ChildrenItems ?? Enumerable.Empty<UpsertTemplateFieldDto>())
                    AddFieldRecursive(templateId, entity.Id, true, c);
            }
        }

        private void AddFieldRecursive(int templateId, int? parentId, bool isArrayItem, CreateTemplateFieldDto f)
        {
            var up = ToUpsert(f);
            AddFieldRecursive(templateId, parentId, isArrayItem, up);
        }

        private static UpsertTemplateFieldDto ToUpsert(CreateTemplateFieldDto f) =>
            new(
                Id: null,
                Name: f.Name,
                Type: f.Type,
                ItemType: f.ItemType,
                Children: (f.Children ?? new List<CreateTemplateFieldDto>()).Select(ToUpsert).ToList(),
                ChildrenItems: (f.ChildrenItems ?? new List<CreateTemplateFieldDto>()).Select(ToUpsert).ToList()
            );

        private void UpsertChildren(int templateId, int? parentId, bool isArrayItem,
            IEnumerable<UpsertTemplateFieldDto> incoming, List<FieldData> existingSiblings)
        {
            var byName = existingSiblings.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<int>();

            foreach (var f in incoming)
            {
                if (byName.TryGetValue(f.Name, out var ex))
                {
                    ex.Type = f.Type;
                    ex.ItemType = f.Type == FieldDataType.Array ? f.ItemType : null;
                    seen.Add(ex.Id);

                    if (f.Type == FieldDataType.Object)
                    {
                        var children = ex.Children.Where(c => !c.IsArrayItem).ToList();
                        UpsertChildren(templateId, ex.Id, false, f.Children ?? Enumerable.Empty<UpsertTemplateFieldDto>(), children);
                    }
                    else if (f.Type == FieldDataType.Array && f.ItemType == FieldDataType.Object)
                    {
                        var itemChildren = ex.Children.Where(c => c.IsArrayItem).ToList();
                        UpsertChildren(templateId, ex.Id, true, f.ChildrenItems ?? Enumerable.Empty<UpsertTemplateFieldDto>(), itemChildren);
                    }
                    else
                    {
                        var toRemove = ex.Children.ToList();
                        if (toRemove.Count > 0) dataContext.Fields.RemoveRange(toRemove);
                    }
                }
                else
                {
                    var node = new FieldData
                    {
                        TemplateSourceId = templateId,
                        ParentFieldId = parentId,
                        IsArrayItem = isArrayItem,
                        Name = f.Name,
                        Type = f.Type,
                        ItemType = f.Type == FieldDataType.Array ? f.ItemType : null
                    };
                    dataContext.Fields.Add(node);
                    dataContext.SaveChanges(); 

                    if (f.Type == FieldDataType.Object)
                    {
                        UpsertChildren(templateId, node.Id, false, f.Children ?? Enumerable.Empty<UpsertTemplateFieldDto>(), new List<FieldData>());
                    }
                    else if (f.Type == FieldDataType.Array && f.ItemType == FieldDataType.Object)
                    {
                        UpsertChildren(templateId, node.Id, true, f.ChildrenItems ?? Enumerable.Empty<UpsertTemplateFieldDto>(), new List<FieldData>());
                    }

                    seen.Add(node.Id);
                }
            }

            var toDelete = existingSiblings.Where(x => !seen.Contains(x.Id)).ToList();
            if (toDelete.Count > 0)
                dataContext.Fields.RemoveRange(toDelete);
        }

        private async Task RebuildMappingFieldsFromPathsAsync(int mappingTemplateId, int sourceTemplateId, int targetTemplateId, IEnumerable<MappingEntryDto> mappings)
        {
            var sourceFields = await dataContext.Fields
                .Where(f => f.TemplateSourceId == sourceTemplateId)
                .AsNoTracking().ToListAsync();

            var targetFields = await dataContext.Fields
                .Where(f => f.TemplateSourceId == targetTemplateId)
                .AsNoTracking().ToListAsync();

            var sourceMap = BuildPathToIdMap(sourceFields);
            var targetMap = BuildPathToIdMap(targetFields);

            var toAdd = new List<MappingFieldData>();
            foreach (var m in mappings ?? Enumerable.Empty<MappingEntryDto>())
            {
                if (string.IsNullOrWhiteSpace(m.TargetPath)) continue;
                if (!targetMap.TryGetValue(m.TargetPath, out var targetId)) continue;
                if (string.IsNullOrWhiteSpace(m.SourcePath)) continue;
                if (!sourceMap.TryGetValue(m.SourcePath!, out var sourceId)) continue;

                toAdd.Add(new MappingFieldData
                {
                    MappingTemplateId = mappingTemplateId,
                    SourceFieldId = sourceId,
                    TargetFieldId = targetId
                });
            }

            if (toAdd.Count > 0)
            {
                dataContext.MappingFields.AddRange(toAdd);
                await dataContext.SaveChangesAsync();
            }
        }

        private static Dictionary<string, int> BuildPathToIdMap(List<FieldData> all)
        {
            var lookupChildren = all.GroupBy(x => x.ParentFieldId ?? -1)
                                    .ToDictionary(g => g.Key, g => g.ToList());

            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            void Walk(FieldData node, string prefix)
            {
                string name = string.IsNullOrEmpty(prefix) ? node.Name : $"{prefix}.{node.Name}";

                if (node.Type == FieldDataType.Object)
                {
                    result[name] = node.Id;

                    if (lookupChildren.TryGetValue(node.Id, out var kids))
                    {
                        foreach (var c in kids.Where(k => !k.IsArrayItem))
                            Walk(c, name);
                    }
                }
                else if (node.Type == FieldDataType.Array)
                {
                    var arrBase = $"{name}[]";
                    result[arrBase] = node.Id; 

                    if (node.ItemType == FieldDataType.Object)
                    {
                        if (lookupChildren.TryGetValue(node.Id, out var kids))
                        {
                            foreach (var c in kids.Where(k => k.IsArrayItem))
                                Walk(c, arrBase);
                        }
                    }
                }
                else
                {
                    result[name] = node.Id;
                }
            }

            if (lookupChildren.TryGetValue(-1, out var roots))
            {
                foreach (var r in roots.Where(x => !x.IsArrayItem))
                    Walk(r, "");
            }

            return result;
        }
    }
}
