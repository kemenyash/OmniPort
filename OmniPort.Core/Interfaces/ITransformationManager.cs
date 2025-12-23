using OmniPort.Core.Enums;
using OmniPort.Core.Models;
using OmniPort.Core.Records;

namespace OmniPort.Core.Interfaces
{
    public interface ITransformationManager
    {
        Task<ImportProfileForJoinResultDto> GetImportProfileForJoin(int mappingTemplateId);
    }
}
