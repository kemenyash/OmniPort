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

        public FieldDataType? ItemType { get; set; }

        public List<TemplateField> Children { get; set; } = new();

        public List<TemplateField> ChildrenItems { get; set; } = new();
    }
}
