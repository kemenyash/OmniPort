using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Models
{
    public class TransformFormModel
    {
        public int SelectedTemplateId { get; set; }
        public string? FileUrl { get; set; }
        public int IntervalMinutes { get; set; }
    }

}
