using OmniPort.Core.Models;
using OmniPort.UI.Presentation.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.ViewModels
{
    public class JoinTemplatesViewModel
    {
        private readonly ITemplateManager templateManager;
        private readonly IJoinTemplateManager joinTemplateManager;

        public List<ImportTemplate> Templates { get; private set; } = new();
        public List<JoinedTemplateSummary> JoinedTemplates { get; private set; } = new();

        public int? SourceId { get; private set; }
        public int? TargetId { get; private set; }

        public ImportTemplate? SourceTemplate { get; private set; }
        public ImportTemplate? TargetTemplate { get; private set; }

        public Dictionary<string, string> Mappings { get; private set; } = new();

        public bool CanSave => SourceId.HasValue && TargetId.HasValue && Mappings.Any();

        public JoinTemplatesViewModel(ITemplateManager templateManager, IJoinTemplateManager joinTemplateManager)
        {
            this.templateManager = templateManager;
            this.joinTemplateManager = joinTemplateManager;
        }

        public async Task InitAsync()
        {
            Templates = await templateManager.GetTemplatesAsync();
            JoinedTemplates = await joinTemplateManager.GetJoinedTemplatesAsync();

            if (Templates.Count >= 2)
            {
                SourceId = Templates[0].Id;
                TargetId = Templates[1].Id;
                await LoadTemplates();
            }
        }

        public async Task SetSourceTemplateAsync(int id)
        {
            SourceId = id;
            Mappings.Clear();
            await LoadTemplates();
        }

        public async Task SetTargetTemplateAsync(int id)
        {
            TargetId = id;
            Mappings.Clear();
            await LoadTemplates();
        }

        public void MapField(string targetField, string? sourceField)
        {
            if (string.IsNullOrWhiteSpace(sourceField))
                Mappings.Remove(targetField);
            else
                Mappings[targetField] = sourceField;
        }

        public string? GetMappedValue(string targetField) =>
            Mappings.TryGetValue(targetField, out var val) ? val : null;

        public async Task SaveMappingAsync()
        {
            if (!CanSave || SourceTemplate is null || TargetTemplate is null)
                return;

            var profile = new ImportProfile
            {
                ProfileName = $"{SourceTemplate.TemplateName} -> {TargetTemplate.TemplateName}",
                Template = TargetTemplate,
                Mappings = Mappings.Select(kv =>
                {
                    var field = TargetTemplate.Fields.FirstOrDefault(f => f.Name == kv.Key);
                    return new FieldMapping
                    {
                        SourceField = kv.Value,
                        TargetField = kv.Key,
                        TargetType = field?.Type ?? FieldDataType.String
                    };
                }).ToList()
            };

            await joinTemplateManager.SaveMappingAsync(profile, SourceId.Value);
            JoinedTemplates = await joinTemplateManager.GetJoinedTemplatesAsync();
        }

        public async Task DeleteJoinTemplateAsync(int id)
        {
            await joinTemplateManager.DeleteJoinTemplateAsync(id);
            JoinedTemplates = await joinTemplateManager.GetJoinedTemplatesAsync();
        }

        private async Task LoadTemplates()
        {
            if (SourceId.HasValue)
                SourceTemplate = await templateManager.GetTemplateByIdAsync(SourceId.Value);

            if (TargetId.HasValue)
                TargetTemplate = await templateManager.GetTemplateByIdAsync(TargetId.Value);
        }
    }
}
