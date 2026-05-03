using Microsoft.Extensions.Logging;

namespace Web.Telemetry
{
    public sealed class ApplicationLogStoreOptions
    {
        public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

        public string FilePath { get; set; } = Path.Combine("logs", "omniport-app.jsonl");
    }
}
