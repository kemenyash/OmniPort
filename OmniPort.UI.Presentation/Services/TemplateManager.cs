using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Records;
using OmniPort.Data;
using OmniPort.UI.Presentation.Helpers;

namespace OmniPort.UI.Presentation.Services
{
    public class TemplateManager : ITemplateManager
    {
        private readonly OmniPortDataContext dataContext;
        private readonly IMapper mapper;
        private readonly TemplateManagerHelpers templateManagerHelpers;

        public TemplateManager(OmniPortDataContext dataContext, IMapper mapper)
        {
            this.dataContext = dataContext;
            this.mapper = mapper;
            this.templateManagerHelpers = new TemplateManagerHelpers(dataContext);
        }

        public async Task<IReadOnlyList<TemplateSummaryDto>> GetBasicTemplatesSummary()
        {
            List<TemplateSummaryDto> basicTemplates = await dataContext.BasicTemplates
                .AsNoTracking()
                .ProjectTo<TemplateSummaryDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return basicTemplates;
        }
        public async Task<BasicTemplateDto?> GetBasicTemplate(int templateId)
        {
            BasicTemplateData? basicTemplateEntity = await dataContext.BasicTemplates
                                                    .AsNoTracking()
                                                    .FirstOrDefaultAsync(templateEntity => templateEntity.Id == templateId);

            if (basicTemplateEntity is null)
            {
                return null;
            }

            List<FieldData> allFieldEntities = await dataContext.Fields
                                .AsNoTracking()
                                .Where(fieldEntity => fieldEntity.TemplateSourceId == templateId)
                                .ToListAsync();

            List<FieldData> rootFieldEntities = allFieldEntities
                                    .Where(fieldEntity => fieldEntity.ParentFieldId == null && !fieldEntity.IsArrayItem)
                                    .ToList();

            Dictionary<int, List<FieldData>> childrenByParent = allFieldEntities.Where(fieldEntity => fieldEntity.ParentFieldId.HasValue)
                    .GroupBy(fieldEntity => fieldEntity.ParentFieldId!.Value)
                    .ToDictionary(group => group.Key, group => group.ToList());

            List<TemplateFieldDto> templateFieldDtos =
                rootFieldEntities
                    .Select(fieldEntity => ConvertFieldToDto(fieldEntity, childrenByParent))
                    .ToList();

            return new BasicTemplateDto(
                Id: basicTemplateEntity.Id,
                Name: basicTemplateEntity.Name,
                SourceType: basicTemplateEntity.SourceType,
                Fields: templateFieldDtos
            );
        }
        public async Task<int> CreateBasicTemplate(CreateBasicTemplateDto dto)
        {
            BasicTemplateData newBasicTemplateEntity = new BasicTemplateData
            {
                Name = dto.Name,
                SourceType = dto.SourceType
            };

            dataContext.BasicTemplates.Add(newBasicTemplateEntity);
            await dataContext.SaveChangesAsync();

            foreach (CreateTemplateFieldDto field in dto.Fields)
            {
                templateManagerHelpers.AddFieldRecursive(
                    newBasicTemplateEntity.Id,
                    parentId: null,
                    isArrayItem: false,
                    fieldDto: field);
            }

            await dataContext.SaveChangesAsync();
            return newBasicTemplateEntity.Id;
        }
        public async Task<bool> UpdateBasicTemplate(UpdateBasicTemplateDto dto)
        {
            BasicTemplateData? existingTemplateEntity = await dataContext.BasicTemplates
                                        .Include(template => template.Fields)
                                        .FirstOrDefaultAsync(template => template.Id == dto.Id);

            if (existingTemplateEntity is null)
            {
                return false;
            }

            existingTemplateEntity.Name = dto.Name;
            existingTemplateEntity.SourceType = dto.SourceType;

            List<FieldData> existingRootFields = existingTemplateEntity.Fields
                                    .Where(field => field.ParentFieldId == null && !field.IsArrayItem)
                                    .ToList();

            templateManagerHelpers.UpsertChildren(
                templateId: existingTemplateEntity.Id,
                parentId: null,
                isArrayItem: false,
                incomingFields: dto.Fields,
                existingSiblingFields: existingRootFields
            );

            await dataContext.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteBasicTemplate(int templateId)
        {
            BasicTemplateData? templateEntity = await dataContext.BasicTemplates.FindAsync(templateId);

            if (templateEntity is null)
            {
                return false;
            }

            dataContext.BasicTemplates.Remove(templateEntity);
            await dataContext.SaveChangesAsync();
            return true;
        }

        public async Task<IReadOnlyList<JoinedTemplateSummaryDto>> GetJoinedTemplates()
        {
            List<JoinedTemplateSummaryDto> joindTemplates = await dataContext.MappingTemplates
                                .AsNoTracking()
                                .Include(mapping => mapping.SourceTemplate)
                                .Include(mapping => mapping.TargetTemplate)
                                .ProjectTo<JoinedTemplateSummaryDto>(mapper.ConfigurationProvider)
                                .ToListAsync();

            return joindTemplates;
        }

        public async Task<MappingTemplateDto?> GetMappingTemplate(int mappingTemplateId)
        {
            MappingTemplateData? mappingTemplateEntity = await dataContext.MappingTemplates
                    .AsNoTracking()
                    .Include(mapping => mapping.SourceTemplate)
                    .Include(mapping => mapping.TargetTemplate)
                    .Include(mapping => mapping.MappingFields).ThenInclude(field => field.SourceField)
                    .Include(mapping => mapping.MappingFields).ThenInclude(field => field.TargetField)
                    .FirstOrDefaultAsync(mapping => mapping.Id == mappingTemplateId);

            if (mappingTemplateEntity is null)
            {
                return null;
            }

            return mapper.Map<MappingTemplateDto>(mappingTemplateEntity);
        }
        public async Task<int> CreateMappingTemplate(CreateMappingTemplateDto dto)
        {
            MappingTemplateData mappingTemplateEntity = new MappingTemplateData
            {
                Name = dto.Name,
                SourceTemplateId = dto.SourceTemplateId,
                TargetTemplateId = dto.TargetTemplateId
            };

            dataContext.MappingTemplates.Add(mappingTemplateEntity);
            await dataContext.SaveChangesAsync();

            await templateManagerHelpers.RebuildMappingFieldsFromPaths(
                mappingTemplateEntity.Id,
                dto.SourceTemplateId,
                dto.TargetTemplateId,
                dto.Mappings
            );

            return mappingTemplateEntity.Id;
        }
        public async Task<bool> UpdateMappingTemplate(UpdateMappingTemplateDto dto)
        {
            MappingTemplateData? existingMappingTemplate = await dataContext.MappingTemplates
                    .Include(mapping => mapping.MappingFields)
                    .FirstOrDefaultAsync(mapping => mapping.Id == dto.Id);

            if (existingMappingTemplate is null)
            {
                return false;
            }

            existingMappingTemplate.Name = dto.Name;
            existingMappingTemplate.SourceTemplateId = dto.SourceTemplateId;
            existingMappingTemplate.TargetTemplateId = dto.TargetTemplateId;

            dataContext.MappingFields.RemoveRange(existingMappingTemplate.MappingFields);
            await dataContext.SaveChangesAsync();

            await templateManagerHelpers.RebuildMappingFieldsFromPaths(
                existingMappingTemplate.Id,
                dto.SourceTemplateId,
                dto.TargetTemplateId,
                dto.Mappings
            );

            return true;
        }
        public async Task<bool> DeleteMappingTemplate(int mappingTemplateId)
        {
            MappingTemplateData? mappingTemplateEntity = await dataContext.MappingTemplates.FindAsync(mappingTemplateId);

            if (mappingTemplateEntity is null)
            {
                return false;
            }

            dataContext.MappingTemplates.Remove(mappingTemplateEntity);
            await dataContext.SaveChangesAsync();
            return true;
        }

        public async Task<IReadOnlyList<UrlConversionHistoryDto>> GetUrlConversionHistory()
        {
            List<UrlConversionHistoryDto> urlConversionHistory = await dataContext.UrlConversionHistory
                .AsNoTracking()
                .Include(history => history.MappingTemplate)
                .ProjectTo<UrlConversionHistoryDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return urlConversionHistory;
        }
        public async Task AddUrlConversion(UrlConversionHistoryDto dto)
        {
            UrlConversionHistoryData urlConversionEntity = new UrlConversionHistoryData
            {
                ConvertedAt = dto.ConvertedAt,
                InputUrl = dto.InputUrl,
                OutputUrl = dto.OutputLink,
                MappingTemplateId = dto.MappingTemplateId
            };

            dataContext.UrlConversionHistory.Add(urlConversionEntity);
            await dataContext.SaveChangesAsync();
        }
        public async Task AddFileConversion(FileConversionHistoryDto dto)
        {
            FileConversionHistoryData fileConversionEntity = new FileConversionHistoryData
            {
                ConvertedAt = dto.ConvertedAt,
                FileName = dto.FileName,
                OutputUrl = dto.OutputLink,
                MappingTemplateId = dto.MappingTemplateId
            };

            dataContext.FileConversionHistory.Add(fileConversionEntity);
            await dataContext.SaveChangesAsync();
        }
        public async Task<IReadOnlyList<FileConversionHistoryDto>> GetFileConversionHistory()
        {
            List<FileConversionHistoryDto> fileConversations = await dataContext.FileConversionHistory
                .AsNoTracking()
                .Include(history => history.MappingTemplate)
                .ProjectTo<FileConversionHistoryDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return fileConversations;
        }

        public async Task<IReadOnlyList<WatchedUrlDto>> GetWatchedUrls()
        {
            List<WatchedUrlDto> urls = await dataContext.UrlFileGetting
                .AsNoTracking()
                .ProjectTo<WatchedUrlDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return urls;
        }
        public async Task<int> AddWatchedUrl(string url, int intervalMinutes, int mappingTemplateId)
        {
            UrlFileGettingData? existingWatchedUrl = await dataContext.UrlFileGetting.FirstOrDefaultAsync(watchedUrlEntity =>
                            watchedUrlEntity.Url == url &&
                            watchedUrlEntity.MappingTemplateId == mappingTemplateId);

            if (existingWatchedUrl is not null)
            {
                existingWatchedUrl.CheckIntervalMinutes = intervalMinutes;
                await dataContext.SaveChangesAsync();
                return existingWatchedUrl.Id;
            }

            UrlFileGettingData newWatchedUrl = new UrlFileGettingData
            {
                Url = url,
                CheckIntervalMinutes = intervalMinutes,
                MappingTemplateId = mappingTemplateId
            };

            dataContext.UrlFileGetting.Add(newWatchedUrl);
            await dataContext.SaveChangesAsync();
            return newWatchedUrl.Id;
        }
        public async Task<bool> DeleteWatchedUrl(int watchedUrlId)
        {
            UrlFileGettingData? watchedUrlEntity = await dataContext.UrlFileGetting.FindAsync(watchedUrlId);

            if (watchedUrlEntity is null)
            {
                return false;
            }

            dataContext.UrlFileGetting.Remove(watchedUrlEntity);
            await dataContext.SaveChangesAsync();
            return true;
        }

        private TemplateFieldDto ConvertFieldToDto(
            FieldData fieldEntity,
            Dictionary<int, List<FieldData>> childrenByParent)
        {
            List<FieldData> directChildren = childrenByParent.TryGetValue(fieldEntity.Id, out List<FieldData>? foundChildren)
                    ? foundChildren
                    : new List<FieldData>();

            List<FieldData> objectChildren = directChildren
                .Where(child => !child.IsArrayItem)
                .ToList();

            List<FieldData> arrayChildren = directChildren
                .Where(child => child.IsArrayItem)
                .ToList();

            return new TemplateFieldDto(
                Id: fieldEntity.Id,
                Name: fieldEntity.Name,
                Type: fieldEntity.Type,
                ItemType: fieldEntity.ItemType,
                Children: objectChildren
                    .Select(child => ConvertFieldToDto(child, childrenByParent))
                    .ToList(),
                ChildrenItems: arrayChildren
                    .Select(child => ConvertFieldToDto(child, childrenByParent))
                    .ToList()
            );
        }
    }
}
