using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Models
{
    public class UrlConversionHistory
    {
        public int TemplateMapId { get; set; }
        public string InputUrl { get; set; } = default!;
        public DateTime ConvertedAt { get; set; }
        public string OutputLink { get; set; } = default!;
        public string TemplateName { get; set; } = default!;
    }
}
