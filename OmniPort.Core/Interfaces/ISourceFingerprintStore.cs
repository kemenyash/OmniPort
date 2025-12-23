namespace OmniPort.Core.Interfaces
{
    public interface ISourceFingerprintStore
    {
        Task<string?> GetHash(string url, int? mappingTemplateId = null);
        Task SetHash(string url, string sha256Hex, int? mappingTemplateId = null);
        Task Remove(string url, int? mappingTemplateId = null);
    }

}
