using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Utilities
{
    public static class UrlWatchTagger
    {
        public static string StripTag(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return url;

            var b = new UriBuilder(uri) { Fragment = string.Empty };
            return b.Uri.ToString();
        }
    }
}
