using OmniPort.Core.Enums;
using OmniPort.Core.Models;

namespace OmniPort.Core.Interfaces
{
    public interface ITransformationManager
    {
        Task<(ImportProfile Profile, SourceType ImportSourceType, SourceType ConvertSourceType)> GetImportProfileForJoinAsync(int mappingTemplateId);
    }
}
