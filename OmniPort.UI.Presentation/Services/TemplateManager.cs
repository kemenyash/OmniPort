using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Records;
using OmniPort.Data;
using OmniPort.UI.Presentation.Helpers;
using System.Collections.Concurrent;

namespace OmniPort.UI.Presentation.Services
{
    public class TemplateManager : ITemplateManager
    {
        private readonly OmniPortDataContext omniPortDataContext;
        private readonly IMapper objectMapper;
        private readonly TemplateManagerHelpers templateManagerHelpers;

        public TemplateManager(OmniPortDataContext omniPortDataContext, IMapper objectMapper)
        {
            this.omniPortDataContext = omniPortDataContext;
            this.objectMapper = objectMapper;
            templateManagerHelpers = new TemplateManagerHelpers(omniPortDataContext);
        }

        public async Task<IReadOnlyList<TemplateSummaryDto>> GetBasicTemplatesSummary()
        {
            var basicTemplates = await omniPortDataContext.BasicTemplates
                .AsNoTracking()
                .ProjectTo<TemplateSummaryDto>(objectMapper.ConfigurationProvider)
                .ToListAsync();

            return basicTemplates;
        }

        public async Task<BasicTemplateDto?> GetBasicTemplate(int templateId)
        {
            var basicTemplateEntity = await omniPortDataContext.BasicTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(templateEntity => templateEntity.Id == templateId);

            if (basicTemplateEntity is null)
            {
                return null;
            }

            var allFieldEntities = await omniPortDataContext.Fields
                .AsNoTracking()
                .Where(fieldEntity => fieldEntity.TemplateSourceId == templateId)
                .ToListAsync();

            var rootFieldEntities = allFieldEntities
                .Where(fieldEntity => fieldEntity.ParentFieldId == null && !fieldEntity.IsArrayItem)
                .ToList();

            var childrenByParentFieldId = allFieldEntities
                .Where(fieldEntity => fieldEntity.ParentFieldId.HasValue)
                .GroupBy(fieldEntity => fieldEntity.ParentFieldId!.Value)
                .ToDictionary(group => group.Key, group => group.ToList());

            var templateFieldDtos = rootFieldEntities
                .Select(rootFieldEntity => ConvertFieldToDto(rootFieldEntity, childrenByParentFieldId))
                .ToList();

            return new BasicTemplateDto(
                Id: basicTemplateEntity.Id,
                Name: basicTemplateEntity.Name,
                SourceType: basicTemplateEntity.SourceType,
                Fields: templateFieldDtos
            );
        }

        public async Task<int> CreateBasicTemplate(CreateBasicTemplateDto createBasicTemplateDto)
        {
            var basicTemplateEntity = new BasicTemplateData
            {
                Name = createBasicTemplateDto.Name,
                SourceType = createBasicTemplateDto.SourceType
            };

            omniPortDataContext.BasicTemplates.Add(basicTemplateEntity);
            await omniPortDataContext.SaveChangesAsync();

            foreach (var createTemplateFieldDto in createBasicTemplateDto.Fields)
            {
                templateManagerHelpers.AddFieldRecursive(
                    basicTemplateEntity.Id,
                    parentId: null,
                    isArrayItem: false,
                    fieldDto: createTemplateFieldDto);
            }

            await omniPortDataContext.SaveChangesAsync();
            return basicTemplateEntity.Id;
        }

