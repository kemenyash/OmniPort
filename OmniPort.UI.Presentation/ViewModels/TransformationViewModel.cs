using Microsoft.AspNetCore.Components.Forms;
using OmniPort.Core.Models;
using OmniPort.Core.Records;
using OmniPort.Core.Utilities; 
using OmniPort.UI.Presentation.Interfaces;

namespace OmniPort.UI.Presentation.ViewModels
{
    public class TransformationViewModel
    {
        private readonly ITemplateManager templateManager;
        private readonly ITransformationExecutionService executor;

        public TransformationViewModel(ITemplateManager templateManager, ITransformationExecutionService executor)
        {
            this.templateManager = templateManager;
            this.executor = executor;
        }

        public TransformationRunForm FormModel { get; private set; } = new();

        public List<JoinedTemplateSummaryDto> JoinedTemplates { get; private set; } = new();

        public List<FileConversionHistoryDto> FileConversions { get; private set; } = new();
        public List<UrlConversionHistoryDto> UrlConversions { get; private set; } = new();
        public List<WatchedUrlDto> WatchedUrls { get; private set; } = new();

        private object? _uploadObject;
        private string? _uploadedFileName;

        public async Task InitAsync()
        {
            JoinedTemplates = (await templateManager.GetJoinedTemplatesAsync()).ToList();
            FileConversions = (await templateManager.GetFileConversionHistoryAsync()).OrderByDescending(x => x.ConvertedAt).ToList();
            UrlConversions = (await templateManager.GetUrlConversionHistoryAsync()).OrderByDescending(x => x.ConvertedAt).ToList();
            WatchedUrls = (await templateManager.GetWatchedUrlsAsync()).ToList();

            if (FormModel.SelectedMappingTemplateId == 0 && JoinedTemplates.Any())
                FormModel.SelectedMappingTemplateId = JoinedTemplates.First().Id;
        }

        public void SetUploadedFile(string fileName, Func<Task<Stream>> openStream)
        {
            _uploadedFileName = fileName;
            _uploadObject = new LazyStream(openStream);
        }

        public void SetUploadedFile(IBrowserFile file)
        {
            _uploadedFileName = file.Name;
            _uploadObject = file;
        }

        public async Task RunUploadAsync()
        {
            if (FormModel.SelectedMappingTemplateId == 0 || _uploadObject is null || string.IsNullOrWhiteSpace(_uploadedFileName))
                return;

            var selected = JoinedTemplates.FirstOrDefault(x => x.Id == FormModel.SelectedMappingTemplateId);
            if (selected is null) return;

            var ext = FileToFormatConverter.ToExtension(selected.OutputFormat);

            var outputUrl = await executor.TransformUploadedFileAsync(
                templateId: FormModel.SelectedMappingTemplateId,
                file: _uploadObject,
                outputExtension: ext
            );

            await templateManager.AddFileConversionAsync(new FileConversionHistoryDto(
                Id: 0,
                ConvertedAt: DateTime.UtcNow,
                FileName: _uploadedFileName!,
                OutputLink: outputUrl,
                MappingTemplateId: FormModel.SelectedMappingTemplateId,
                MappingTemplateName: string.Empty
            ));

            FileConversions = (await templateManager.GetFileConversionHistoryAsync())
                .OrderByDescending(x => x.ConvertedAt).ToList();
        }

        public async Task RunUrlAsync()
        {
            if (FormModel.SelectedMappingTemplateId == 0 || string.IsNullOrWhiteSpace(FormModel.FileUrl))
                return;

            var selected = JoinedTemplates.FirstOrDefault(x => x.Id == FormModel.SelectedMappingTemplateId);
            if (selected is null) return;

            var ext = FileToFormatConverter.ToExtension(selected.OutputFormat);

            var outputUrl = await executor.TransformFromUrlAsync(
                templateId: FormModel.SelectedMappingTemplateId,
                url: FormModel.FileUrl!,
                outputExtension: ext
            );

            await templateManager.AddUrlConversionAsync(new UrlConversionHistoryDto(
                Id: 0,
                ConvertedAt: DateTime.UtcNow,
                InputUrl: FormModel.FileUrl!,
                OutputLink: outputUrl,
                MappingTemplateId: FormModel.SelectedMappingTemplateId,
                MappingTemplateName: string.Empty
            ));

            if (FormModel.IntervalMinutes is int m && m > 0)
            {
                await templateManager.AddWatchedUrlAsync(new AddWatchedUrlDto(FormModel.FileUrl!, m));
                WatchedUrls = (await templateManager.GetWatchedUrlsAsync()).ToList();
            }

            UrlConversions = (await templateManager.GetUrlConversionHistoryAsync())
                .OrderByDescending(x => x.ConvertedAt).ToList();
        }

        private sealed class LazyStream
        {
            private readonly Func<Task<Stream>> _factory;
            public LazyStream(Func<Task<Stream>> factory) => _factory = factory;
            public async Task<Stream> OpenAsync() => await _factory();
        }
    }
}
