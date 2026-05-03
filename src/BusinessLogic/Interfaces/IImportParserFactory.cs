using BusinessLogic.Enums;

namespace BusinessLogic.Interfaces
{
    public interface IImportParserFactory
    {
        IImportParser Create(SourceType sourceType);
    }
}
