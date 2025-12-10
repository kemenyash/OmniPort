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

            var urlBuilder = new UriBuilder(uri) 
            {
                Fragment = string.Empty 
            };

            return urlBuilder.Uri.ToString();
        }
    }
}
