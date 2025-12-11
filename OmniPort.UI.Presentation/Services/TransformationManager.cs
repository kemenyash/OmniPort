using Microsoft.EntityFrameworkCore;
using OmniPort.Core.Enums;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Models;
using OmniPort.Data;

public class TransformationManager : ITransformationManager
{
    private readonly OmniPortDataContext dataContext;

    public TransformationManager(OmniPortDataContext db)
    {
        dataContext = db;
    }

    public async Task<(ImportProfile Profile, SourceType ImportSourceType, SourceType ConvertSourceType)>
        GetImportProfileForJoinAsync(int mappingTemplateId)
    {
        MappingTemplateData? mapping = await dataContext.MappingTemplates
            .Include(m => m.SourceTemplate)
            .Include(m => m.TargetTemplate)
            .FirstOrDefaultAsync(m => m.Id == mappingTemplateId);

        if (mapping is null)
            throw new InvalidOperationException($"Join mapping {mappingTemplateId} not found.");

        List<FieldData> sourceFields = await dataContext.Fields
            .Where(f => f.TemplateSourceId == mapping.SourceTemplateId)
            .AsNoTracking()
            .ToListAsync();

        List<FieldData> targetFields = await dataContext.Fields
            .Where(f => f.TemplateSourceId == mapping.TargetTemplateId)
            .AsNoTracking()
            .ToListAsync();

        Dictionary<int, FieldData> byId = targetFields.Concat(sourceFields).ToDictionary(f => f.Id);

        string BuildPath(FieldData f)
        {
            List<string> parts = new List<string>();
            FieldData cur = f;
            while (cur != null!)
            {
                string seg = cur.Name;

                if (cur.IsArrayItem)
                {

                }
                parts.Add(seg);

                if (cur.ParentFieldId is null) break;

                FieldData parent = byId[cur.ParentFieldId.Value];
                if (parent.Type == FieldDataType.Array)
                {
                    parts.Add(parent.Name + "[]");
                    cur = parent.ParentFieldId is null ? null! : byId[parent.ParentFieldId.Value];
                }
                else
                {
                    cur = parent;
                }
            }

            parts.Reverse();

            return string.Join('.', parts);
        }

        static bool IsLeaf(FieldData f) =>
            f.Type switch
            {
                FieldDataType.Object => false,
                FieldDataType.Array => true,
                _ => true,
            };

        List<FieldData> targetLeafs = targetFields.Where(IsLeaf).ToList();

        ImportTemplate importTemplate = new ImportTemplate
        {
            Id = mapping.TargetTemplateId,
            TemplateName = mapping.TargetTemplate.Name,
            SourceType = mapping.TargetTemplate.SourceType,
            Fields = targetLeafs.Select(tf => new TemplateField
            {
                Name = BuildPath(tf),
                Type = tf.Type
            }).ToList()
        };

        List<MappingFieldData> mf = await dataContext.MappingFields
            .Where(x => x.MappingTemplateId == mappingTemplateId)
            .AsNoTracking()
            .ToListAsync();

        Dictionary<int, FieldData> targetById = targetFields.ToDictionary(x => x.Id);
        Dictionary<int, FieldData> sourceById = sourceFields.ToDictionary(x => x.Id);

        List<FieldMapping> mappings = new List<FieldMapping>(mf.Count);
        foreach (MappingFieldData? m in mf)
        {
            if (!sourceById.TryGetValue(m.SourceFieldId, out FieldData? s) ||
                !targetById.TryGetValue(m.TargetFieldId, out FieldData? t))
                continue;

            mappings.Add(new FieldMapping
            {
                SourceField = BuildPath(s),
                TargetField = BuildPath(t),
                TargetType = t.Type
            });
        }

        ImportProfile profile = new ImportProfile
        {
            Id = mapping.Id,
            ProfileName = $"{mapping.SourceTemplate.Name} → {mapping.TargetTemplate.Name}",
            Template = importTemplate,
            Mappings = mappings
        };

        return (profile, mapping.SourceTemplate.SourceType, mapping.TargetTemplate.SourceType);
    }
}
