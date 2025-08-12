using Microsoft.EntityFrameworkCore;
using OmniPort.Core.Models;
using OmniPort.Data;
using OmniPort.UI.Presentation.Interfaces;

public class TransformationManager : ITransformationManager
{
    private readonly OmniPortDataContext dataContext;

    public TransformationManager(OmniPortDataContext db)
    {
        dataContext = db;
    }

    public async Task<(ImportProfile Profile, SourceType importSourceType, SourceType convertSourceType)> GetImportProfileForJoinAsync(int joinId)
    {
        var mapping = await dataContext.TemplateMappings
            .Include(m => m.SourceTemplate)
            .Include(m => m.TargetTemplate)
            .FirstOrDefaultAsync(m => m.Id == joinId);

        if (mapping is null) throw new InvalidOperationException($"Join mapping {joinId} not found.");

        var fields = await dataContext.TemplateMappingFields
            .Include(f => f.TargetField)
            .Include(f => f.SourceField)
            .Where(f => f.MappingId == joinId)
            .ToListAsync();

        var profile = new ImportProfile
        {
            Id = mapping.Id,
            ProfileName = $"{mapping.SourceTemplate.Name} → {mapping.TargetTemplate.Name}",
            Template = new ImportTemplate
            {
                Id = mapping.TargetTemplate.Id,
                TemplateName = mapping.TargetTemplate.Name,
                SourceType = mapping.TargetTemplate.SourceType,
                Fields = mapping.TargetTemplate.Fields?.Select(tf => new TemplateField
                {
                    Name = tf.Name,
                    Type = tf.Type
                }).ToList() ?? new List<TemplateField>()
            },
            Mappings = fields.Select(f => new FieldMapping
            {
                SourceField = f.SourceField?.Name ?? string.Empty,
                TargetField = f.TargetField.Name,
                TargetType = f.TargetField.Type
            }).ToList()
        };

        return (profile, mapping.SourceTemplate.SourceType, mapping.TargetTemplate.SourceType);
    }

    public async Task<List<JoinedTemplateSummary>> GetJoinedTemplatesAsync()
    {
        return await dataContext.TemplateMappings
            .Include(m => m.SourceTemplate)
            .Include(m => m.TargetTemplate)
            .Select(m => new JoinedTemplateSummary
            {
                Id = m.Id,
                SourceTemplate = m.SourceTemplate.Name,
                TargetTemplate = m.TargetTemplate.Name,
                OutputFormat = m.TargetTemplate.SourceType,
            })
            .ToListAsync();
    }

    public async Task<List<ConversionHistory>> GetFileConversionHistoryAsync()
    {
        return await dataContext.FileConversions
            .OrderByDescending(f => f.ConvertedAt)
            .Select(f => new ConversionHistory
            {
                FileName = f.FileName,
                ConvertedAt = f.ConvertedAt,
                OutputLink = f.OutputUrl,
                TemplateName = f.TemplateMap != null && f.TemplateMap.SourceField != null && f.TemplateMap.TargetField != null
                    ? $"{f.TemplateMap.SourceField.Name} → {f.TemplateMap.TargetField.Name}"
                    : "Unknown"
            })
            .ToListAsync();
    }

    public async Task<List<UrlConversionHistory>> GetUrlConversionHistoryAsync()
    {
        return await dataContext.UrlConversions
            .OrderByDescending(u => u.ConvertedAt)
            .Select(u => new UrlConversionHistory
            {
                InputUrl = u.InputUrl,
                ConvertedAt = u.ConvertedAt,
                OutputLink = u.OutputUrl,
                TemplateName = u.TemplateMap != null && u.TemplateMap.SourceField != null && u.TemplateMap.TargetField != null
                    ? $"{u.TemplateMap.SourceField.Name} → {u.TemplateMap.TargetField.Name}"
                    : "Unknown",
                TemplateMapId = u.TemplateMapId
            })
            .ToListAsync();
    }

    public async Task<List<WatchedUrl>> GetWatchedUrlsAsync()
    {
        return await dataContext.WatchedUrls
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WatchedUrl
            {
                Url = w.Url,
                IntervalMinutes = w.IntervalMinutes
            })
            .ToListAsync();
    }

    public async Task AddFileConversionAsync(ConversionHistory model)
    {
        try
        {
            var entity = new FileConversionData
            {
                FileName = model.FileName,
                ConvertedAt = model.ConvertedAt,
                OutputUrl = model.OutputLink,
                TemplateMapId = model.TemplateMapId
            };

            dataContext.FileConversions.Add(entity);
            await dataContext.SaveChangesAsync();
        }
        catch(Exception error)
        {

        }
    }

    public async Task AddUrlConversionAsync(UrlConversionHistory model)
    {
        var entity = new UrlConversionData
        {
            InputUrl = model.InputUrl,
            ConvertedAt = model.ConvertedAt,
            OutputUrl = model.OutputLink,
            TemplateMapId = model.TemplateMapId
        };

        dataContext.UrlConversions.Add(entity);
        await dataContext.SaveChangesAsync();
    }

    public async Task AddWatchedUrlAsync(WatchedUrl model)
    {
        dataContext.WatchedUrls.Add(new WatchedUrlData
        {
            Url = model.Url,
            IntervalMinutes = model.IntervalMinutes,
            CreatedAt = DateTime.UtcNow
        });
        await dataContext.SaveChangesAsync();
    }
}
