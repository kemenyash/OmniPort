using System.ComponentModel.DataAnnotations;

namespace Presentation.Models
{
    public class MappingEntryForm
    {
        [Required] public string TargetPath { get; set; } = string.Empty;
        public string? SourcePath { get; set; }
    }
}
