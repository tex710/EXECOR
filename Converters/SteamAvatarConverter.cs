using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;

namespace HackHelper.Converters
{
    public class SteamAvatarConverter : IValueConverter
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string STEAM_API_KEY = "YOUR_STEAM_API_KEY_HERE"; // Get from https://steamcommunity.com/dev/apikey

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string steamId = value as string;

            // If no Steam ID or empty, return default icon
            if (string.IsNullOrWhiteSpace(steamId))
            {
                System.Diagnostics.Debug.WriteLine("No Steam ID provided, using default icon");
                return CreateDefaultSteamIcon();
            }

            // Try to load cached avatar or fetch from Steam API
            System.Diagnostics.Debug.WriteLine($"Loading avatar for Steam ID: {steamId}");
            return LoadSteamAvatar(steamId);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private BitmapImage LoadSteamAvatar(string steamId)
        {
            try
            {
                // Check cache first
                string cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache", "avatars");
                string cachedFile = Path.Combine(cacheDir, $"{steamId}.jpg");

                if (File.Exists(cachedFile))
                {
                    return LoadImageFromFile(cachedFile);
                }

                // Fetch from Steam API asynchronously
                Task.Run(async () => await FetchAndCacheAvatar(steamId, cacheDir, cachedFile));

                // Return default while loading
                return CreateDefaultSteamIcon();
            }
            catch
            {
                return CreateDefaultSteamIcon();
            }
        }

        private async Task FetchAndCacheAvatar(string steamId, string cacheDir, string cachedFile)
        {
            try
            {
                // Steam API endpoint to get player summaries
                string apiUrl = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={STEAM_API_KEY}&steamids={steamId}";

                var response = await _httpClient.GetStringAsync(apiUrl);
                var json = JObject.Parse(response);

                var players = json["response"]?["players"];
                if (players != null && players.HasValues)
                {
                    string avatarUrl = players[0]?["avatarfull"]?.ToString(); // Full size avatar (184x184)

                    if (!string.IsNullOrEmpty(avatarUrl))
                    {
                        // Download avatar image
                        byte[] imageBytes = await _httpClient.GetByteArrayAsync(avatarUrl);

                        // Save to cache
                        Directory.CreateDirectory(cacheDir);
                        await File.WriteAllBytesAsync(cachedFile, imageBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to fetch Steam avatar for {steamId}: {ex.Message}");
            }
        }

        private BitmapImage LoadImageFromFile(string filePath)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private BitmapImage CreateDefaultSteamIcon()
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("pack://application:,,,/HackHelper;component/Resources/steam_default.png", UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load default Steam icon: {ex.Message}");
                // Return null to show emoji fallback
                return null;
            }
        }
    }
}