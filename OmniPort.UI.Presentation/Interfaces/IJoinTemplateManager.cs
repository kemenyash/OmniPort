using OmniPort.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Interfaces
{
    public interface IJoinTemplateManager
    {
        Task<List<JoinedTemplateSummary>> GetJoinedTemplatesAsync();
        Task SaveMappingAsync(ImportProfile profile, int sourceTemplateId);
        Task DeleteJoinTemplateAsync(int id);
    }
}
