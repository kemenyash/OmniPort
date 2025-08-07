using OmniPort.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.Interfaces
{
    public interface ITransformationManager
    {
        Task<List<ConversionHistory>> GetFileConversionHistoryAsync();
        Task<List<UrlConversionHistory>> GetUrlConversionHistoryAsync();
        Task<List<WatchedUrl>> GetWatchedUrlsAsync();
        Task<List<JoinedTemplateSummary>> GetJoinedTemplatesAsync();

        Task AddFileConversionAsync(ConversionHistory record);
        Task AddUrlConversionAsync(UrlConversionHistory record);
        Task AddWatchedUrlAsync(WatchedUrl watch);
    }

}
