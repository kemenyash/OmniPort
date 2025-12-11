using Microsoft.AspNetCore.Components;
using OmniPort.Core.Enums;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.ViewModels
{
    public class JoinTemplatesViewModel
    {
        private readonly IAppSyncContext syncContext;
        private readonly Dictionary<string, string?> mapByPath;

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

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            if (!syncContext.Templates.Any())
            {
                await syncContext.Initialize();
            }

            Templates = syncContext.BasicTemplatesFull.ToList();
            JoinedTemplates = syncContext.JoinedTemplates.ToList();

            syncContext.Changed += OnChanged;
        }

        public async Task OnSourceChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var id))
            {
                await SetSourceTemplate(id);
            }
        }

        public async Task OnTargetChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var id))
            {
                await SetTargetTemplate(id);
            }
        }

        public async Task SetSourceTemplate(int id)
        {
            SourceId = id;
            SourceTemplate = Templates.FirstOrDefault(x => x.Id == id);
            SourceFlattened = FlattenTemplate(SourceTemplate);
        }

        public async Task SetTargetTemplate(int id)
        {
            TargetId = id;
            TargetTemplate = Templates.FirstOrDefault(x => x.Id == id);
            TargetFlattened = FlattenTemplate(TargetTemplate);

            mapByPath.Clear();
            foreach (var target in TargetFlattened)
            {
                mapByPath[target.Path] = null;
            }
        }

        public string? GetMappedValue(string targetPath)
        {
            return mapByPath.TryGetValue(targetPath, out var src) ? src : null;
        }

        public void MapField(string targetPath, string? sourcePath)
        {
            if (!mapByPath.ContainsKey(targetPath)) return;
            mapByPath[targetPath] = string.IsNullOrWhiteSpace(sourcePath) ? null : sourcePath;
        }

        public async Task SaveMapping()
        {
            if (!CanSave || TargetId is null || SourceId is null) return;

            var name = $"{SourceTemplate!.Name} → {TargetTemplate!.Name}";

            var mappings = mapByPath
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .Select(kv => new MappingEntryDto(TargetPath: kv.Key, SourcePath: kv.Value!))
                .ToList();

            var creatingMappingTemplate = new CreateMappingTemplateDto(
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
            var result = (!flatField.Path.Contains('.') && !flatField.Path.Contains("[]"))
               || flatField.Type == FieldDataType.Object
               || flatField.Type == FieldDataType.Array;
            return result;
        }


        public string? GetTopSelectValue(string? fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return null;
            var dot = fullPath.IndexOf('.');
            return dot >= 0 ? fullPath[..dot] : fullPath;
        }

        public int GetDepth(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return 0;

            var dotDepth = path.Count(c => c == '.');
            var arrDepth = path.Split("[]").Length - 1;

            return dotDepth + arrDepth;
        }

        private FieldDataType? GetSourceType(string? sourcePath)
        {
            return string.IsNullOrWhiteSpace(sourcePath)
               ? null
               : SourceFlattened.FirstOrDefault(x => x.Path == sourcePath).Type;
        }

        public bool HasDescendants(string sourcePath)
        {
            var sourceType = GetSourceType(sourcePath);
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
            var sourceType = GetSourceType(sourcePath);
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
            var result = new string(' ', depth * 2) + path + $" ({type})";
            return result;
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

            var targetSet = new HashSet<string>(TargetFlattened.Select(f => f.Path));
            foreach (var key in mapByPath.Keys.ToList())
            {
                if (!targetSet.Contains(key)) mapByPath.Remove(key);
            }

            foreach (var target in TargetFlattened)
            {
                if (!mapByPath.ContainsKey(target.Path)) mapByPath[target.Path] = null;
            }
        }

        private static List<FlatField> FlattenTemplate(BasicTemplateDto? basicTemplate)
        {
            var flatFields = new List<FlatField>();
            if (basicTemplate is null) return flatFields;

            var visitingIds = new HashSet<int>();

            void Walk(TemplateFieldDto templateField, string prefix, bool isArrayItem)
            {
                if (templateField.Id == 0 || !visitingIds.Add(templateField.Id)) return;

                try
                {
                    var baseName = string.IsNullOrEmpty(prefix)
                        ? templateField.Name
                        : $"{prefix}.{templateField.Name}";

                    if (templateField.Type == FieldDataType.Object)
                    {
                        flatFields.Add(new FlatField(baseName, FieldDataType.Object));

                        foreach (var child in templateField.Children ?? Enumerable.Empty<TemplateFieldDto>())
                        {
                            if (child.Id != templateField.Id)
                            {
                                Walk(child, baseName, false);
                            }
                        }
                    }
                    else if (templateField.Type == FieldDataType.Array)
                    {
                        var arrPath = $"{baseName}[]";
                        flatFields.Add(new FlatField(arrPath, FieldDataType.Array));

                        if (templateField.ItemType == FieldDataType.Object)
                        {
                            foreach (var child in templateField.ChildrenItems ?? Enumerable.Empty<TemplateFieldDto>())
                            {
                                if (child.Id != templateField.Id)
                                {
                                    Walk(child, arrPath, true);
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

            foreach (var root in basicTemplate.Fields ?? Enumerable.Empty<TemplateFieldDto>())
            {
                Walk(root, "", false);
            }

            return flatFields
                .OrderBy(flatField => flatField.Type == FieldDataType.Object || flatField.Type == FieldDataType.Array ? 1 : 0)
                .ThenBy(flatField => flatField.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
