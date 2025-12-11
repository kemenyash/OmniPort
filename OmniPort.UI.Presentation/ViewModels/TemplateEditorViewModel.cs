using OmniPort.Core.Enums;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Records;
using OmniPort.UI.Presentation.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.ViewModels
{
    public class TemplateEditorViewModel
    {
        private readonly IAppSyncContext sync;
        public bool IsModalOpen { get; private set; }
        public int? EditingTemplateId { get; private set; }

        public SourceType SelectedSourceType
        {
            get => CurrentTemplate.SourceType;
            set => CurrentTemplate.SourceType = value;
        }

        public TemplateEditForm CurrentTemplate { get; private set; }
        public List<TemplateSummaryDto> Templates { get; private set; }
        public List<TemplateFieldRow> CurrentFields => CurrentTemplate.Fields;


        public TemplateEditorViewModel(IAppSyncContext sync)
        {
            this.sync = sync;
            CurrentTemplate = new TemplateEditForm();
            Templates = new List<TemplateSummaryDto>();

            _ = LoadTemplates();
        }

        
        public async Task LoadTemplates()
        {
            if (!sync.Templates.Any())
            {
                await sync.Initialize();
            }
            Templates = sync.Templates.ToList();
            sync.Changed += OnChanged;
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
        public async Task StartEdit(int id)
        {
            var full = sync.BasicTemplatesFull.FirstOrDefault(x => x.Id == id);
            if (full == null) return;

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
            await Task.CompletedTask;
        }
        public void CancelEdit()
        {
            IsModalOpen = false;
            EditingTemplateId = null;
        }

        public void AddField()
        {
            CurrentTemplate.Fields.Add(new TemplateFieldRow { Name = "", Type = FieldDataType.String });
        }
        public void RemoveField(TemplateFieldRow row)
        {
            CurrentTemplate.Fields.Remove(row);
        }

        public async Task Save()
        {
            if (EditingTemplateId is null)
            {
                var create = new CreateBasicTemplateDto(
                    CurrentTemplate.Name,
                    CurrentTemplate.SourceType,
                    CurrentTemplate.Fields.Select(ToCreate).ToList()
                );
                await sync.CreateBasicTemplate(create);
            }
            else
            {
                var update = new UpdateBasicTemplateDto(
                    EditingTemplateId.Value,
                    CurrentTemplate.Name,
                    CurrentTemplate.SourceType,
                    CurrentTemplate.Fields.Select(ToUpsert).ToList()
                );
                await sync.UpdateBasicTemplate(update);
            }

            IsModalOpen = false;
        }
        public async Task Delete(int id)
        {
            await sync.DeleteBasicTemplate(id);
        }
        
        private void OnChanged()
        {
            Templates = sync.Templates.ToList();
        }

        private static TemplateFieldRow ToRow(TemplateFieldDto field)
        {
            return new TemplateFieldRow()
            {
                Id = field.Id,
                Name = field.Name,
                Type = field.Type,
                ItemType = field.ItemType,
                Children = (field.Children ?? new List<TemplateFieldDto>()).Select(ToRow).ToList(),
                ChildrenItems = (field.ChildrenItems ?? new List<TemplateFieldDto>()).Select(ToRow).ToList()

            };
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
