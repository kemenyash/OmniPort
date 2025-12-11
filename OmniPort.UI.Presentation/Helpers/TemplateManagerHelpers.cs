using Microsoft.EntityFrameworkCore;
using OmniPort.Core.Enums;
using OmniPort.Core.Records;
using OmniPort.Data;

namespace OmniPort.UI.Presentation.Helpers
{
    internal class TemplateManagerHelpers
    {
        private readonly OmniPortDataContext dataContext;

        public TemplateManagerHelpers(OmniPortDataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        internal void AddFieldRecursive(
            int templateId,
            int? parentFieldId,
            bool isArrayItem,
            UpsertTemplateFieldDto fieldDto)
        {
            FieldData newFieldEntity = new FieldData
            {
                TemplateSourceId = templateId,
                ParentFieldId = parentFieldId,
                IsArrayItem = isArrayItem,
                Name = fieldDto.Name,
                Type = fieldDto.Type,
                ItemType = fieldDto.ItemType
            };

            dataContext.Fields.Add(newFieldEntity);
            dataContext.SaveChanges();

            if (fieldDto.Type == FieldDataType.Object)
            {
                foreach (UpsertTemplateFieldDto childField in fieldDto.Children ?? Enumerable.Empty<UpsertTemplateFieldDto>())
                {
                    AddFieldRecursive(templateId, newFieldEntity.Id, false, childField);
                }
            }
            else if (fieldDto.Type == FieldDataType.Array &&
                     fieldDto.ItemType == FieldDataType.Object)
            {
                foreach (UpsertTemplateFieldDto childField in fieldDto.ChildrenItems ?? Enumerable.Empty<UpsertTemplateFieldDto>())
                {
                    AddFieldRecursive(templateId, newFieldEntity.Id, true, childField);
                }
            }
        }

        internal void AddFieldRecursive(
            int templateId,
            int? parentId,
            bool isArrayItem,
            CreateTemplateFieldDto fieldDto)
        {
            UpsertTemplateFieldDto convertedDto = ConvertCreateToUpsert(fieldDto);
            AddFieldRecursive(templateId, parentId, isArrayItem, convertedDto);
        }

        internal static UpsertTemplateFieldDto ConvertCreateToUpsert(CreateTemplateFieldDto fieldDto)
        {
            return new UpsertTemplateFieldDto(
                Id: null,
                Name: fieldDto.Name,
                Type: fieldDto.Type,
                ItemType: fieldDto.ItemType,
                Children: (fieldDto.Children ?? new List<CreateTemplateFieldDto>())
                    .Select(ConvertCreateToUpsert)
                    .ToList(),
                ChildrenItems: (fieldDto.ChildrenItems ?? new List<CreateTemplateFieldDto>())
                    .Select(ConvertCreateToUpsert)
                    .ToList()
            );
        }

        internal void UpsertChildren(
            int templateId,
            int? parentId,
            bool isArrayItem,
            IEnumerable<UpsertTemplateFieldDto> incomingFields,
            List<FieldData> existingSiblingFields)
        {
            Dictionary<string, FieldData> existingFieldsByName =
                existingSiblingFields.ToDictionary(
                    fieldEntity => fieldEntity.Name,
                    StringComparer.OrdinalIgnoreCase
                );

            HashSet<int> processedFieldIds = new HashSet<int>();

            foreach (UpsertTemplateFieldDto incomingField in incomingFields)
            {
                if (existingFieldsByName.TryGetValue(
                        incomingField.Name,
                        out FieldData? existingFieldEntity))
                {
                    existingFieldEntity.Type = incomingField.Type;
                    existingFieldEntity.ItemType =
                        incomingField.Type == FieldDataType.Array
                            ? incomingField.ItemType
                            : null;

                    processedFieldIds.Add(existingFieldEntity.Id);

                    if (incomingField.Type == FieldDataType.Object)
                    {
                        List<FieldData> existingChildren =
                            existingFieldEntity.Children.Where(child => !child.IsArrayItem).ToList();

                        UpsertChildren(
                            templateId,
                            existingFieldEntity.Id,
                            false,
                            incomingField.Children ?? Enumerable.Empty<UpsertTemplateFieldDto>(),
                            existingChildren
                        );
                    }
                    else if (incomingField.Type == FieldDataType.Array &&
                             incomingField.ItemType == FieldDataType.Object)
                    {
                        List<FieldData> existingArrayChildren =
                            existingFieldEntity.Children.Where(child => child.IsArrayItem).ToList();

                        UpsertChildren(
                            templateId,
                            existingFieldEntity.Id,
                            true,
                            incomingField.ChildrenItems ?? Enumerable.Empty<UpsertTemplateFieldDto>(),
                            existingArrayChildren
                        );
                    }
                    else
                    {
                        List<FieldData> childrenToRemove = existingFieldEntity.Children.ToList();

                        if (childrenToRemove.Count > 0)
                        {
                            dataContext.Fields.RemoveRange(childrenToRemove);
                        }
                    }
                }
                else
                {
                    FieldData newFieldEntity = new FieldData
                    {
                        TemplateSourceId = templateId,
                        ParentFieldId = parentId,
                        IsArrayItem = isArrayItem,
                        Name = incomingField.Name,
                        Type = incomingField.Type,
                        ItemType = incomingField.Type == FieldDataType.Array
                            ? incomingField.ItemType
                            : null
                    };

                    dataContext.Fields.Add(newFieldEntity);
                    dataContext.SaveChanges();

                    if (incomingField.Type == FieldDataType.Object)
                    {
                        UpsertChildren(
                            templateId,
                            newFieldEntity.Id,
                            false,
                            incomingField.Children ?? Enumerable.Empty<UpsertTemplateFieldDto>(),
                            new List<FieldData>()
                        );
                    }
                    else if (incomingField.Type == FieldDataType.Array &&
                             incomingField.ItemType == FieldDataType.Object)
                    {
                        UpsertChildren(
                            templateId,
                            newFieldEntity.Id,
                            true,
                            incomingField.ChildrenItems ?? Enumerable.Empty<UpsertTemplateFieldDto>(),
                            new List<FieldData>()
                        );
                    }

                    processedFieldIds.Add(newFieldEntity.Id);
                }
            }

            List<FieldData> fieldsToDelete =
                existingSiblingFields.Where(existing => !processedFieldIds.Contains(existing.Id))
                                     .ToList();

            if (fieldsToDelete.Count > 0)
            {
                dataContext.Fields.RemoveRange(fieldsToDelete);
            }
        }

        internal async Task RebuildMappingFieldsFromPaths(
            int mappingTemplateId,
            int sourceTemplateId,
            int targetTemplateId,
            IEnumerable<MappingEntryDto> mappings)
        {
            List<FieldData> sourceFieldEntities = await dataContext.Fields
                .Where(field => field.TemplateSourceId == sourceTemplateId)
                .AsNoTracking()
                .ToListAsync();

            List<FieldData> targetFieldEntities = await dataContext.Fields
                .Where(field => field.TemplateSourceId == targetTemplateId)
                .AsNoTracking()
                .ToListAsync();

            Dictionary<string, int> sourcePaths = BuildPathToIdMap(sourceFieldEntities);
            Dictionary<string, int> targetPaths = BuildPathToIdMap(targetFieldEntities);

            List<MappingFieldData> mappingFieldEntities = new List<MappingFieldData>();

            foreach (MappingEntryDto mappingEntry in mappings ?? Enumerable.Empty<MappingEntryDto>())
            {
                if (string.IsNullOrWhiteSpace(mappingEntry.TargetPath))
                {
                    continue;
                }

                if (!targetPaths.TryGetValue(mappingEntry.TargetPath, out int targetFieldId))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(mappingEntry.SourcePath))
                {
                    continue;
                }

                if (!sourcePaths.TryGetValue(mappingEntry.SourcePath!, out int sourceFieldId))
                {
                    continue;
                }

                mappingFieldEntities.Add(new MappingFieldData
                {
                    MappingTemplateId = mappingTemplateId,
                    SourceFieldId = sourceFieldId,
                    TargetFieldId = targetFieldId
                });
            }

            if (mappingFieldEntities.Count > 0)
            {
                dataContext.MappingFields.AddRange(mappingFieldEntities);
                await dataContext.SaveChangesAsync();
            }
        }

