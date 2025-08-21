using OmniPort.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Models
{
    public class ImportTemplate
    {
        public int Id { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public List<string> Columns => Fields.Select(f => f.Name).ToList();
        public List<TemplateField> Fields { get; set; } = new();
        public SourceType SourceType { get; set; }
    }
}
