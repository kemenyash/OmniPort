using Microsoft.EntityFrameworkCore;
using OmniPort.Core.Enums;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Models;
using OmniPort.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        var mapping = await dataContext.MappingTemplates
            .Include(m => m.SourceTemplate)
            .Include(m => m.TargetTemplate)
            .FirstOrDefaultAsync(m => m.Id == mappingTemplateId);

        if (mapping is null)
            throw new InvalidOperationException($"Join mapping {mappingTemplateId} not found.");

        var sourceFields = await dataContext.Fields
            .Where(f => f.TemplateSourceId == mapping.SourceTemplateId)
            .AsNoTracking()
            .ToListAsync();

        var targetFields = await dataContext.Fields
            .Where(f => f.TemplateSourceId == mapping.TargetTemplateId)
            .AsNoTracking()
            .ToListAsync();

        var byId = targetFields.Concat(sourceFields).ToDictionary(f => f.Id);

        string BuildPath(FieldData f)
        {
            var parts = new List<string>();
            var cur = f;
            while (cur != null!)
            {
                var seg = cur.Name;

                if (cur.IsArrayItem)
                {

                }
                parts.Add(seg);

                if (cur.ParentFieldId is null) break;

                var parent = byId[cur.ParentFieldId.Value];
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

        var targetLeafs = targetFields.Where(IsLeaf).ToList();

        var importTemplate = new ImportTemplate
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

        var mf = await dataContext.MappingFields
            .Where(x => x.MappingTemplateId == mappingTemplateId)
            .AsNoTracking()
            .ToListAsync();

        var targetById = targetFields.ToDictionary(x => x.Id);
        var sourceById = sourceFields.ToDictionary(x => x.Id);

        var mappings = new List<FieldMapping>(mf.Count);
        foreach (var m in mf)
        {
            if (!sourceById.TryGetValue(m.SourceFieldId, out var s) ||
                !targetById.TryGetValue(m.TargetFieldId, out var t))
                continue;

            mappings.Add(new FieldMapping
            {
                SourceField = BuildPath(s),  
                TargetField = BuildPath(t),  
                TargetType = t.Type
            });
        }

        var profile = new ImportProfile
        {
            Id = mapping.Id,
            ProfileName = $"{mapping.SourceTemplate.Name} → {mapping.TargetTemplate.Name}",
            Template = importTemplate,
            Mappings = mappings
        };

        return (profile, mapping.SourceTemplate.SourceType, mapping.TargetTemplate.SourceType);
    }
}
