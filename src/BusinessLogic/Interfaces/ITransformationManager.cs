using BusinessLogic.Enums;
using BusinessLogic.Models;
using BusinessLogic.Records;

namespace BusinessLogic.Interfaces
{
    public interface ITransformationManager
    {
        Task<ImportProfileForJoinResultDto> GetImportProfileForJoin(int mappingTemplateId);
    }
}
