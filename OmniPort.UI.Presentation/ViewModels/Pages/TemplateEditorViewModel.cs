using OmniPort.Core.Enums;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Records;
using OmniPort.UI.Presentation.Models;

namespace OmniPort.UI.Presentation.ViewModels.Pages
{
    public class TemplateEditorViewModel
    {
        private readonly IAppSyncContext sync;

        public event Action? Changed;

        public bool IsModalOpen { get; private set; }
        public int? EditingTemplateId { get; private set; }

        public SourceType SelectedSourceType
        {
            get => CurrentTemplate.SourceType;
            set
            {
                CurrentTemplate.SourceType = value;
                Changed?.Invoke();
            }
        }

        public TemplateEditForm CurrentTemplate { get; private set; }
        public List<TemplateSummaryDto> Templates { get; private set; }
        public List<TemplateFieldRow> CurrentFields => CurrentTemplate.Fields;

        public TemplateEditorViewModel(IAppSyncContext sync)
        {
            this.sync = sync;

            CurrentTemplate = new TemplateEditForm();
            Templates = new List<TemplateSummaryDto>();
        }

        public async Task Initialize()
        {
            if (!sync.Templates.Any())
            {
                await sync.Initialize();
            }

            Templates = sync.Templates.ToList();
            sync.Changed += OnChanged;

            Changed?.Invoke();
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
            Changed?.Invoke();
        }

        public Task StartEdit(int id)
        {
            BasicTemplateDto? full = sync.BasicTemplatesFull.FirstOrDefault(x => x.Id == id);
            if (full == null) return Task.CompletedTask;

            EditingTemplateId = full.Id;

            TemplateFieldRow ToRow(TemplateFieldDto templateField)
            {
                return new TemplateFieldRow
                {
                    Id = templateField.Id,
                    Name = templateField.Name,
                    Type = templateField.Type,
                    ItemType = templateField.ItemType,
                    Children = (templateField.Children ?? Array.Empty<TemplateFieldDto>()).Select(ToRow).ToList(),
                    ChildrenItems = (templateField.ChildrenItems ?? Array.Empty<TemplateFieldDto>()).Select(ToRow).ToList()
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
            Changed?.Invoke();
            return Task.CompletedTask;
        }

        public void CancelEdit()
        {
            IsModalOpen = false;
            EditingTemplateId = null;
            Changed?.Invoke();
        }

        public void AddField()
        {
            CurrentTemplate.Fields.Add(new TemplateFieldRow { Name = "", Type = FieldDataType.String });
            Changed?.Invoke();
        }

        public void RemoveField(TemplateFieldRow row)
        {
            CurrentTemplate.Fields.Remove(row);
            Changed?.Invoke();
        }

        public async Task Save()
        {
            if (EditingTemplateId is null)
            {
                CreateBasicTemplateDto create = new CreateBasicTemplateDto(
                    CurrentTemplate.Name,
                    CurrentTemplate.SourceType,
                    CurrentTemplate.Fields.Select(ToCreate).ToList()
                );
                await sync.CreateBasicTemplate(create);
            }
            else
            {
                UpdateBasicTemplateDto update = new UpdateBasicTemplateDto(
                    EditingTemplateId.Value,
                    CurrentTemplate.Name,
                    CurrentTemplate.SourceType,
                    CurrentTemplate.Fields.Select(ToUpsert).ToList()
                );
                await sync.UpdateBasicTemplate(update);
            }

            IsModalOpen = false;
            Changed?.Invoke();
        }

        public async Task Delete(int id)
        {
            await sync.DeleteBasicTemplate(id);
            Changed?.Invoke();
        }

        private void OnChanged()
        {
            Templates = sync.Templates.ToList();
            Changed?.Invoke();
        }

        private static CreateTemplateFieldDto ToCreate(TemplateFieldRow row)
        {
            return new CreateTemplateFieldDto(
                 Name: row.Name,
                 Type: row.Type,
                 ItemType: row.ItemType,
                 Children: (row.Children ?? new List<TemplateFieldRow>()).Select(ToCreate).ToList(),
                 ChildrenItems: (row.ChildrenItems ?? new List<TemplateFieldRow>()).Select(ToCreate).ToList()
             );
        }

        private static UpsertTemplateFieldDto ToUpsert(TemplateFieldRow row)
        {
            return new UpsertTemplateFieldDto(
                Id: row.Id,
                Name: row.Name,
                Type: row.Type,
                ItemType: row.ItemType,
                Children: (row.Children ?? new List<TemplateFieldRow>()).Select(ToUpsert).ToList(),
                ChildrenItems: (row.ChildrenItems ?? new List<TemplateFieldRow>()).Select(ToUpsert).ToList()
            );
        }
    }
}
