using OmniPort.Core.Interfaces;
using OmniPort.Core.Records;
using OmniPort.UI.Presentation.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.ViewModels
{
    public class JoinTemplatesViewModel
    {
        private readonly IAppSyncContext _sync;

        public JoinTemplatesViewModel(IAppSyncContext sync) => _sync = sync;

        public List<BasicTemplateDto> Templates { get; private set; } = new();

        public int? SourceId { get; private set; }
        public int? TargetId { get; private set; }

        public BasicTemplateDto? SourceTemplate { get; private set; }
        public BasicTemplateDto? TargetTemplate { get; private set; }

        private readonly Dictionary<int, int?> _targetToSource = new();

        public List<JoinedTemplateSummaryDto> JoinedTemplates { get; private set; } = new();

        public bool CanSave => SourceId.HasValue && TargetId.HasValue;

        public async Task InitAsync()
        {
            if (!_sync.Templates.Any())
                await _sync.InitializeAsync();

            Templates = _sync.BasicTemplatesFull.ToList();
            JoinedTemplates = _sync.JoinedTemplates.ToList();
            _sync.Changed += OnChanged;
        }

        private void OnChanged()
        {
            Templates = _sync.BasicTemplatesFull.ToList();
            JoinedTemplates = _sync.JoinedTemplates.ToList();
        }

        public Task SetSourceTemplateAsync(int id)
        {
            SourceId = id;
            SourceTemplate = Templates.FirstOrDefault(x => x.Id == id);
            return Task.CompletedTask;
        }

        public Task SetTargetTemplateAsync(int id)
        {
            TargetId = id;
            TargetTemplate = Templates.FirstOrDefault(x => x.Id == id);

            _targetToSource.Clear();
            foreach (var tf in TargetTemplate?.Fields ?? Enumerable.Empty<TemplateFieldDto>())
                _targetToSource[tf.Id] = null;

            return Task.CompletedTask;
        }

        public string? GetMappedValue(string targetFieldName)
        {
            var target = TargetTemplate?.Fields.FirstOrDefault(f => f.Name == targetFieldName);
            if (target is null) return null;

            if (_targetToSource.TryGetValue(target.Id, out var sourceId) && sourceId.HasValue)
                return SourceTemplate?.Fields.FirstOrDefault(f => f.Id == sourceId.Value)?.Name;

            return null;
        }

        public void MapField(string targetFieldName, string? sourceFieldName)
        {
            if (TargetTemplate is null) return;
            var target = TargetTemplate.Fields.FirstOrDefault(f => f.Name == targetFieldName);
            if (target is null) return;

            if (string.IsNullOrWhiteSpace(sourceFieldName))
            {
                _targetToSource[target.Id] = null;
                return;
            }

            var src = SourceTemplate?.Fields.FirstOrDefault(f => f.Name == sourceFieldName);
            _targetToSource[target.Id] = src?.Id;
        }

        public async Task SaveMappingAsync()
        {
            if (!CanSave || TargetId is null || SourceId is null) return;

            var name = $"{SourceTemplate!.Name} → {TargetTemplate!.Name}";
            var cmd = new CreateMappingTemplateDto(name, SourceId.Value, TargetId.Value, new Dictionary<int, int?>(_targetToSource));
            await _sync.CreateMappingTemplateAsync(cmd);
        }

        public async Task DeleteJoinTemplateAsync(int mappingTemplateId) => await _sync.DeleteMappingTemplateAsync(mappingTemplateId);
    }
}
