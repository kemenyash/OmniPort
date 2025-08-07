using OmniPort.Core.Models;
using OmniPort.UI.Presentation.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.ViewModels
{
    public class TemplateEditorViewModel
    {
        private readonly ITemplateManager templateManager;

        public List<TemplateSummary> Templates { get; private set; } = new();
        public ImportTemplate CurrentTemplate { get; private set; } = new();
        public List<FieldMapping> CurrentFields { get; private set; } = new();
        public SourceType SelectedSourceType { get; set; } = SourceType.CSV;
        public int? EditingTemplateId { get; private set; }
        public bool IsModalOpen { get; private set; }

        public TemplateEditorViewModel(ITemplateManager templateManager)
        {
            this.templateManager = templateManager;
        }

        public async Task LoadTemplatesAsync()
        {
            Templates = await templateManager.GetTemplatesSummaryAsync();
        }

        public void StartCreate()
        {
            CurrentTemplate = new ImportTemplate();
            CurrentFields = new List<FieldMapping> { new() { SourceField = "", TargetType = FieldDataType.String } };
            SelectedSourceType = SourceType.CSV;
            EditingTemplateId = null;
            IsModalOpen = true;
        }

        public async Task StartEditAsync(int id)
        {
            var template = await templateManager.GetTemplateAsync(id);
            var fields = await templateManager.GetMappingsByTemplateIdAsync(id);

            if (template is null || fields is null)
                return;

            CurrentTemplate = template;
            CurrentFields = fields.ToList();
            SelectedSourceType = template.SourceType;
            EditingTemplateId = id;
            IsModalOpen = true;
        }

        public void CancelEdit()
        {
            IsModalOpen = false;
            EditingTemplateId = null;
        }

        public void AddField() =>
            CurrentFields.Add(new FieldMapping { SourceField = "", TargetType = FieldDataType.String });

        public void RemoveField(FieldMapping field) =>
            CurrentFields.Remove(field);

        public async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentTemplate.TemplateName) || !CurrentFields.Any())
                return;

            CurrentTemplate.Fields = CurrentFields
                .Select(f => new TemplateField { Name = f.SourceField, Type = f.TargetType })
                .ToList();

            CurrentTemplate.SourceType = SelectedSourceType;

            if (EditingTemplateId.HasValue)
            {
                await templateManager.UpdateTemplateByIdAsync(EditingTemplateId.Value, CurrentTemplate, CurrentFields);
            }
            else
            {
                await templateManager.CreateTemplateAsync(CurrentTemplate, SelectedSourceType, CurrentFields);
            }

            await LoadTemplatesAsync();
            CancelEdit();
        }

        public async Task DeleteAsync(int id)
        {
            await templateManager.DeleteTemplateByIdAsync(id);
            await LoadTemplatesAsync();
        }
    }
}
