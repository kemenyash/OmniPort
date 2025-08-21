using OmniPort.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
