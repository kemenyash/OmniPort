using OmniPort.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Services
{
    public class UploadLimits
    {
        public long MaxUploadBytes { get; set; } = 200L * 1024 * 1024;
        public long InMemoryThresholdBytes { get; set; } = 32L * 1024 * 1024;
        public Dictionary<string, long> PerType { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public long GetMaxFor(SourceType type)
        {
            var key = type switch
            {
                SourceType.Excel => "Excel",
                SourceType.CSV => "Csv",
                SourceType.JSON => "Json",
                SourceType.XML => "Xml",
                _ => "Default"
            };
            return PerType.TryGetValue(key, out var v) ? v : MaxUploadBytes;
        }
    }

}
