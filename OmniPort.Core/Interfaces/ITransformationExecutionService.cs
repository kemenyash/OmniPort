namespace OmniPort.Core.Interfaces
{
    public interface ITransformationExecutionService
    {
        Task<string> TransformUploadedFile(int templateId, object file, string outputExtension);
        Task<string> TransformFromUrl(int templateId, string url, string outputExtension);
        Task<string> SaveTransformed(IEnumerable<IDictionary<string, object?>> rows, string outputExtension, string baseName);

    }
}
