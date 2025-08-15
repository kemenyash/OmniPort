using Microsoft.EntityFrameworkCore;
using OmniPort.Core.Models;
using OmniPort.Data;

using OmniPort.UI.Presentation.Interfaces;
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

    public async Task<(ImportProfile Profile, SourceType ImportSourceType, SourceType ConvertSourceType)> GetImportProfileForJoinAsync(int mappingTemplateId)
    {
        var mapping = await dataContext.MappingTemplates
            .Include(m => m.SourceTemplate).ThenInclude(t => t.Fields)
            .Include(m => m.TargetTemplate).ThenInclude(t => t.Fields)
            .FirstOrDefaultAsync(m => m.Id == mappingTemplateId);

        if (mapping is null)
            throw new InvalidOperationException($"Join mapping {mappingTemplateId} not found.");

        var fields = await dataContext.MappingFields
            .Include(f => f.TargetField)
            .Include(f => f.SourceField)
            .Where(f => f.MappingTemplateId == mappingTemplateId)
            .ToListAsync();


        var importTemplate = new ImportTemplate
        {
            Id = mapping.TargetTemplateId,
            TemplateName = mapping.TargetTemplate.Name,
            SourceType = mapping.TargetTemplate.SourceType,
            Fields = mapping.TargetTemplate.Fields?.Select(tf => new TemplateField
            {
                Name = tf.Name,
                Type = tf.Type
            }).ToList() ?? new List<TemplateField>()
        };


        var mappings = fields.Select(f => new FieldMapping
        {
            SourceField = f.SourceField?.Name ?? string.Empty,
            TargetField = f.TargetField.Name,
            TargetType = f.TargetField.Type
        }).ToList();

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
