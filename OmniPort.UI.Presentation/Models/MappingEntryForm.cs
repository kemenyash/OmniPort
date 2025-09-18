using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Models
{
    public class MappingEntryForm
    {
        [Required] public string TargetPath { get; set; } = string.Empty;
        public string? SourcePath { get; set; }
    }
}
