namespace BusinessLogic.Interfaces
{
    public interface IImportParser
    {
        Task<IReadOnlyList<IDictionary<string, object?>>> ParseAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}
