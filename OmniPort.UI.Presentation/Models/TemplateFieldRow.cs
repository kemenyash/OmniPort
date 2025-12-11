using OmniPort.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace OmniPort.UI.Presentation.Models
{
    public class TemplateFieldRow
    {
        public int? Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public FieldDataType Type { get; set; }
        public FieldDataType? ItemType { get; set; }
        public List<TemplateFieldRow> Children { get; set; } = new();
        public List<TemplateFieldRow> ChildrenItems { get; set; } = new();
    }
}
