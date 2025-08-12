using OmniPort.Core.Interfaces;
using OmniPort.Core.Models;
using OmniPort.UI.Presentation.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Services
{
    public class JoinTemplateManager : IJoinTemplateManager
    {
        private readonly ICRUDService crudService;

        public JoinTemplateManager(ICRUDService crudService)
        {
            this.crudService = crudService;
        }

        public async Task<List<JoinedTemplateSummary>> GetJoinedTemplatesAsync()
            => await crudService.GetJoinedTemplatesAsync();

        public async Task SaveMappingAsync(ImportProfile profile, int sourceTemplateId)
            => await crudService.SaveMappingAsync(profile, sourceTemplateId);

        public async Task DeleteJoinTemplateAsync(int id)
            => await crudService.DeleteJoinTemplateAsync(id);
    }
}
