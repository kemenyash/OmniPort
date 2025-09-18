using OmniPort.Core.Enums;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Records;
using OmniPort.UI.Presentation.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.ViewModels
{
    public class TemplateEditorViewModel
    {
        private readonly IAppSyncContext _sync;

        public TemplateEditorViewModel(IAppSyncContext sync) => _sync = sync;

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
            if (!_sync.Templates.Any())
                await _sync.InitializeAsync();
            Templates = _sync.Templates.ToList();
            _sync.Changed += OnChanged;
        }

        private void OnChanged()
        {
            Templates = _sync.Templates.ToList();
        }

        public void StartCreate()
        {
            EditingTemplateId = null;
            CurrentTemplate = new TemplateEditForm
            {
                SourceType = SourceType.CSV,
                Fields = new List<TemplateFieldRow>
                {
                    new() { Name = "Name", Type = FieldDataType.String }
                }
            };
            IsModalOpen = true;
        }

        public async Task StartEditAsync(int id)
        {
            var full = _sync.BasicTemplatesFull.FirstOrDefault(x => x.Id == id);
            if (full == null) return;

            EditingTemplateId = full.Id;

            TemplateFieldRow ToRow(TemplateFieldDto f)
            {
                return new TemplateFieldRow
                {
                    Id = f.Id,
                    Name = f.Name,
                    Type = f.Type,
                    ItemType = f.ItemType,
                    Children = (f.Children ?? Array.Empty<TemplateFieldDto>()).Select(ToRow).ToList(),
                    ChildrenItems = (f.ChildrenItems ?? Array.Empty<TemplateFieldDto>()).Select(ToRow).ToList()
                };
            }

            CurrentTemplate = new TemplateEditForm
            {
                Id = full.Id,
                Name = full.Name,
                SourceType = full.SourceType,
                Fields = full.Fields.Select(ToRow).ToList()
            };

            IsModalOpen = true;
            await Task.CompletedTask;
        }


        public void AddField() =>
            CurrentTemplate.Fields.Add(new TemplateFieldRow { Name = "", Type = FieldDataType.String });

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
                    CurrentTemplate.Fields.Select(ToCreate).ToList()
                );
                await _sync.CreateBasicTemplateAsync(create);
            }
            else
            {
                var update = new UpdateBasicTemplateDto(
                    EditingTemplateId.Value,
                    CurrentTemplate.Name,
                    CurrentTemplate.SourceType,
                    CurrentTemplate.Fields.Select(ToUpsert).ToList()
                );
                await _sync.UpdateBasicTemplateAsync(update);
            }

            IsModalOpen = false;
        }

        public async Task DeleteAsync(int id) => await _sync.DeleteBasicTemplateAsync(id);

        private static TemplateFieldRow ToRow(TemplateFieldDto f) => new()
        {
            Id = f.Id,
            Name = f.Name,
            Type = f.Type,
            ItemType = f.ItemType,
            Children = (f.Children ?? new List<TemplateFieldDto>()).Select(ToRow).ToList(),
            ChildrenItems = (f.ChildrenItems ?? new List<TemplateFieldDto>()).Select(ToRow).ToList()
        };

        private static CreateTemplateFieldDto ToCreate(TemplateFieldRow r) =>
            new(
                Name: r.Name,
                Type: r.Type,
                ItemType: r.ItemType,
                Children: (r.Children ?? new List<TemplateFieldRow>()).Select(ToCreate).ToList(),
                ChildrenItems: (r.ChildrenItems ?? new List<TemplateFieldRow>()).Select(ToCreate).ToList()
            );

        private static UpsertTemplateFieldDto ToUpsert(TemplateFieldRow r) =>
            new(
                Id: r.Id,
                Name: r.Name,
                Type: r.Type,
                ItemType: r.ItemType,
                Children: (r.Children ?? new List<TemplateFieldRow>()).Select(ToUpsert).ToList(),
                ChildrenItems: (r.ChildrenItems ?? new List<TemplateFieldRow>()).Select(ToUpsert).ToList()
            );
    }
}
