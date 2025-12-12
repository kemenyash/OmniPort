using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OmniPort.Core.Utilities
{
    public sealed class UrlContentFetcher
    {
        private readonly string userAgent;

        public UrlContentFetcher(string userAgent)
        {
            this.userAgent = userAgent;
        }

        public ContentSnapshot DownloadEffectiveContent(string effectiveUrl, CancellationToken ct)
        {
            using var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10,
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };

            using var http = new HttpClient(handler, disposeHandler: true);

            ContentSnapshot first = Download(http, effectiveUrl, referer: null, ct);

            if (IsHtml(first))
            {
                Uri? direct = TryFindDirectFileLink(PeekText(first.Bytes, 512 * 1024), new Uri(effectiveUrl));
                if (direct is not null)
                {
                    ContentSnapshot second = Download(http, direct.ToString(), referer: effectiveUrl, ct);
                    return second;
                }
            }

            return first;
        }

        private ContentSnapshot Download(HttpClient http, string url, string? referer, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd(userAgent);
            req.Headers.Accept.ParseAdd("*/*");
            if (!string.IsNullOrWhiteSpace(referer))
            {
                req.Headers.Referrer = new Uri(referer);
            }

            using HttpResponseMessage resp = http.Send(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            string contentType = resp.Content.Headers.ContentType?.MediaType ?? string.Empty;

            using Stream s = resp.Content.ReadAsStream(ct);
            byte[] bytes = ReadAllBytes(s, ct);

            return new ContentSnapshot(bytes, contentType);
        }

        private static bool IsHtml(ContentSnapshot snap)
        {
            if (snap.ContentType.Contains("html", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string head = PeekText(snap.Bytes, 4096);
            return head.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase)
                   || head.Contains("<html", StringComparison.OrdinalIgnoreCase);
        }

        private static string PeekText(byte[] bytes, int maxBytes)
        {
            int len = Math.Min(maxBytes, bytes.Length);
            if (len <= 0) return string.Empty;

            try
            {
                string t = Encoding.UTF8.GetString(bytes, 0, len);
                return t.Replace("\0", "").Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static byte[] ReadAllBytes(Stream src, CancellationToken ct)
        {
            using var ms = new MemoryStream();
            byte[] buffer = new byte[81920];

            while (true)
            {
                ct.ThrowIfCancellationRequested();
                int read = src.Read(buffer, 0, buffer.Length);
                if (read <= 0) break;
                ms.Write(buffer, 0, read);
            }

            return ms.ToArray();
        }

        private static Uri? TryFindDirectFileLink(string html, Uri baseUri)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;

            if (IndexOfHrefWithExtensions(html, out string? href, new[] { ".xlsx", ".csv", ".json", ".xml" }) < 0)
            {
                return null;
            }

            return Uri.TryCreate(baseUri, href, out Uri? abs) ? abs : null;

            static int IndexOfHrefWithExtensions(string html, out string? href, string[] exts)
            {
                href = null;
                string lower = html.ToLowerInvariant();
                const string key = "href";
                int pos = 0;

                while ((pos = lower.IndexOf(key, pos, StringComparison.Ordinal)) >= 0)
                {
                    int eq = lower.IndexOf('=', pos + key.Length);
                    if (eq < 0) { pos += key.Length; continue; }

                    int i = eq + 1;
                    while (i < html.Length && char.IsWhiteSpace(html[i])) i++;
                    if (i >= html.Length) break;

                    char quote = html[i];
                    string candidate;

                    if (quote == '"' || quote == '\'')
                    {
                        i++;
                        int j = html.IndexOf(quote, i);
                        if (j < 0) break;
                        candidate = html.Substring(i, j - i);
                        pos = j + 1;
                    }
                    else
                    {
                        int j = i;
                        while (j < html.Length && !char.IsWhiteSpace(html[j]) && html[j] != '>')
                        {
                            j++;
                        }
                        candidate = html.Substring(i, j - i);
                        pos = j;
                    }

                    if (exts.Any(e => candidate.Contains(e, StringComparison.OrdinalIgnoreCase)))
                    {
                        href = candidate.Trim();
                        return pos;
                    }
                }

                return -1;
            }
        }
    }
}
