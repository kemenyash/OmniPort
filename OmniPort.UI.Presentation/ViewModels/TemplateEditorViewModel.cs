using OmniPort.Core.Interfaces;
using OmniPort.Core.Models;
using OmniPort.Core.Records;
using OmniPort.UI.Presentation.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.ViewModels
{
    public class TemplateEditorViewModel
    {
        private readonly ITemplateManager _service;

        public TemplateEditorViewModel(ITemplateManager service)
        {
            _service = service;
        }

        public List<TemplateSummaryDto> Templates { get; private set; } = new();

        public bool IsModalOpen { get; private set; }
        public int? EditingTemplateId { get; private set; }
        public TemplateEditForm CurrentTemplate { get; private set; } = new();
        public List<TemplateFieldRow> CurrentFields => CurrentTemplate.Fields;

        public SourceType SelectedSourceType
        {
            get => CurrentTemplate.SourceType;
            set => CurrentTemplate.SourceType = value;
        }

        public async Task LoadTemplatesAsync()
        {
            Templates = (await _service.GetBasicTemplatesSummaryAsync()).ToList();
        }

        public void StartCreate()
        {
            EditingTemplateId = null;
            CurrentTemplate = new TemplateEditForm
            {
                SourceType = SourceType.CSV,
                Fields = new List<TemplateFieldRow> { new() { Name = "Name", Type = FieldDataType.String } }
            };
            IsModalOpen = true;
        }

        public async Task StartEditAsync(int id)
        {
            var dto = await _service.GetBasicTemplateAsync(id);
            if (dto is null) return;

            EditingTemplateId = dto.Id;
            CurrentTemplate = new TemplateEditForm
            {
                Id = dto.Id,
                Name = dto.Name,
                SourceType = dto.SourceType,
                Fields = dto.Fields.Select(f => new TemplateFieldRow
                {
                    Id = f.Id,
                    Name = f.Name,
                    Type = f.Type
                }).ToList()
            };
            IsModalOpen = true;
        }

        public void AddField() => CurrentTemplate.Fields.Add(new TemplateFieldRow { Name = "", Type = FieldDataType.String });
        public void RemoveField(TemplateFieldRow row) => CurrentTemplate.Fields.Remove(row);

        public void CancelEdit()
        {
            IsModalOpen = false;
            EditingTemplateId = null;
        }

        public async Task SaveAsync()
        {
            if (EditingTemplateId is null)
            {
                var create = new CreateBasicTemplateDto(
                    CurrentTemplate.Name,
                    CurrentTemplate.SourceType,
                    CurrentTemplate.Fields.Select(f => new CreateTemplateFieldDto(f.Name, f.Type)).ToList()
                );
                await _service.CreateBasicTemplateAsync(create);
            }
            else
            {
                var update = new UpdateBasicTemplateDto(
                    EditingTemplateId.Value,
                    CurrentTemplate.Name,
                    CurrentTemplate.SourceType,
                    CurrentTemplate.Fields.Select(f => new UpsertTemplateFieldDto(f.Id, f.Name, f.Type)).ToList()
                );
                await _service.UpdateBasicTemplateAsync(update);
            }

            IsModalOpen = false;
            await LoadTemplatesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            await _service.DeleteBasicTemplateAsync(id);
            await LoadTemplatesAsync();
        }
    }
}
