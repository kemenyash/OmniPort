namespace OmniPort.Core.Interfaces
{
    public interface ISourceFingerprintStore
    {
        Task<string?> GetHash(string url, int? mappingTemplateId = null, CancellationToken ct = default);
        Task SetHash(string url, string sha256Hex, int? mappingTemplateId = null, CancellationToken ct = default);
        Task Remove(string url, int? mappingTemplateId = null, CancellationToken ct = default);
    }

}
