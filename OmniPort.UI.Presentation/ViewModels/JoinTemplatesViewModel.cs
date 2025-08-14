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
        private readonly ITemplateManager _service;

        public JoinTemplatesViewModel(ITemplateManager service)
        {
            _service = service;
        }

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
            var summaries = await _service.GetBasicTemplatesSummaryAsync();
            var ids = summaries.Select(x => x.Id).ToList();

            Templates = new List<BasicTemplateDto>();
            foreach (var id in ids)
            {
                var dto = await _service.GetBasicTemplateAsync(id);
                if (dto != null) Templates.Add(dto);
            }

            JoinedTemplates = (await _service.GetJoinedTemplatesAsync()).ToList();
        }

        public async Task SetSourceTemplateAsync(int id)
        {
            SourceId = id;
            SourceTemplate = await _service.GetBasicTemplateAsync(id);
        }

        public async Task SetTargetTemplateAsync(int id)
        {
            TargetId = id;
            TargetTemplate = await _service.GetBasicTemplateAsync(id);

            _targetToSource.Clear();
            foreach (var tf in TargetTemplate?.Fields ?? Enumerable.Empty<TemplateFieldDto>())
                _targetToSource[tf.Id] = null;
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

            var cmd = new CreateMappingTemplateDto(
                name,
                SourceId.Value,
                TargetId.Value,
                new Dictionary<int, int?>(_targetToSource)
            );

            await _service.CreateMappingTemplateAsync(cmd);
            JoinedTemplates = (await _service.GetJoinedTemplatesAsync()).ToList();
        }

        public async Task DeleteJoinTemplateAsync(int mappingTemplateId)
        {
            await _service.DeleteMappingTemplateAsync(mappingTemplateId);
            JoinedTemplates = (await _service.GetJoinedTemplatesAsync()).ToList();
        }
    }
}
