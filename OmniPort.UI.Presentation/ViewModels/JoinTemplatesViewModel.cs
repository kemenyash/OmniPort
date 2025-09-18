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

        public int? SourceId { get; private set; }
        public int? TargetId { get; private set; }

        public BasicTemplateDto? SourceTemplate { get; private set; }
        public BasicTemplateDto? TargetTemplate { get; private set; }

        public List<FlatField> SourceFlattened { get; private set; } = new();
        public List<FlatField> TargetFlattened { get; private set; } = new();

        private readonly Dictionary<string, string?> _mapByPath = new();

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

            if (SourceId.HasValue)
                SourceTemplate = Templates.FirstOrDefault(x => x.Id == SourceId.Value);
            if (TargetId.HasValue)
                TargetTemplate = Templates.FirstOrDefault(x => x.Id == TargetId.Value);

            SourceFlattened = FlattenTemplate(SourceTemplate);
            TargetFlattened = FlattenTemplate(TargetTemplate);

            var targetSet = new HashSet<string>(TargetFlattened.Select(f => f.Path));
            foreach (var k in _mapByPath.Keys.ToList())
                if (!targetSet.Contains(k)) _mapByPath.Remove(k);

            foreach (var t in TargetFlattened)
                if (!_mapByPath.ContainsKey(t.Path)) _mapByPath[t.Path] = null;
        }

        public async Task SetSourceTemplateAsync(int id)
        {
            SourceId = id;
            SourceTemplate = Templates.FirstOrDefault(x => x.Id == id);
            SourceFlattened = FlattenTemplate(SourceTemplate);
            await Task.CompletedTask;
        }

        public async Task SetTargetTemplateAsync(int id)
        {
            TargetId = id;
            TargetTemplate = Templates.FirstOrDefault(x => x.Id == id);
            TargetFlattened = FlattenTemplate(TargetTemplate);

            _mapByPath.Clear();
            foreach (var tf in TargetFlattened)
                _mapByPath[tf.Path] = null;

            await Task.CompletedTask;
        }

        public string? GetMappedValue(string targetPath)
            => _mapByPath.TryGetValue(targetPath, out var src) ? src : null;

        public void MapField(string targetPath, string? sourcePath)
        {
            if (!_mapByPath.ContainsKey(targetPath)) return;
            _mapByPath[targetPath] = string.IsNullOrWhiteSpace(sourcePath) ? null : sourcePath;
        }

        public async Task SaveMappingAsync()
        {
            if (!CanSave || TargetId is null || SourceId is null) return;

            var name = $"{SourceTemplate!.Name} → {TargetTemplate!.Name}";

            var mappings = _mapByPath
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .Select(kv => new MappingEntryDto(TargetPath: kv.Key, SourcePath: kv.Value!))
                .ToList();

            var cmd = new CreateMappingTemplateDto(
                Name: name,
                SourceTemplateId: SourceId.Value,
                TargetTemplateId: TargetId.Value,
                Mappings: mappings
            );

            await _sync.CreateMappingTemplateAsync(cmd);
            await _sync.RefreshAllAsync();
            OnChanged();
        }

        public async Task DeleteJoinTemplateAsync(int mappingTemplateId)
        {
            await _sync.DeleteMappingTemplateAsync(mappingTemplateId);
            await _sync.RefreshAllAsync();
            OnChanged();
        }



        public record FlatField(string Path, FieldDataType Type);

        private static List<FlatField> FlattenTemplate(BasicTemplateDto? tpl)
        {
            var res = new List<FlatField>();
            if (tpl is null) return res;

            var visitingIds = new HashSet<int>();

            void Walk(TemplateFieldDto f, string prefix, bool isArrayItem)
            {
                if (f.Id == 0 || !visitingIds.Add(f.Id))
                    return;

                try
                {
                    var baseName = string.IsNullOrEmpty(prefix) ? f.Name : $"{prefix}.{f.Name}";

                    if (f.Type == FieldDataType.Object)
                    {
                        res.Add(new FlatField(baseName, FieldDataType.Object));

                        foreach (var ch in f.Children ?? Enumerable.Empty<TemplateFieldDto>())
                        {
                            if (ch.Id != f.Id) Walk(ch, baseName, false);
                        }
                    }
                    else if (f.Type == FieldDataType.Array)
                    {
                        var arrPath = $"{baseName}[]";
                        res.Add(new FlatField(arrPath, FieldDataType.Array));

                        if (f.ItemType == FieldDataType.Object)
                        {
                            foreach (var ch in f.ChildrenItems ?? Enumerable.Empty<TemplateFieldDto>())
                            {
                                if (ch.Id != f.Id) Walk(ch, arrPath, true);
                            }
                        }
                    }
                    else
                    {
                        res.Add(new FlatField(baseName, f.Type));
                    }
                }
                finally
                {
                    visitingIds.Remove(f.Id);
                }
            }

            foreach (var root in tpl.Fields ?? Enumerable.Empty<TemplateFieldDto>())
                Walk(root, "", false);

            return res
                .OrderBy(ff => ff.Type == FieldDataType.Object || ff.Type == FieldDataType.Array ? 1 : 0)
                .ThenBy(ff => ff.Path, System.StringComparer.OrdinalIgnoreCase)
                .ToList();
        }


    }
}
