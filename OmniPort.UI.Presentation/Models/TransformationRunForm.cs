using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Models
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
