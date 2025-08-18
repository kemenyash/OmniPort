using OmniPort.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Models
{
    public class FieldMapping
    {
        public string SourceField { get; set; } = string.Empty;
        public string TargetField { get; set; } = string.Empty;
        public FieldDataType TargetType { get; set; }

        // Optional: format for DateTime
        public string? DateFormat { get; set; }

        // Optional: raw transformation expression or delegate (advanced)
        public Func<object?, object?>? CustomTransform { get; set; }
    }
}
