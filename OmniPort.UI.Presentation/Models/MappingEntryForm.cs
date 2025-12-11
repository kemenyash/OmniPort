using System.ComponentModel.DataAnnotations;

namespace OmniPort.UI.Presentation.Models
{
    public class MappingEntryForm
    {
        [Required] public string TargetPath { get; set; } = string.Empty;
        public string? SourcePath { get; set; }
    }
}
