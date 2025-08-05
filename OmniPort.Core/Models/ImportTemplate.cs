using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Models
{
    public class ImportTemplate
    {
        public string TemplateName { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new();
        public SourceType SourceType { get; set; }
    }
}
