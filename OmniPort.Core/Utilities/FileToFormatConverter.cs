using OmniPort.Core.Models;
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

        public static SourceType DetectSourceType(byte[] bytes, string? fileNameOrUrl)
        {
            if (!string.IsNullOrWhiteSpace(fileNameOrUrl))
            {
                string ext;
                if (Uri.TryCreate(fileNameOrUrl, UriKind.RelativeOrAbsolute, out var uri) && uri.IsAbsoluteUri)
                    ext = Path.GetExtension(uri.AbsolutePath);
                else
                    ext = Path.GetExtension(fileNameOrUrl);

                switch (ext.ToLowerInvariant())
                {
                    case ".csv": return SourceType.CSV;
                    case ".xlsx":
                    case ".xls": return SourceType.Excel;
                    case ".json": return SourceType.JSON;
                    case ".xml": return SourceType.XML;
                }
            }

            if (bytes.Length >= 4 &&
                bytes[0] == (byte)'P' && bytes[1] == (byte)'K' && bytes[2] == 3 && bytes[3] == 4)
                return SourceType.Excel;

            if (bytes.Length >= 8 &&
                bytes[0] == 0xD0 && bytes[1] == 0xCF && bytes[2] == 0x11 && bytes[3] == 0xE0 &&
                bytes[4] == 0xA1 && bytes[5] == 0xB1 && bytes[6] == 0x1A && bytes[7] == 0xE1)
                return SourceType.Excel;

            var text = TryGetTextPrefix(bytes, 2048).TrimStart();

            if (text.StartsWith("{") || text.StartsWith("["))
                return SourceType.JSON;

            if (text.StartsWith("<"))
                return SourceType.XML;

            if ((text.Contains(',') || text.Contains(';') || text.Contains('\t')) && text.Contains('\n'))
                return SourceType.CSV;

            return SourceType.CSV;
        }

        private static string TryGetTextPrefix(byte[] bytes, int maxBytes)
        {
            var take = Math.Min(bytes.Length, maxBytes);
            if (take <= 0) return string.Empty;

            try
            {
                return Encoding.UTF8.GetString(bytes, 0, take);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
