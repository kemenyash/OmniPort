using OmniPort.Core.Enums;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Records;
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

        public IReadOnlyList<(string Path, FieldDataType Type)> SourceFlattened => _sourcePaths;
        public IReadOnlyList<(string Path, FieldDataType Type)> TargetFlattened => _targetPaths;


        public int? SourceId { get; private set; }
        public int? TargetId { get; private set; }

        public BasicTemplateDto? SourceTemplate { get; private set; }
        public BasicTemplateDto? TargetTemplate { get; private set; }

        private List<(string Path, FieldDataType Type)> _sourcePaths = new();
        private List<(string Path, FieldDataType Type)> _targetPaths = new();
        private readonly Dictionary<string, string?> _targetToSourcePath = new(StringComparer.OrdinalIgnoreCase);

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
            _sourcePaths = FieldPathHelper.FlattenMany(SourceTemplate?.Fields ?? Array.Empty<TemplateFieldDto>()).ToList();
            return Task.CompletedTask;
        }

        public Task SetTargetTemplateAsync(int id)
        {
            TargetId = id;
            TargetTemplate = Templates.FirstOrDefault(x => x.Id == id);
            _targetPaths = FieldPathHelper.FlattenMany(TargetTemplate?.Fields ?? Array.Empty<TemplateFieldDto>()).ToList();

            _targetToSourcePath.Clear();
            foreach (var t in _targetPaths) _targetToSourcePath[t.Path] = null;

            return Task.CompletedTask;
        }

        public string? GetMappedValue(string targetPath)
        {
            return _targetToSourcePath.TryGetValue(targetPath, out var sp) ? sp : null;
        }

        public void MapField(string targetPath, string? sourcePath)
        {
            if (!_targetToSourcePath.ContainsKey(targetPath)) return;

            _targetToSourcePath[targetPath] = string.IsNullOrWhiteSpace(sourcePath) ? null : sourcePath;
        }

        public async Task SaveMappingAsync()
        {
            if (!CanSave || TargetId is null || SourceId is null) return;

            var name = $"{SourceTemplate!.Name} → {TargetTemplate!.Name}";
            var mappings = _targetToSourcePath.Select(kv => new MappingEntryDto(kv.Key, kv.Value)).ToList();

            var cmd = new CreateMappingTemplateDto(
                name,
                SourceId.Value,
                TargetId.Value,
                mappings
            );
            await _sync.CreateMappingTemplateAsync(cmd);
        }

        public async Task DeleteJoinTemplateAsync(int mappingTemplateId) => await _sync.DeleteMappingTemplateAsync(mappingTemplateId);
    }
}
