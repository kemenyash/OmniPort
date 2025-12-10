using OmniPort.Core.Enums;
using System;
using System.IO;
using System.Text;

namespace OmniPort.Core.Utilities
{
    public static class FileToFormatConverter
    {
        public static string ToExtension(SourceType sourceType)
        {
            switch (sourceType)
            {
                case SourceType.CSV: return "csv";
                case SourceType.JSON: return "json";
                case SourceType.XML: return "xml";
                case SourceType.Excel: return "xlsx";
                default: return "csv";
            }
        }
    }
}
