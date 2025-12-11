using OmniPort.Core.Interfaces;
using System.Collections.Concurrent;

namespace OmniPort.UI.Presentation.Services
{
    public class InMemorySourceFingerprintStore : ISourceFingerprintStore
    {
        private readonly ConcurrentDictionary<string, string> map;

        public InMemorySourceFingerprintStore()
        {
            map = new(StringComparer.Ordinal);
        }

        private static string Key(string url, int? mappingTemplateId)
        {
            string key = $"{url}|{mappingTemplateId?.ToString() ?? "-"}";
            return key;
        }


        public Task<string?> GetHash(string url, int? mappingTemplateId = null, CancellationToken ct = default)
        {
            map.TryGetValue(Key(url, mappingTemplateId), out string? hash);
            return Task.FromResult(hash);
        }

        public Task SetHash(string url, string sha256Hex, int? mappingTemplateId = null, CancellationToken ct = default)
        {
            map[Key(url, mappingTemplateId)] = sha256Hex;
            return Task.CompletedTask;
        }

        public Task Remove(string url, int? mappingTemplateId = null, CancellationToken ct = default)
        {
            map.TryRemove(Key(url, mappingTemplateId), out _);
            return Task.CompletedTask;
        }
    }
}
