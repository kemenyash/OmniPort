using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Models
{
    public class MappingTemplateForm
    {
        public int? Id { get; set; } // null => Create

        [Required] public string Name { get; set; } = string.Empty;

        [Required] public int SourceTemplateId { get; set; }
        [Required] public int TargetTemplateId { get; set; }

        public Dictionary<int, int?> TargetToSource { get; set; } = new();
    }
}
