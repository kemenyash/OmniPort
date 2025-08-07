using OmniPort.Core.Models;
using OmniPort.UI.Presentation.Interfaces;
using OmniPort.UI.Presentation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.ViewModels
{
    public class TransformationViewModel
    {
        private readonly ITransformationManager _manager;
        private readonly ITransformationExecutionService _executor;

        public TransformFormModel FormModel { get; } = new();
        public object UploadedFile { get; private set; }

        public List<JoinedTemplateSummary> JoinedTemplates { get; private set; } = new();
        public List<ConversionHistory> FileConversions { get; private set; } = new();
        public List<UrlConversionHistory> UrlConversions { get; private set; } = new();
        public List<WatchedUrl> WatchedUrls { get; private set; } = new();

        public TransformationViewModel(ITransformationManager manager, ITransformationExecutionService executor)
        {
            _manager = manager;
            _executor = executor;
        }

        public async Task InitAsync()
        {
            JoinedTemplates = await _manager.GetJoinedTemplatesAsync();
            FileConversions = await _manager.GetFileConversionHistoryAsync();
            UrlConversions = await _manager.GetUrlConversionHistoryAsync();
            WatchedUrls = await _manager.GetWatchedUrlsAsync();
        }

        public void SetUploadedFile(object file)
        {
            UploadedFile = file;
        }

        public async Task RunUploadAsync()
        {
            if (UploadedFile is null || FormModel.SelectedTemplateId == 0)
                return;

            var outputLink = await _executor.TransformUploadedFileAsync(FormModel.SelectedTemplateId, UploadedFile, FormModel.OutputExtension);

            var record = new ConversionHistory
            {
                FileName = "",
                ConvertedAt = DateTime.UtcNow,
                TemplateName = GetTemplateName(),
                OutputLink = outputLink
            };

            await _manager.AddFileConversionAsync(record);
            FileConversions = await _manager.GetFileConversionHistoryAsync();
        }

        public async Task RunUrlAsync()
        {
            if (string.IsNullOrWhiteSpace(FormModel.FileUrl) || FormModel.SelectedTemplateId == 0)
                return;

            var outputLink = await _executor.TransformFromUrlAsync(FormModel.SelectedTemplateId, FormModel.FileUrl, FormModel.OutputExtension);

            var record = new UrlConversionHistory
            {
                InputUrl = FormModel.FileUrl,
                ConvertedAt = DateTime.UtcNow,
                TemplateName = GetTemplateName(),
                OutputLink = outputLink,
                TemplateMapId = FormModel.SelectedTemplateId
            };

            var watch = new WatchedUrl
            {
                Url = FormModel.FileUrl,
                IntervalMinutes = FormModel.IntervalMinutes
            };

            await _manager.AddUrlConversionAsync(record);
            await _manager.AddWatchedUrlAsync(watch);

            UrlConversions = await _manager.GetUrlConversionHistoryAsync();
            WatchedUrls = await _manager.GetWatchedUrlsAsync();
        }

        private string GetTemplateName()
        {
            return JoinedTemplates.FirstOrDefault(t => t.Id == FormModel.SelectedTemplateId)?.ToString() ?? "Unknown";
        }
    }

}
