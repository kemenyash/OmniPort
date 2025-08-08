using DocumentFormat.OpenXml.Spreadsheet;
using OmniPort.Core.Extensions;
using OmniPort.Core.Interfaces;
using OmniPort.Core.Mappers;
using OmniPort.Core.Models;
using OmniPort.Core.Parsers;
using OmniPort.Core.Utilities;
using OmniPort.UI.Presentation.Interfaces;
using OmniPort.UI.Presentation.Models;
using System.Net.Http.Headers;

namespace OmniPort.UI.Presentation.ViewModels
{
    public class TransformationViewModel
    {
        private readonly ITransformationManager _manager;
        private readonly ITransformationExecutionService _executor;
        private readonly HttpClient _http;

        public TransformFormModel FormModel { get; } = new();

        public (string FileName, Func<Task<Stream>> OpenReadStream)? UploadedFile { get; private set; }

        public List<JoinedTemplateSummary> JoinedTemplates { get; private set; } = new();
        public List<ConversionHistory> FileConversions { get; private set; } = new();
        public List<UrlConversionHistory> UrlConversions { get; private set; } = new();
        public List<WatchedUrl> WatchedUrls { get; private set; } = new();

        public TransformationViewModel(ITransformationManager manager,
                                       ITransformationExecutionService executor,
                                       HttpClient http)
        {
            _manager = manager;
            _executor = executor;
            _http = http;
        }

        public async Task InitAsync()
        {
            JoinedTemplates = await _manager.GetJoinedTemplatesAsync();
            FileConversions = await _manager.GetFileConversionHistoryAsync();
            UrlConversions = await _manager.GetUrlConversionHistoryAsync();
            WatchedUrls = await _manager.GetWatchedUrlsAsync();
        }


        public void SetUploadedFile(string fileName, Func<Task<Stream>> openReadStream)
        {
            UploadedFile = (fileName, openReadStream);
        }

        public async Task RunUploadAsync()
        {
            if (UploadedFile is null || FormModel.SelectedTemplateId == 0) return;

            var (profile, _importSourceFromJoin, convertSource) = await _manager.GetImportProfileForJoinAsync(FormModel.SelectedTemplateId);
            var mapper = new ImportMapper(profile);

            List<IDictionary<string, object?>> rows = new List<IDictionary<string, object?>>();

            await using (var src = await UploadedFile.Value.OpenReadStream())
            await using (var ms = new MemoryStream())
            {
                await src.CopyToAsync(ms);
                var bytes = ms.ToArray();
                var detectedImport = FileToFormatConverter.DetectSourceType(bytes, UploadedFile.Value.FileName);

                var parser = ResolveParser(detectedImport, UploadedFile.Value.FileName);
                ms.Position = 0;
                rows = parser.Parse(ms).ToList();
            }

            rows.Remove(rows[0]);
            var mappedRows = rows.Select(mapper.MapRow).ToList();

            var outputLink = await _executor.SaveTransformedAsync(
                mappedRows,
                FileToFormatConverter.ToExtension(convertSource),
                Path.GetFileNameWithoutExtension(UploadedFile.Value.FileName));

            var record = new ConversionHistory
            {
                FileName = UploadedFile.Value.FileName,
                ConvertedAt = DateTime.UtcNow,
                TemplateName = GetTemplateName(),
                OutputLink = outputLink,
                TemplateMapId = FormModel.SelectedTemplateId
            };

            await _manager.AddFileConversionAsync(record);
            FileConversions = await _manager.GetFileConversionHistoryAsync();
        }

        public async Task RunUrlAsync()
        {
            if (string.IsNullOrWhiteSpace(FormModel.FileUrl) || FormModel.SelectedTemplateId == 0) return;

            var (profile, _importSourceFromJoin, convertSource) = await _manager.GetImportProfileForJoinAsync(FormModel.SelectedTemplateId);
            var mapper = new ImportMapper(profile);

            using var request = new HttpRequestMessage(HttpMethod.Get, FormModel.FileUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            List<IDictionary<string, object?>> rows;

            await using (var src = await response.Content.ReadAsStreamAsync())
            await using (var ms = new MemoryStream())
            {
                await src.CopyToAsync(ms);
                var bytes = ms.ToArray();

                var detectedImport = FileToFormatConverter.DetectSourceType(bytes, FormModel.FileUrl);

                var parser = ResolveParser(detectedImport, FormModel.FileUrl);

                ms.Position = 0;
                rows = parser.Parse(ms).ToList();
            }

            var mappedRows = rows.Select(mapper.MapRow).ToList();

            var safeName = MakeSafeBaseNameFromUrl(FormModel.FileUrl);
            var outputLink = await _executor.SaveTransformedAsync(
                mappedRows,
                FileToFormatConverter.ToExtension(convertSource),
                safeName);

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




        private IImportParser ResolveParser(SourceType sourceType, string? fileNameOrUrl)
        {

            if (!string.IsNullOrWhiteSpace(fileNameOrUrl))
            {
                var ext = Path.GetExtension(new Uri(fileNameOrUrl, UriKind.RelativeOrAbsolute).IsAbsoluteUri
                                            ? new Uri(fileNameOrUrl).AbsolutePath
                                            : fileNameOrUrl)
                          .ToLowerInvariant();

                switch (ext)
                {
                    case ".csv": return new CsvImportParser();
                    case ".xlsx":
                    case ".xls": return new ExcelImportParser();
                    case ".json": return new JsonImportParser();
                    case ".xml": return new XmlImportParser("record"); 
                }
            }

            return sourceType switch
            {
                SourceType.CSV => new CsvImportParser(),
                SourceType.Excel => new ExcelImportParser(),
                SourceType.JSON => new JsonImportParser(),
                SourceType.XML => new XmlImportParser("record"),
                _ => new CsvImportParser()
            };
        }

        private string MakeSafeBaseNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var file = Path.GetFileNameWithoutExtension(uri.LocalPath);
                return string.IsNullOrWhiteSpace(file) ? "remote" : file;
            }
            catch
            {
                return "remote";
            }
        }

        private string GetTemplateName()
        {
            var jt = JoinedTemplates.FirstOrDefault(t => t.Id == FormModel.SelectedTemplateId);
            return jt is null ? "Unknown" : $"{jt.SourceTemplate} → {jt.TargetTemplate}";
        }
    }
}
