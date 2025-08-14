using OmniPort.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Models
{
    public class TemplateFieldRow
    {
        public int? Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public FieldDataType Type { get; set; }
    }
}
