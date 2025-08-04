using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Models
{
    public class ImportProfile
    {
        public string ProfileName { get; set; } = string.Empty;
        public ImportTemplate Template { get; set; } = new();
        public List<FieldMapping> Mappings { get; set; } = new();
    }
}
