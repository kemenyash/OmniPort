using OmniPort.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace OmniPort.UI.Presentation.Models
{
    public class TemplateEditForm
    {
        public int? Id { get; set; } // null => Create

        [Required] public string Name { get; set; } = string.Empty;
        [Required] public SourceType SourceType { get; set; }

        [MinLength(1)]
        public List<TemplateFieldRow> Fields { get; set; } = new();
    }
}
