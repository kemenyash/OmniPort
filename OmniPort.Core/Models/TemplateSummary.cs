using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Models
{
    public class TemplateSummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public SourceType SourceType { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }
}
