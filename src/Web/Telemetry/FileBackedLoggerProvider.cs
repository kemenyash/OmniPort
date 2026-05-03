using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Web.Telemetry
{
    public sealed class FileBackedLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly IApplicationLogStore logStore;
        private IExternalScopeProvider scopeProvider;

        public FileBackedLoggerProvider(IApplicationLogStore logStore)
        {
            this.logStore = logStore;
            scopeProvider = new LoggerExternalScopeProvider();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileBackedLogger(categoryName, logStore, () => scopeProvider);
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            this.scopeProvider = scopeProvider;
        }

        public void Dispose()
        {
        }

        private sealed class FileBackedLogger : ILogger
        {
            private readonly string categoryName;
            private readonly IApplicationLogStore logStore;
            private readonly Func<IExternalScopeProvider> scopeProviderAccessor;

            public FileBackedLogger(
                string categoryName,
                IApplicationLogStore logStore,
                Func<IExternalScopeProvider> scopeProviderAccessor)
            {
                this.categoryName = categoryName;
                this.logStore = logStore;
                this.scopeProviderAccessor = scopeProviderAccessor;
            }

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
            {
                return scopeProviderAccessor().Push(state);
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return logStore.ShouldCapture(logLevel);
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var activity = Activity.Current;
                var scopes = CaptureScopes();

                logStore.Add(new ApplicationLogEntry(
                    Id: 0,
                    Timestamp: DateTimeOffset.UtcNow,
                    Level: logLevel,
                    Category: categoryName,
                    EventId: eventId.Id,
                    EventName: eventId.Name,
                    Message: formatter(state, exception),
                    Exception: exception?.ToString(),
                    TraceId: activity?.TraceId.ToString(),
                    SpanId: activity?.SpanId.ToString(),
                    Scopes: scopes));
            }

            private IReadOnlyDictionary<string, string> CaptureScopes()
            {
                var scopes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                scopeProviderAccessor().ForEachScope((scope, state) =>
                {
                    if (scope is IEnumerable<KeyValuePair<string, object?>> values)
                    {
                        foreach (var value in values)
                        {
                            if (value.Key != "{OriginalFormat}" && value.Value is not null)
                            {
                                state[value.Key] = value.Value.ToString() ?? string.Empty;
                            }
                        }

                        return;
                    }

                    if (scope is not null)
                    {
                        state[$"scope{state.Count + 1}"] = scope.ToString() ?? string.Empty;
                    }
                }, scopes);

                return scopes;
            }
        }
    }
}
