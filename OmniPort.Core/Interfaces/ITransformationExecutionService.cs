using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Interfaces
{
    public interface ITransformationExecutionService
    {
        Task<string> TransformUploadedFileAsync(int templateId, object file, string outputExtension);
        Task<string> TransformFromUrlAsync(int templateId, string url, string outputExtension);
        Task<string> SaveTransformedAsync(IEnumerable<IDictionary<string, object?>> rows, string outputExtension, string baseName);

    }
}