        private static Dictionary<string, int> BuildPathToIdMap(List<FieldData> allFieldEntities)
        {
            Dictionary<int, List<FieldData>> childrenByParent = allFieldEntities
                    .GroupBy(field => field.ParentFieldId ?? -1)
                    .ToDictionary(group => group.Key, group => group.ToList());

            Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (childrenByParent.TryGetValue(-1, out List<FieldData>? rootFields))
            {
                foreach (FieldData? rootField in rootFields.Where(field => !field.IsArrayItem))
                {
                    WalkFieldTree(rootField, string.Empty, childrenByParent, result);
                }
            }

            return result;
        }

        private static void WalkFieldTree(
            FieldData currentField,
            string currentPath,
            Dictionary<int, List<FieldData>> childrenByParent,
            Dictionary<string, int> result)
        {
            string fullPath =
                string.IsNullOrEmpty(currentPath)
                    ? currentField.Name
                    : $"{currentPath}.{currentField.Name}";

            if (currentField.Type == FieldDataType.Object)
            {
                result[fullPath] = currentField.Id;

                if (childrenByParent.TryGetValue(currentField.Id, out List<FieldData>? childFields))
                {
                    foreach (FieldData childField in childFields.Where(child => !child.IsArrayItem))
                    {
                        WalkFieldTree(childField, fullPath, childrenByParent, result);
                    }
                }
            }
            else if (currentField.Type == FieldDataType.Array)
            {
                string arrayPath = $"{fullPath}[]";
                result[arrayPath] = currentField.Id;

                if (currentField.ItemType == FieldDataType.Object)
                {
                    if (childrenByParent.TryGetValue(currentField.Id, out List<FieldData>? childFields))
                    {
                        foreach (FieldData childField in childFields.Where(child => child.IsArrayItem))
                        {
                            WalkFieldTree(childField, arrayPath, childrenByParent, result);
                        }
                    }
                }
            }
            else
            {
                result[fullPath] = currentField.Id;
            }
        }
    }
}
