using Microsoft.AspNetCore.Components.Forms;
using BusinessLogic.Interfaces;
using BusinessLogic.Models;
using BusinessLogic.Records;
using BusinessLogic.Utilities;
using Presentation.Models;

namespace Presentation.ViewModels.Pages
{
    public class TransformationViewModel
    {
        private readonly IAppSyncContext syncContext;
        private readonly ITransformationExecutionService executor;

        private object? uploadObject;
        private string? uploadFileName;

        public event Action? Changed;

        public UploadMode InputMode { get; private set; }
        public bool IsBusy { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? SuccessMessage { get; private set; }
        public string? LastOutputLink { get; private set; }
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
            ErrorMessage = null;
            SuccessMessage = null;
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
            ErrorMessage = null;
            SuccessMessage = null;
            Changed?.Invoke();
        }

        public void SetUploadedFile(IBrowserFile file)
        {
            uploadFileName = file.Name;
            uploadObject = file;
            FormModel.UploadedFileName = file.Name;
            ErrorMessage = null;
            SuccessMessage = null;
            Changed?.Invoke();
        }

        public async Task RunTransformation()
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;
            LastOutputLink = null;
            Changed?.Invoke();

            try
            {
                if (FormModel.SelectedMappingTemplateId == 0)
                {
                    throw new InvalidOperationException("Select a transformation template first.");
                }

                string outputUrl;
                if (InputMode == UploadMode.Upload)
                {
                    outputUrl = await RunUpload();
                }
                else
                {
                    outputUrl = await RunUrl();
                }

                await ReloadWatched();
                BindFromSyncContext();
                LastOutputLink = outputUrl;
                SuccessMessage = "Transformation completed.";
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.Message;
            }
            finally
            {
                IsBusy = false;
                Changed?.Invoke();
            }
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

            IsBusy = true;
            ErrorMessage = null;
            Changed?.Invoke();

            try
            {
                await AddToWatchlist(url, interval, templateId);
                await ReloadWatched();
                BindFromSyncContext();
            }
            catch (Exception exception)
            {
                ErrorMessage = exception.Message;
            }
            finally
            {
                IsBusy = false;
                Changed?.Invoke();
            }
        }

        private async Task<string> RunUpload()
        {
            if (FormModel.SelectedMappingTemplateId == 0)
            {
                throw new InvalidOperationException("Select a transformation template first.");
            }

            if (uploadObject is null || string.IsNullOrWhiteSpace(uploadFileName))
            {
                throw new InvalidOperationException("Please select the file again before running the transformation.");
            }

            JoinedTemplateSummaryDto? selected = JoinedTemplates.FirstOrDefault(x => x.Id == FormModel.SelectedMappingTemplateId);
            if (selected is null)
            {
                throw new InvalidOperationException("The selected transformation template was not found.");
            }

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

            return outputUrl;
        }

        private async Task<string> RunUrl()
        {
            if (FormModel.SelectedMappingTemplateId == 0 ||
                string.IsNullOrWhiteSpace(FormModel.FileUrl))
            {
                throw new InvalidOperationException("Enter a valid file URL before running the transformation.");
            }

            JoinedTemplateSummaryDto? selected = JoinedTemplates.FirstOrDefault(x => x.Id == FormModel.SelectedMappingTemplateId);
            if (selected is null)
            {
                throw new InvalidOperationException("The selected transformation template was not found.");
            }

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

            return outputUrl;
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
                    ? uploadObject is not null && !string.IsNullOrWhiteSpace(FormModel.UploadedFileName)
                    : !string.IsNullOrWhiteSpace(FormModel.FileUrl));
        }
    }
}
