using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Models
{
    public class MappingTemplateForm
    {
        public int? Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public int SourceTemplateId { get; set; }
        [Required] public int TargetTemplateId { get; set; }

        public List<MappingEntryForm> Mappings { get; set; } = new();
    }
}
