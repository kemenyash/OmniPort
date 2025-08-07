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
    public class TransformationManager : ITransformationManager
    {
        private readonly ICRUDService crudService;

        public TransformationManager(ICRUDService crudService)
        {
            this.crudService = crudService;
        }

        public async Task<List<ConversionHistory>> GetFileConversionHistoryAsync()
            => await crudService.GetFileConversionHistoryAsync();

        public async Task<List<UrlConversionHistory>> GetUrlConversionHistoryAsync()
            => await crudService.GetUrlConversionHistoryAsync();

        public async Task<List<WatchedUrl>> GetWatchedUrlsAsync()
            => await crudService.GetWatchedUrlsAsync();

        public async Task AddFileConversionAsync(ConversionHistory record)
            => await crudService.AddFileConversionAsync(record);

        public async Task AddUrlConversionAsync(UrlConversionHistory record)
            => await crudService.AddUrlConversionAsync(record);

        public async Task AddWatchedUrlAsync(WatchedUrl watch)
            => await crudService.AddWatchedUrlAsync(watch);

        public Task<List<JoinedTemplateSummary>> GetJoinedTemplatesAsync() => 
            crudService.GetJoinedTemplatesAsync();
    }
}
