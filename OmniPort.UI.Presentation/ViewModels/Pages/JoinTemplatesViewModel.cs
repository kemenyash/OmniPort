using OmniPort.Core.Enums;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Records;

namespace OmniPort.UI.Presentation.ViewModels.Pages
{
    public class JoinTemplatesViewModel
    {
        private readonly IAppSyncContext syncContext;
        private readonly Dictionary<string, string?> mapByPath;

        public event Action? Changed;

        public record FlatField(string Path, FieldDataType Type);

        public bool CanSave => SourceId.HasValue && TargetId.HasValue;

        public int? SourceId { get; private set; }
        public int? TargetId { get; private set; }

        public List<BasicTemplateDto> Templates { get; private set; }
        public BasicTemplateDto? SourceTemplate { get; private set; }
        public BasicTemplateDto? TargetTemplate { get; private set; }
        public List<FlatField> SourceFlattened { get; private set; }
        public List<FlatField> TargetFlattened { get; private set; }
        public List<JoinedTemplateSummaryDto> JoinedTemplates { get; private set; }

        public JoinTemplatesViewModel(IAppSyncContext syncContext)
        {
            this.syncContext = syncContext;

            mapByPath = new Dictionary<string, string?>();
            Templates = new List<BasicTemplateDto>();
            SourceFlattened = new List<FlatField>();
            TargetFlattened = new List<FlatField>();
            JoinedTemplates = new List<JoinedTemplateSummaryDto>();
        }

        public async Task InitializeAsync()
        {
            if (!syncContext.Templates.Any())
            {
                await syncContext.Initialize();
            }

            Templates = syncContext.BasicTemplatesFull.ToList();
            JoinedTemplates = syncContext.JoinedTemplates.ToList();

            syncContext.Changed += OnChanged;
            Changed?.Invoke();
        }

        public Task SetSourceTemplate(int id)
        {
            SourceId = id;
            SourceTemplate = Templates.FirstOrDefault(x => x.Id == id);
            SourceFlattened = FlattenTemplate(SourceTemplate);
            Changed?.Invoke();
            return Task.CompletedTask;
        }

        public Task SetTargetTemplate(int id)
        {
            TargetId = id;
            TargetTemplate = Templates.FirstOrDefault(x => x.Id == id);
            TargetFlattened = FlattenTemplate(TargetTemplate);

            mapByPath.Clear();
            foreach (FlatField target in TargetFlattened)
            {
                mapByPath[target.Path] = null;
            }

            Changed?.Invoke();
            return Task.CompletedTask;
        }

        public string? GetMappedValue(string targetPath)
        {
            return mapByPath.TryGetValue(targetPath, out string? src) ? src : null;
        }

        public void MapField(string targetPath, string? sourcePath)
        {
            if (!mapByPath.ContainsKey(targetPath)) return;
            mapByPath[targetPath] = string.IsNullOrWhiteSpace(sourcePath) ? null : sourcePath;
            Changed?.Invoke();
        }

        public async Task SaveMapping()
        {
            if (!CanSave || TargetId is null || SourceId is null) return;

            string name = $"{SourceTemplate!.Name} → {TargetTemplate!.Name}";

            List<MappingEntryDto> mappings = mapByPath
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .Select(kv => new MappingEntryDto(TargetPath: kv.Key, SourcePath: kv.Value!))
                .ToList();

            CreateMappingTemplateDto creatingMappingTemplate = new CreateMappingTemplateDto(
                Name: name,
                SourceTemplateId: SourceId.Value,
                TargetTemplateId: TargetId.Value,
                Mappings: mappings
            );

            await syncContext.CreateMappingTemplate(creatingMappingTemplate);
            await syncContext.RefreshAll();
            OnChanged();
        }

        public async Task DeleteJoinTemplate(int mappingTemplateId)
        {
            await syncContext.DeleteMappingTemplate(mappingTemplateId);
            await syncContext.RefreshAll();
            OnChanged();
        }

        public bool IsTopOptionCandidate(FlatField flatField)
        {
            return (!flatField.Path.Contains('.') && !flatField.Path.Contains("[]"))
                || flatField.Type == FieldDataType.Object
                || flatField.Type == FieldDataType.Array;
        }

        public string? GetTopSelectValue(string? fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return null;
            int dot = fullPath.IndexOf('.');
            return dot >= 0 ? fullPath[..dot] : fullPath;
        }

        public int GetDepth(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return 0;

            int dotDepth = path.Count(c => c == '.');
            int arrDepth = path.Split("[]").Length - 1;

            return dotDepth + arrDepth;
        }

