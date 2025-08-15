using OmniPort.Core.Models;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Interfaces
{

    public interface ITransformationManager
    {
        Task<(ImportProfile Profile, SourceType ImportSourceType, SourceType ConvertSourceType)>
            GetImportProfileForJoinAsync(int mappingTemplateId);
    }
}
