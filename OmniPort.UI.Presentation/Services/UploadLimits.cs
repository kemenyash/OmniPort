using OmniPort.Core.Enums;

namespace OmniPort.UI.Presentation.Services
{
    public class UploadLimits
    {
        public long MaxUploadBytes { get; set; }
        public long InMemoryThresholdBytes { get; set; }
        public Dictionary<string, long> PerType { get; set; }

        public UploadLimits()
        {
            MaxUploadBytes = 200L * 1024 * 1024;
            InMemoryThresholdBytes = 32L * 1024 * 1024;
            PerType = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        }

        public long GetMaxFor(SourceType sourceType)
        {
            var sourceTypeKey = ResolveSourceTypeKey(sourceType);

            if (PerType.TryGetValue(sourceTypeKey, out var maxBytesForType))
            {
                return maxBytesForType;
            }

            return MaxUploadBytes;
        }

        private static string ResolveSourceTypeKey(SourceType sourceType)
        {
            switch (sourceType)
            {
                case SourceType.Excel:
                    {
                        return "Excel";
                    }
                case SourceType.CSV:
                    {
                        return "Csv";
                    }
                case SourceType.JSON:
                    {
                        return "Json";
                    }
                case SourceType.XML:
                    {
                        return "Xml";
                    }
                default:
                    {
                        return "Default";
                    }
            }
        }
    }
}
