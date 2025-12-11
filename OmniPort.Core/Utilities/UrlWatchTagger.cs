namespace OmniPort.Core.Utilities
{
    public static class UrlWatchTagger
    {
        public static string StripTag(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return url;

            UriBuilder urlBuilder = new UriBuilder(uri)
            {
                Fragment = string.Empty
            };

            return urlBuilder.Uri.ToString();
        }
    }
}
