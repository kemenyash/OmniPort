using BusinessLogic.Enums;
using BusinessLogic.Interfaces;

namespace Infrastructure.Parsers
{
    public sealed class ImportParserFactory : IImportParserFactory
    {
        public IImportParser Create(SourceType sourceType)
        {
            switch (sourceType)
            {
                case SourceType.CSV:
                    {
                        return new CsvImportParser();
                    }
                case SourceType.Excel:
                    {
                        return new ExcelImportParser();
                    }
                case SourceType.JSON:
                    {
                        return new JsonImportParser();
                    }
                case SourceType.XML:
                    {
                        return new XmlImportParser("record");
                    }
                default:
                    {
                        return new CsvImportParser();
                    }
            }
        }
    }
}