        public async Task<bool> UpdateBasicTemplate(UpdateBasicTemplateDto updateBasicTemplateDto)
        {
            var existingTemplateEntity = await omniPortDataContext.BasicTemplates
                .Include(template => template.Fields)
                .FirstOrDefaultAsync(template => template.Id == updateBasicTemplateDto.Id);

            if (existingTemplateEntity is null)
            {
                return false;
            }

            existingTemplateEntity.Name = updateBasicTemplateDto.Name;
            existingTemplateEntity.SourceType = updateBasicTemplateDto.SourceType;

            var existingRootFieldEntities = existingTemplateEntity.Fields
                .Where(fieldEntity => fieldEntity.ParentFieldId == null && !fieldEntity.IsArrayItem)
                .ToList();

            templateManagerHelpers.UpsertChildren(
                templateId: existingTemplateEntity.Id,
                parentId: null,
                isArrayItem: false,
                incomingFields: updateBasicTemplateDto.Fields,
                existingSiblingFields: existingRootFieldEntities
            );

            await omniPortDataContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteBasicTemplate(int templateId)
        {
            var templateEntity = await omniPortDataContext.BasicTemplates.FindAsync(templateId);

            if (templateEntity is null)
            {
                return false;
            }

            omniPortDataContext.BasicTemplates.Remove(templateEntity);
            await omniPortDataContext.SaveChangesAsync();
            return true;
        }

        public async Task<IReadOnlyList<JoinedTemplateSummaryDto>> GetJoinedTemplates()
        {
            var joinedTemplates = await omniPortDataContext.MappingTemplates
                .AsNoTracking()
                .Include(mapping => mapping.SourceTemplate)
                .Include(mapping => mapping.TargetTemplate)
                .ProjectTo<JoinedTemplateSummaryDto>(objectMapper.ConfigurationProvider)
                .ToListAsync();

            return joinedTemplates;
        }

        public async Task<MappingTemplateDto?> GetMappingTemplate(int mappingTemplateId)
        {
            var mappingTemplateEntity = await omniPortDataContext.MappingTemplates
                .AsNoTracking()
                .Include(mapping => mapping.SourceTemplate)
                .Include(mapping => mapping.TargetTemplate)
                .Include(mapping => mapping.MappingFields).ThenInclude(mappingField => mappingField.SourceField)
                .Include(mapping => mapping.MappingFields).ThenInclude(mappingField => mappingField.TargetField)
                .FirstOrDefaultAsync(mapping => mapping.Id == mappingTemplateId);

            if (mappingTemplateEntity is null)
            {
                return null;
            }

            return objectMapper.Map<MappingTemplateDto>(mappingTemplateEntity);
        }

        public async Task<int> CreateMappingTemplate(CreateMappingTemplateDto createMappingTemplateDto)
        {
            var mappingTemplateEntity = new MappingTemplateData
            {
                Name = createMappingTemplateDto.Name,
                SourceTemplateId = createMappingTemplateDto.SourceTemplateId,
                TargetTemplateId = createMappingTemplateDto.TargetTemplateId
            };

            omniPortDataContext.MappingTemplates.Add(mappingTemplateEntity);
            await omniPortDataContext.SaveChangesAsync();

            await templateManagerHelpers.RebuildMappingFieldsFromPaths(
                mappingTemplateEntity.Id,
                createMappingTemplateDto.SourceTemplateId,
                createMappingTemplateDto.TargetTemplateId,
                createMappingTemplateDto.Mappings
            );

            return mappingTemplateEntity.Id;
        }

        public async Task<bool> UpdateMappingTemplate(UpdateMappingTemplateDto updateMappingTemplateDto)
        {
            var existingMappingTemplateEntity = await omniPortDataContext.MappingTemplates
                .Include(mapping => mapping.MappingFields)
                .FirstOrDefaultAsync(mapping => mapping.Id == updateMappingTemplateDto.Id);

            if (existingMappingTemplateEntity is null)
            {
                return false;
            }

            existingMappingTemplateEntity.Name = updateMappingTemplateDto.Name;
            existingMappingTemplateEntity.SourceTemplateId = updateMappingTemplateDto.SourceTemplateId;
            existingMappingTemplateEntity.TargetTemplateId = updateMappingTemplateDto.TargetTemplateId;

            omniPortDataContext.MappingFields.RemoveRange(existingMappingTemplateEntity.MappingFields);
            await omniPortDataContext.SaveChangesAsync();

            await templateManagerHelpers.RebuildMappingFieldsFromPaths(
                existingMappingTemplateEntity.Id,
                updateMappingTemplateDto.SourceTemplateId,
                updateMappingTemplateDto.TargetTemplateId,
                updateMappingTemplateDto.Mappings
            );

            return true;
        }

        public async Task<bool> DeleteMappingTemplate(int mappingTemplateId)
        {
            var mappingTemplateEntity = await omniPortDataContext.MappingTemplates.FindAsync(mappingTemplateId);

            if (mappingTemplateEntity is null)
            {
                return false;
            }

            omniPortDataContext.MappingTemplates.Remove(mappingTemplateEntity);
            await omniPortDataContext.SaveChangesAsync();
            return true;
        }

        public async Task<IReadOnlyList<UrlConversionHistoryDto>> GetUrlConversionHistory()
        {
            var urlConversionHistoryItems = await omniPortDataContext.UrlConversionHistory
                .AsNoTracking()
                .Include(history => history.MappingTemplate)
                .ProjectTo<UrlConversionHistoryDto>(objectMapper.ConfigurationProvider)
                .ToListAsync();

            return urlConversionHistoryItems;
        }

        public async Task AddUrlConversion(UrlConversionHistoryDto urlConversionHistoryDto)
        {
            var urlConversionHistoryEntity = new UrlConversionHistoryData
            {
                ConvertedAt = urlConversionHistoryDto.ConvertedAt,
                InputUrl = urlConversionHistoryDto.InputUrl,
                OutputUrl = urlConversionHistoryDto.OutputLink,
                MappingTemplateId = urlConversionHistoryDto.MappingTemplateId
            };

            omniPortDataContext.UrlConversionHistory.Add(urlConversionHistoryEntity);
            await omniPortDataContext.SaveChangesAsync();
        }

        public async Task AddFileConversion(FileConversionHistoryDto fileConversionHistoryDto)
        {
            var fileConversionHistoryEntity = new FileConversionHistoryData
            {
                ConvertedAt = fileConversionHistoryDto.ConvertedAt,
                FileName = fileConversionHistoryDto.FileName,
                OutputUrl = fileConversionHistoryDto.OutputLink,
                MappingTemplateId = fileConversionHistoryDto.MappingTemplateId
            };

            omniPortDataContext.FileConversionHistory.Add(fileConversionHistoryEntity);
            await omniPortDataContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<FileConversionHistoryDto>> GetFileConversionHistory()
        {
            var fileConversionHistoryItems = await omniPortDataContext.FileConversionHistory
                .AsNoTracking()
                .Include(history => history.MappingTemplate)
                .ProjectTo<FileConversionHistoryDto>(objectMapper.ConfigurationProvider)
                .ToListAsync();

            return fileConversionHistoryItems;
        }

        public async Task<IReadOnlyList<WatchedUrlDto>> GetWatchedUrls()
        {
            var watchedUrls = await omniPortDataContext.UrlFileGetting
                .AsNoTracking()
                .ProjectTo<WatchedUrlDto>(objectMapper.ConfigurationProvider)
                .ToListAsync();

            return watchedUrls;
        }

        public async Task<int> AddWatchedUrl(string url, int intervalMinutes, int mappingTemplateId)
        {
            var existingWatchedUrlEntity = await omniPortDataContext.UrlFileGetting
                .FirstOrDefaultAsync(watchedUrlEntity =>
                    watchedUrlEntity.Url == url &&
                    watchedUrlEntity.MappingTemplateId == mappingTemplateId);

            if (existingWatchedUrlEntity is not null)
            {
                existingWatchedUrlEntity.CheckIntervalMinutes = intervalMinutes;
                await omniPortDataContext.SaveChangesAsync();
                return existingWatchedUrlEntity.Id;
            }

            var watchedUrlEntityToCreate = new UrlFileGettingData
            {
                Url = url,
                CheckIntervalMinutes = intervalMinutes,
                MappingTemplateId = mappingTemplateId
            };

            omniPortDataContext.UrlFileGetting.Add(watchedUrlEntityToCreate);
            await omniPortDataContext.SaveChangesAsync();
            return watchedUrlEntityToCreate.Id;
        }

        public async Task<bool> DeleteWatchedUrl(int watchedUrlId)
        {
            var watchedUrlEntity = await omniPortDataContext.UrlFileGetting.FindAsync(watchedUrlId);

            if (watchedUrlEntity is null)
            {
                return false;
            }

            omniPortDataContext.UrlFileGetting.Remove(watchedUrlEntity);
            await omniPortDataContext.SaveChangesAsync();
            return true;
        }

        private TemplateFieldDto ConvertFieldToDto(
            FieldData fieldEntity,
            Dictionary<int, List<FieldData>> childrenByParentFieldId)
        {
            if (!childrenByParentFieldId.TryGetValue(fieldEntity.Id, out var directChildren))
            {
                directChildren = new List<FieldData>();
            }

            var objectChildren = directChildren
                .Where(childEntity => !childEntity.IsArrayItem)
                .ToList();

            var arrayItemChildren = directChildren
                .Where(childEntity => childEntity.IsArrayItem)
                .ToList();

            return new TemplateFieldDto(
                Id: fieldEntity.Id,
                Name: fieldEntity.Name,
                Type: fieldEntity.Type,
                ItemType: fieldEntity.ItemType,
                Children: objectChildren
                    .Select(childEntity => ConvertFieldToDto(childEntity, childrenByParentFieldId))
                    .ToList(),
                ChildrenItems: arrayItemChildren
                    .Select(childEntity => ConvertFieldToDto(childEntity, childrenByParentFieldId))
                    .ToList()
            );
        }
    }
}
