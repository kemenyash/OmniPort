using Microsoft.AspNetCore.Components.Forms;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Models;
using OmniPort.Core.Records;
using OmniPort.Core.Utilities;
using OmniPort.UI.Presentation.Interfaces;
using OmniPort.UI.Presentation.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OmniPort.UI.Presentation.ViewModels
{
    public class TransformationViewModel
    {
        private readonly IAppSyncContext sync;
        private readonly ITransformationExecutionService executor;

        public TransformationViewModel(IAppSyncContext sync, ITransformationExecutionService executor)
        {
            this.sync = sync;
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
            if (!sync.JoinedTemplates.Any())
                await sync.InitializeAsync();

            BindFromSync();
            sync.Changed += () => BindFromSync();

            if (FormModel.SelectedMappingTemplateId == 0 && JoinedTemplates.Any())
                FormModel.SelectedMappingTemplateId = JoinedTemplates.First().Id;
        }

        private void BindFromSync()
        {
            JoinedTemplates = sync.JoinedTemplates.ToList();
            FileConversions = sync.FileConversions.ToList();
            UrlConversions = sync.UrlConversions.ToList();
            WatchedUrls = sync.WatchedUrls.ToList();
        }

        public void SetUploadedFile(string fileName, Func<Task<Stream>> openStream)
        {
            _uploadedFileName = fileName;
            _uploadObject = new LazyStream(openStream);
            FormModel.UploadedFileName = fileName;
        }

        public void SetUploadedFile(IBrowserFile file)
        {
            _uploadedFileName = file.Name;
            _uploadObject = file;
            FormModel.UploadedFileName = file.Name;
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

            await sync.AddFileConversionAsync(new FileConversionHistoryDto(
                Id: 0,
                ConvertedAt: DateTime.UtcNow,
                FileName: _uploadedFileName!,
                OutputLink: outputUrl,
                MappingTemplateId: FormModel.SelectedMappingTemplateId,
                MappingTemplateName: string.Empty
            ));
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

            await sync.AddUrlConversionAsync(new UrlConversionHistoryDto(
                Id: 0,
                ConvertedAt: DateTime.UtcNow,
                InputUrl: FormModel.FileUrl!,
                OutputLink: outputUrl,
                MappingTemplateId: FormModel.SelectedMappingTemplateId,
                MappingTemplateName: string.Empty
            ));
        }

        public async Task AddToWatchlistAsync(string url, int intervalMinutes, int mappingTemplateId)
        {
            if (string.IsNullOrWhiteSpace(url) || intervalMinutes <= 0) return;
            await sync.AddWatchedUrlAsync(new AddWatchedUrlDto(url, intervalMinutes));
        }

        public async Task ReloadWatchedAsync() => await sync.RefreshAllAsync();

        private sealed class LazyStream
        {
            private readonly Func<Task<Stream>> _factory;
            public LazyStream(Func<Task<Stream>> factory) => _factory = factory;
            public async Task<Stream> OpenAsync() => await _factory();
        }
    }
}
