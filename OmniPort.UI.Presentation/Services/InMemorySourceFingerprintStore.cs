using OmniPort.Core.Interfaces;
using System.Collections.Concurrent;
using System.Threading;

namespace OmniPort.UI.Presentation.Services
{
    public class InMemorySourceFingerprintStore : ISourceFingerprintStore
    {
        private readonly ConcurrentDictionary<string, string> map = new(StringComparer.Ordinal);

        private static string Key(string url, int? mappingTemplateId)
            => $"{url}|{mappingTemplateId?.ToString() ?? "-"}";

        public Task<string?> GetHashAsync(string url, int? mappingTemplateId = null, CancellationToken ct = default)
        {
            map.TryGetValue(Key(url, mappingTemplateId), out var hash);
            return Task.FromResult(hash);
        }

        public Task SetHashAsync(string url, string sha256Hex, int? mappingTemplateId = null, CancellationToken ct = default)
        {
            map[Key(url, mappingTemplateId)] = sha256Hex;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string url, int? mappingTemplateId = null, CancellationToken ct = default)
        {
            map.TryRemove(Key(url, mappingTemplateId), out _);
            return Task.CompletedTask;
        }
    }
}
