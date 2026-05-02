namespace BusinessLogic.Interfaces
{
    public interface IImportParser
    {
        IEnumerable<IDictionary<string, object?>> Parse(Stream stream);
    }
}
