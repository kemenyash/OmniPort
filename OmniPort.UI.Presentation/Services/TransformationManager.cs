using Microsoft.EntityFrameworkCore;
using OmniPort.Core.Enums;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Models;
using OmniPort.Data;

public class TransformationManager : ITransformationManager
{
    private readonly OmniPortDataContext omniPortDataContext;

    public TransformationManager(OmniPortDataContext omniPortDataContext)
    {
        this.omniPortDataContext = omniPortDataContext;
    }

    public async Task<(ImportProfile Profile, SourceType ImportSourceType, SourceType ConvertSourceType)>
        GetImportProfileForJoin(int mappingTemplateId)
    {
        var mappingTemplateData = await omniPortDataContext.MappingTemplates
            .Include(mappingTemplate => mappingTemplate.SourceTemplate)
            .Include(mappingTemplate => mappingTemplate.TargetTemplate)
            .FirstOrDefaultAsync(mappingTemplate => mappingTemplate.Id == mappingTemplateId);

        if (mappingTemplateData is null)
        {
            throw new InvalidOperationException($"Join mapping {mappingTemplateId} not found.");
        }

        var sourceTemplateFields = await omniPortDataContext.Fields
            .Where(field => field.TemplateSourceId == mappingTemplateData.SourceTemplateId)
            .AsNoTracking()
            .ToListAsync();

        var targetTemplateFields = await omniPortDataContext.Fields
            .Where(field => field.TemplateSourceId == mappingTemplateData.TargetTemplateId)
            .AsNoTracking()
            .ToListAsync();

        var fieldsById = targetTemplateFields
            .Concat(sourceTemplateFields)
            .ToDictionary(field => field.Id);

        var targetTemplateLeafFields = targetTemplateFields
            .Where(IsLeafField)
            .ToList();

        var importTemplate = new ImportTemplate
        {
            Id = mappingTemplateData.TargetTemplateId,
            TemplateName = mappingTemplateData.TargetTemplate.Name,
            SourceType = mappingTemplateData.TargetTemplate.SourceType,
            Fields = targetTemplateLeafFields
                .Select(targetField => new TemplateField
                {
                    Name = BuildFieldPath(targetField, fieldsById),
                    Type = targetField.Type
                })
                .ToList()
        };

        var mappingFields = await omniPortDataContext.MappingFields
            .Where(mappingField => mappingField.MappingTemplateId == mappingTemplateId)
            .AsNoTracking()
            .ToListAsync();

        var targetFieldsById = targetTemplateFields.ToDictionary(field => field.Id);
        var sourceFieldsById = sourceTemplateFields.ToDictionary(field => field.Id);

        var fieldMappings = new List<FieldMapping>(mappingFields.Count);

        foreach (var mappingField in mappingFields)
        {
            if (!sourceFieldsById.TryGetValue(mappingField.SourceFieldId, out var sourceField))
            {
                continue;
            }

            if (!targetFieldsById.TryGetValue(mappingField.TargetFieldId, out var targetField))
            {
                continue;
            }

            fieldMappings.Add(new FieldMapping
            {
                SourceField = BuildFieldPath(sourceField, fieldsById),
                TargetField = BuildFieldPath(targetField, fieldsById),
                TargetType = targetField.Type
            });
        }

        var importProfile = new ImportProfile
        {
            Id = mappingTemplateData.Id,
            ProfileName = $"{mappingTemplateData.SourceTemplate.Name} → {mappingTemplateData.TargetTemplate.Name}",
            Template = importTemplate,
            Mappings = fieldMappings
        };

        return (importProfile, mappingTemplateData.SourceTemplate.SourceType, mappingTemplateData.TargetTemplate.SourceType);
    }

    private static bool IsLeafField(FieldData fieldData)
    {
        switch (fieldData.Type)
        {
            case FieldDataType.Object:
                {
                    return false;
                }
            case FieldDataType.Array:
                {
                    return true;
                }
            default:
                {
                    return true;
                }
        }
    }

    private static string BuildFieldPath(FieldData fieldData, Dictionary<int, FieldData> fieldsById)
    {
        var pathSegments = new List<string>();
        var currentField = fieldData;

        while (currentField != null)
        {
            pathSegments.Add(currentField.Name);

            if (currentField.ParentFieldId is null)
            {
                break;
            }

            var parentField = fieldsById[currentField.ParentFieldId.Value];

            if (parentField.Type == FieldDataType.Array)
            {
                pathSegments.Add(parentField.Name + "[]");

                if (parentField.ParentFieldId is null)
                {
                    currentField = null;
                }
                else
                {
                    currentField = fieldsById[parentField.ParentFieldId.Value];
                }
            }
            else
            {
                currentField = parentField;
            }
        }

        pathSegments.Reverse();

        return string.Join('.', pathSegments);
    }
}
