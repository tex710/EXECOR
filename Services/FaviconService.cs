using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Execor.Services
{
    public static class FaviconService
    {
        private static readonly HttpClient httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        public static async Task<string> FetchFaviconAsync(string websiteUrl)
        {
            if (string.IsNullOrWhiteSpace(websiteUrl))
                return null;

            try
            {
                // Normalize URL
                if (!websiteUrl.StartsWith("http://") && !websiteUrl.StartsWith("https://"))
                {
                    websiteUrl = "https://" + websiteUrl;
                }

                Uri uri = new Uri(websiteUrl);
                string domain = uri.Host;

                // Try multiple favicon sources
                string[] faviconUrls = new[]
                {
                    $"https://www.google.com/s2/favicons?domain={domain}&sz=64",
                    $"https://{domain}/favicon.ico",
                    $"https://icons.duckduckgo.com/ip3/{domain}.ico"
                };

                foreach (string faviconUrl in faviconUrls)
                {
                    try
                    {
                        byte[] imageBytes = await httpClient.GetByteArrayAsync(faviconUrl);

                        // Verify it's a valid image (at least 100 bytes)
                        if (imageBytes != null && imageBytes.Length > 100)
                        {
                            return Convert.ToBase64String(imageBytes);
                        }
                    }
                    catch
                    {
                        // Try next source
                        continue;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public static string FetchFavicon(string websiteUrl)
        {
            try
            {
                return FetchFaviconAsync(websiteUrl).GetAwaiter().GetResult();
            }
            catch
            {
                return null;
            }
        }
    }
}