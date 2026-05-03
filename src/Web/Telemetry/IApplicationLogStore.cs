using Microsoft.Extensions.Logging;

namespace Web.Telemetry
{
    public interface IApplicationLogStore
    {
        event Action? Changed;

        LogLevel MinimumLevel { get; }

        IReadOnlyList<ApplicationLogEntry> GetRecent(int count = 300);

        ApplicationLogPage GetPage(int pageIndex, int pageSize);

        bool ShouldCapture(LogLevel logLevel);

        void Add(ApplicationLogEntry entry);

        void Clear();

        void SetMinimumLevel(LogLevel minimumLevel);
    }
}
