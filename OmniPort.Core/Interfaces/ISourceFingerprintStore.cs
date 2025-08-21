using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Interfaces
{
    public interface ISourceFingerprintStore
    {
        Task<string?> GetHashAsync(string url, int? mappingTemplateId = null, CancellationToken ct = default);
        Task SetHashAsync(string url, string sha256Hex, int? mappingTemplateId = null, CancellationToken ct = default);
        Task RemoveAsync(string url, int? mappingTemplateId = null, CancellationToken ct = default);
    }

}
