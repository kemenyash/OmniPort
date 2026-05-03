using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Web.Telemetry
{
    public sealed class ApplicationLogStore : IApplicationLogStore
    {
        private readonly object fileGate;
        private readonly string filePath;
        private long nextId;
        private LogLevel minimumLevel;

        public event Action? Changed;

        public LogLevel MinimumLevel => minimumLevel;

        public ApplicationLogStore(
            IOptions<ApplicationLogStoreOptions> options,
            IHostEnvironment hostEnvironment)
        {
            fileGate = new object();
            minimumLevel = options.Value.MinimumLevel;
            filePath = ResolveFilePath(hostEnvironment.ContentRootPath, options.Value.FilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            if (File.Exists(filePath))
            {
                nextId = ReadLastId(filePath);
            }
        }

        public IReadOnlyList<ApplicationLogEntry> GetRecent(int count = 300)
        {
            return GetPage(0, count).Entries;
        }

        public ApplicationLogPage GetPage(int pageIndex, int pageSize)
        {
            lock (fileGate)
            {
                if (!File.Exists(filePath))
                {
                    return new ApplicationLogPage(
                        new List<ApplicationLogEntry>(),
                        PageIndex: 0,
                        PageSize: Math.Max(1, pageSize),
                        TotalCount: 0,
                        TotalPages: 0);
                }

                var safePageSize = Math.Max(1, pageSize);
                var totalCount = CountLogLines(filePath);
                var totalPages = totalCount == 0
                    ? 0
                    : (int)Math.Ceiling(totalCount / (double)safePageSize);
                var safePageIndex = totalPages == 0
                    ? 0
                    : Math.Clamp(pageIndex, 0, totalPages - 1);

                if (totalCount == 0)
                {
                    return new ApplicationLogPage(
                        new List<ApplicationLogEntry>(),
                        safePageIndex,
                        safePageSize,
                        totalCount,
                        totalPages);
                }

                var newestEntriesToSkip = safePageIndex * safePageSize;
                var takeCount = Math.Min(safePageSize, totalCount - newestEntriesToSkip);
                var startIndex = totalCount - newestEntriesToSkip - takeCount;
                var endIndexExclusive = startIndex + takeCount;
                var selectedLines = new List<string>(takeCount);
                var currentIndex = 0;

                foreach (var line in File.ReadLines(filePath))
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    if (currentIndex >= startIndex && currentIndex < endIndexExclusive)
                    {
                        selectedLines.Add(line);
                    }

                    currentIndex++;
                    if (currentIndex >= endIndexExclusive)
                    {
                        break;
                    }
                }

                var entries = selectedLines
                    .AsEnumerable()
                    .Reverse()
                    .Select(TryDeserialize)
                    .Where(entry => entry is not null)
                    .Cast<ApplicationLogEntry>()
                    .ToList();

                return new ApplicationLogPage(
                    entries,
                    safePageIndex,
                    safePageSize,
                    totalCount,
                    totalPages);
            }
        }

        public bool ShouldCapture(LogLevel logLevel)
        {
            return logLevel != LogLevel.None && logLevel >= minimumLevel;
        }

        public void Add(ApplicationLogEntry entry)
        {
            if (!ShouldCapture(entry.Level))
            {
                return;
            }

            var entryWithId = entry with { Id = Interlocked.Increment(ref nextId) };
            var line = JsonSerializer.Serialize(entryWithId);

            lock (fileGate)
            {
                File.AppendAllText(filePath, line + Environment.NewLine);
            }
            Changed?.Invoke();
        }

        public void Clear()
        {
            lock (fileGate)
            {
                File.WriteAllText(filePath, string.Empty);
            }

            Changed?.Invoke();
        }

        public void SetMinimumLevel(LogLevel minimumLevel)
        {
            this.minimumLevel = minimumLevel;
            Changed?.Invoke();
        }

        private static string ResolveFilePath(string contentRootPath, string configuredPath)
        {
            if (Path.IsPathFullyQualified(configuredPath))
            {
                return configuredPath;
            }

            return Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
        }

        private static ApplicationLogEntry? TryDeserialize(string line)
        {
            try
            {
                return JsonSerializer.Deserialize<ApplicationLogEntry>(line);
            }
            catch
            {
                return null;
            }
        }

        private static long ReadLastId(string path)
        {
            string? lastLine = null;
            foreach (var line in File.ReadLines(path))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lastLine = line;
                }
            }

            return lastLine is null ? 0 : TryDeserialize(lastLine)?.Id ?? 0;
        }

        private static int CountLogLines(string path)
        {
            var count = 0;
            foreach (var line in File.ReadLines(path))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
