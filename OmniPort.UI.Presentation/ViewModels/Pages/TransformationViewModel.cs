using Microsoft.AspNetCore.Components.Forms;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Models;
using OmniPort.Core.Records;
using OmniPort.Core.Utilities;
using OmniPort.UI.Presentation.Models;

namespace OmniPort.UI.Presentation.ViewModels.Pages
{
    public class TransformationViewModel
    {
        private readonly IAppSyncContext syncContext;
        private readonly ITransformationExecutionService executor;

        private object? uploadObject;
        private string? uploadFileName;

        public event Action? Changed;

        public UploadMode InputMode { get; private set; }
        public bool CanRun => CanRunTransformation();
        public bool CanAddToWatchlist => CanAddToWatchListFromForm();

        public TransformationRunForm FormModel { get; private set; }
        public List<JoinedTemplateSummaryDto> JoinedTemplates { get; private set; }
        public List<FileConversionHistoryDto> FileConversions { get; private set; }
        public List<UrlConversionHistoryDto> UrlConversions { get; private set; }
        public List<WatchedUrlDto> WatchedUrls { get; private set; }

        public TransformationViewModel(IAppSyncContext syncContext, ITransformationExecutionService executor)
        {
            this.syncContext = syncContext;
            this.executor = executor;

            InputMode = UploadMode.Upload;
            FormModel = new TransformationRunForm();
            JoinedTemplates = new List<JoinedTemplateSummaryDto>();
            FileConversions = new List<FileConversionHistoryDto>();
            UrlConversions = new List<UrlConversionHistoryDto>();
            WatchedUrls = new List<WatchedUrlDto>();
        }

        public async Task InitializeAsync()
        {
            if (!syncContext.JoinedTemplates.Any())
            {
                await syncContext.Initialize();
            }

            BindFromSyncContext();
            syncContext.Changed += OnSyncChanged;

            if (FormModel.SelectedMappingTemplateId == 0 && JoinedTemplates.Any())
            {
                FormModel.SelectedMappingTemplateId = JoinedTemplates.First().Id;
            }

            Changed?.Invoke();
        }

        public void SetMode(UploadMode mode)
        {
            InputMode = mode;
            Changed?.Invoke();
        }

        public string GetButtonClass(UploadMode mode)
        {
            return InputMode == mode
                 ? "bg-blue-600 text-white px-3 py-1 rounded"
                 : "bg-gray-200 text-gray-700 px-3 py-1 rounded";
        }

        public void SetUploadedFile(string fileName, Func<Task<Stream>> openStream)
        {
            uploadFileName = fileName;
            uploadObject = new LazyStream(openStream);
            FormModel.UploadedFileName = fileName;
            Changed?.Invoke();
        }

        public void SetUploadedFile(IBrowserFile file)
        {
            uploadFileName = file.Name;
            uploadObject = file;
            FormModel.UploadedFileName = file.Name;
            Changed?.Invoke();
        }

        public async Task RunTransformation()
        {
            if (FormModel.SelectedMappingTemplateId == 0) return;

            if (InputMode == UploadMode.Upload)
            {
                await RunUpload();
            }
            else
            {
                await RunUrl();
            }

            await ReloadWatched();
            BindFromSyncContext();
            Changed?.Invoke();
        }

        public async Task AddToWatchlistFromForm()
        {
            int templateId = FormModel.SelectedMappingTemplateId;
            string url = (FormModel.FileUrl ?? string.Empty).Trim();
            int interval = FormModel.IntervalMinutes.GetValueOrDefault(15);

            if (templateId == 0 || string.IsNullOrWhiteSpace(url) || interval <= 0)
            {
                return;
            }

            await AddToWatchlist(url, interval, templateId);
            await ReloadWatched();
            BindFromSyncContext();
            Changed?.Invoke();
        }

        private async Task RunUpload()
        {
            if (FormModel.SelectedMappingTemplateId == 0 ||
                uploadObject is null ||
                string.IsNullOrWhiteSpace(uploadFileName))
            {
                return;
            }

            JoinedTemplateSummaryDto? selected = JoinedTemplates.FirstOrDefault(x => x.Id == FormModel.SelectedMappingTemplateId);
            if (selected is null) return;

            string extension = FileToFormatConverter.ToExtension(selected.OutputFormat);

            string outputUrl = await executor.TransformUploadedFile(
                templateId: FormModel.SelectedMappingTemplateId,
                file: uploadObject,
                outputExtension: extension
            );

            await syncContext.AddFileConversion(new FileConversionHistoryDto(
                Id: 0,
                ConvertedAt: DateTime.UtcNow,
                FileName: uploadFileName!,
                OutputLink: outputUrl,
                MappingTemplateId: FormModel.SelectedMappingTemplateId,
                MappingTemplateName: string.Empty
            ));
        }

        private async Task RunUrl()
        {
            if (FormModel.SelectedMappingTemplateId == 0 ||
                string.IsNullOrWhiteSpace(FormModel.FileUrl))
            {
                return;
            }

            JoinedTemplateSummaryDto? selected = JoinedTemplates.FirstOrDefault(x => x.Id == FormModel.SelectedMappingTemplateId);
            if (selected is null) return;

            string extension = FileToFormatConverter.ToExtension(selected.OutputFormat);

            string outputUrl = await executor.TransformFromUrl(
                templateId: FormModel.SelectedMappingTemplateId,
                url: FormModel.FileUrl!,
                outputExtension: extension
            );

            await syncContext.AddUrlConversion(new UrlConversionHistoryDto(
                Id: 0,
                ConvertedAt: DateTime.UtcNow,
                InputUrl: FormModel.FileUrl!,
                OutputLink: outputUrl,
                MappingTemplateId: FormModel.SelectedMappingTemplateId,
                MappingTemplateName: string.Empty
            ));
        }

        private Task AddToWatchlist(string url, int intervalMinutes, int mappingTemplateId)
        {
            if (string.IsNullOrWhiteSpace(url) || intervalMinutes <= 0)
            {
                return Task.CompletedTask;
            }

            return syncContext.AddWatchedUrl(new AddWatchedUrlDto(url, intervalMinutes, mappingTemplateId));
        }

        private Task ReloadWatched()
        {
            return syncContext.RefreshAll();
        }

        private void OnSyncChanged()
        {
            BindFromSyncContext();
            Changed?.Invoke();
        }

        private void BindFromSyncContext()
        {
            JoinedTemplates = syncContext.JoinedTemplates.ToList();
            FileConversions = syncContext.FileConversions.ToList();
            UrlConversions = syncContext.UrlConversions.ToList();
            WatchedUrls = syncContext.WatchedUrls.ToList();
        }

        private bool CanAddToWatchListFromForm()
        {
            return InputMode == UploadMode.Url
                && FormModel.SelectedMappingTemplateId != 0
                && !string.IsNullOrWhiteSpace(FormModel.FileUrl)
                && (FormModel.IntervalMinutes.HasValue && FormModel.IntervalMinutes.Value > 0);
        }

        private bool CanRunTransformation()
        {
            return FormModel.SelectedMappingTemplateId != 0
                && (InputMode == UploadMode.Upload
                    ? !string.IsNullOrWhiteSpace(FormModel.UploadedFileName)
                    : !string.IsNullOrWhiteSpace(FormModel.FileUrl));
        }
    }
}
