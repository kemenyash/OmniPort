using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Models
{
    public class WatchedUrl
    {
        public string Url { get; set; } = default!;
        public int IntervalMinutes { get; set; }
    }
}
