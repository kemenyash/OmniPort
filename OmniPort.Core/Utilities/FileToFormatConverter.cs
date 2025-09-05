using OmniPort.Core.Enums;
using System;
using System.IO;
using System.Text;

namespace OmniPort.Core.Utilities
{
    public static class FileToFormatConverter
    {
        public static string ToExtension(SourceType t) => t switch
        {
            SourceType.CSV => "csv",
            SourceType.JSON => "json",
            SourceType.XML => "xml",
            SourceType.Excel => "xlsx",
            _ => "csv"
        };
    }
}
