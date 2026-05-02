using System.ComponentModel.DataAnnotations;

namespace Presentation.Models
{
    public class TransformationRunForm
    {
        [Required] public int SelectedMappingTemplateId { get; set; }

        // Upload mode
        public string? UploadedFileName { get; set; }

        // URL mode
        [Url] public string? FileUrl { get; set; }
        [Range(1, 24 * 60)] public int? IntervalMinutes { get; set; }
    }
}
