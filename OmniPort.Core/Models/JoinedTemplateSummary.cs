using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Models
{
    public class JoinedTemplateSummary
    {
        public int Id { get; set; }
        public string SourceTemplate { get; set; } = string.Empty;
        public string TargetTemplate { get; set; } = string.Empty;
    }
}
