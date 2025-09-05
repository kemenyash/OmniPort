using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Utilities
{
    public static class UrlWatchTagger
    {
        private const string TagKey = "__map";

        public static string AttachMapId(string url, int mappingTemplateId)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;

            var uri = new Uri(url, UriKind.Absolute);

            var baseWithoutFragment = new UriBuilder(uri) { Fragment = string.Empty }.Uri;

            var fragment = $"#{TagKey}={mappingTemplateId}";
            return baseWithoutFragment + fragment;
        }

        public static bool TryExtractMapId(string url, out int mappingTemplateId)
        {
            mappingTemplateId = 0;
            if (string.IsNullOrWhiteSpace(url)) return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            var frag = uri.Fragment; 
            if (string.IsNullOrEmpty(frag)) return false;

            var kv = frag.TrimStart('#').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in kv)
            {
                var pair = p.Split('=', 2);
                if (pair.Length == 2 && string.Equals(pair[0], TagKey, StringComparison.OrdinalIgnoreCase))
                {
                    return int.TryParse(pair[1], out mappingTemplateId);
                }
            }
            return false;
        }

        public static string StripTag(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return url;

            var b = new UriBuilder(uri) { Fragment = string.Empty };
            return b.Uri.ToString();
        }
    }
}
