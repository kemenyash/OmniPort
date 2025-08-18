using OmniPort.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Models
{
    public class TemplateField
    {
        public string Name { get; set; } = string.Empty;
        public FieldDataType Type { get; set; }
    }

}
