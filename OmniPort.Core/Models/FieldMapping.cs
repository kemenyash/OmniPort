using OmniPort.Core.Enums;

namespace OmniPort.Core.Models
{
    public class FieldMapping
    {
        public string SourceField { get; set; } = string.Empty;
        public string TargetField { get; set; } = string.Empty;
        public FieldDataType TargetType { get; set; }
        public string? DateFormat { get; set; }
        public Func<object?, object?>? CustomTransform { get; set; }
    }
}
