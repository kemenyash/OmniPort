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
        public string SourceTemplate { get; set; }
        public string TargetTemplate { get; set; } 

        public SourceType OutputFormat { get; set; }

        public string FullName
        {
            get
            {
                return $"{SourceTemplate} → {TargetTemplate}";
            }
        }
    }
}