        public bool HasDescendants(string sourcePath)
        {
            FieldDataType? sourceType = GetSourceType(sourcePath);
            if (sourceType == FieldDataType.Object)
            {
                return SourceFlattened.Any(x =>
                    x.Path.StartsWith(sourcePath + ".", StringComparison.Ordinal));
            }

            if (sourceType == FieldDataType.Array)
            {
                return SourceFlattened.Any(x =>
                    x.Path.StartsWith(sourcePath + "[]", StringComparison.Ordinal));
            }

            return false;
        }

        public IEnumerable<FlatField> GetDescendants(string sourcePath)
        {
            FieldDataType? sourceType = GetSourceType(sourcePath);
            if (sourceType == FieldDataType.Object)
            {
                return SourceFlattened
                    .Where(x => x.Path.StartsWith(sourcePath + ".", StringComparison.Ordinal))
                    .OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase);
            }

            if (sourceType == FieldDataType.Array)
            {
                return SourceFlattened
                    .Where(x => x.Path.StartsWith(sourcePath + "[]", StringComparison.Ordinal))
                    .OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase);
            }

            return Enumerable.Empty<FlatField>();
        }

        public string GetIndentedLabel(string path, FieldDataType type, int depth)
        {
            return new string(' ', depth * 2) + path + $" ({type})";
        }

        private FieldDataType? GetSourceType(string? sourcePath)
        {
            return string.IsNullOrWhiteSpace(sourcePath)
                ? null
                : SourceFlattened.FirstOrDefault(x => x.Path == sourcePath).Type;
        }

        private void OnChanged()
        {
            Templates = syncContext.BasicTemplatesFull.ToList();
            JoinedTemplates = syncContext.JoinedTemplates.ToList();

            if (SourceId.HasValue)
            {
                SourceTemplate = Templates.FirstOrDefault(x => x.Id == SourceId.Value);
            }

            if (TargetId.HasValue)
            {
                TargetTemplate = Templates.FirstOrDefault(x => x.Id == TargetId.Value);
            }

            SourceFlattened = FlattenTemplate(SourceTemplate);
            TargetFlattened = FlattenTemplate(TargetTemplate);

            HashSet<string> targetSet = new HashSet<string>(TargetFlattened.Select(f => f.Path));
            foreach (string key in mapByPath.Keys.ToList())
            {
                if (!targetSet.Contains(key)) mapByPath.Remove(key);
            }

            foreach (FlatField target in TargetFlattened)
            {
                if (!mapByPath.ContainsKey(target.Path)) mapByPath[target.Path] = null;
            }

            Changed?.Invoke();
        }

        private static List<FlatField> FlattenTemplate(BasicTemplateDto? basicTemplate)
        {
            List<FlatField> flatFields = new List<FlatField>();
            if (basicTemplate is null) return flatFields;

            HashSet<int> visitingIds = new HashSet<int>();

            void Walk(TemplateFieldDto templateField, string prefix)
            {
                if (templateField.Id == 0 || !visitingIds.Add(templateField.Id)) return;

                try
                {
                    string baseName = string.IsNullOrEmpty(prefix)
                        ? templateField.Name
                        : $"{prefix}.{templateField.Name}";

                    if (templateField.Type == FieldDataType.Object)
                    {
                        flatFields.Add(new FlatField(baseName, FieldDataType.Object));

                        foreach (TemplateFieldDto child in templateField.Children ?? Enumerable.Empty<TemplateFieldDto>())
                        {
                            if (child.Id != templateField.Id)
                            {
                                Walk(child, baseName);
                            }
                        }
                    }
                    else if (templateField.Type == FieldDataType.Array)
                    {
                        string arrPath = $"{baseName}[]";
                        flatFields.Add(new FlatField(arrPath, FieldDataType.Array));

                        if (templateField.ItemType == FieldDataType.Object)
                        {
                            foreach (TemplateFieldDto child in templateField.ChildrenItems ?? Enumerable.Empty<TemplateFieldDto>())
                            {
                                if (child.Id != templateField.Id)
                                {
                                    Walk(child, arrPath);
                                }
                            }
                        }
                    }
                    else
                    {
                        flatFields.Add(new FlatField(baseName, templateField.Type));
                    }
                }
                finally
                {
                    visitingIds.Remove(templateField.Id);
                }
            }

            foreach (TemplateFieldDto root in basicTemplate.Fields ?? Enumerable.Empty<TemplateFieldDto>())
            {
                Walk(root, "");
            }

            return flatFields
                .OrderBy(flatField => flatField.Type == FieldDataType.Object || flatField.Type == FieldDataType.Array ? 1 : 0)
                .ThenBy(flatField => flatField.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
