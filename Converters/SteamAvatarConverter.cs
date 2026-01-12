using System;
using System.Diagnostics;
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
        private static readonly string STEAM_API_KEY = GetApiKey();

        private static string GetApiKey()
        {
            try
            {
                // Obfuscated API key - decode at runtime
                byte[] encoded = System.Convert.FromBase64String("N0I3NjRBRTcyN0Y3QkQ0Q0NEODI4MDVEQUY2RjMyN0Q=");
                return System.Text.Encoding.UTF8.GetString(encoded);
            }
            catch
            {
                Debug.WriteLine("[SteamAvatarConverter] Failed to decode API key");
                return "";
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string steamId = value as string;

            Debug.WriteLine($"[SteamAvatarConverter] Convert called with value: {steamId ?? "null"}");

            // If no Steam ID or empty, return default icon
            if (string.IsNullOrWhiteSpace(steamId))
            {
                Debug.WriteLine("[SteamAvatarConverter] No Steam ID, returning default icon");
                return CreateDefaultSteamIcon();
            }

            // Try to load avatar
            Debug.WriteLine($"[SteamAvatarConverter] Loading avatar for Steam ID: {steamId}");
            var result = LoadSteamAvatar(steamId);
            Debug.WriteLine($"[SteamAvatarConverter] Result is null: {result == null}");
            return result ?? CreateDefaultSteamIcon();
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

                Debug.WriteLine($"[SteamAvatarConverter] Checking cache: {cachedFile}");
                Debug.WriteLine($"[SteamAvatarConverter] Cache exists: {File.Exists(cachedFile)}");

                if (File.Exists(cachedFile))
                {
                    Debug.WriteLine("[SteamAvatarConverter] Loading from cache");
                    return LoadImageFromFile(cachedFile);
                }

                // If API key is not set, return default
                if (string.IsNullOrWhiteSpace(STEAM_API_KEY))
                {
                    Debug.WriteLine("[SteamAvatarConverter] WARNING: Steam API key not set!");
                    return CreateDefaultSteamIcon();
                }

                // Fetch from Steam API asynchronously
                Debug.WriteLine("[SteamAvatarConverter] Fetching from Steam API...");
                Task.Run(async () => await FetchAndCacheAvatar(steamId, cacheDir, cachedFile));

                // Return default while loading
                return CreateDefaultSteamIcon();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SteamAvatarConverter] Error: {ex.Message}");
                return CreateDefaultSteamIcon();
            }
        }

        private async Task FetchAndCacheAvatar(string steamId, string cacheDir, string cachedFile)
        {
            try
            {
                Debug.WriteLine($"[SteamAvatarConverter] Calling Steam API for {steamId}");

                // Steam API endpoint to get player summaries
                string apiUrl = $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key={STEAM_API_KEY}&steamids={steamId}";

                var response = await _httpClient.GetStringAsync(apiUrl);
                Debug.WriteLine($"[SteamAvatarConverter] API Response received");

                var json = JObject.Parse(response);

                var players = json["response"]?["players"];
                if (players != null && players.HasValues)
                {
                    string avatarUrl = players[0]?["avatarfull"]?.ToString(); // Full size avatar (184x184)
                    Debug.WriteLine($"[SteamAvatarConverter] Avatar URL: {avatarUrl}");

                    if (!string.IsNullOrEmpty(avatarUrl))
                    {
                        // Download avatar image
                        Debug.WriteLine("[SteamAvatarConverter] Downloading avatar...");
                        byte[] imageBytes = await _httpClient.GetByteArrayAsync(avatarUrl);

                        // Save to cache
                        Directory.CreateDirectory(cacheDir);
                        await File.WriteAllBytesAsync(cachedFile, imageBytes);
                        Debug.WriteLine($"[SteamAvatarConverter] Avatar cached to: {cachedFile}");

                        // Force UI refresh by triggering property change
                        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                        {
                            Debug.WriteLine("[SteamAvatarConverter] Triggering UI refresh");
                            // The UI will re-query the binding and this time find the cached file
                        });
                    }
                }
                else
                {
                    Debug.WriteLine("[SteamAvatarConverter] No players found in API response");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SteamAvatarConverter] Fetch error: {ex.Message}");
            }
        }

        private BitmapImage LoadImageFromFile(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SteamAvatarConverter] Error loading image from file: {ex.Message}");
                return null;
            }
        }

        private BitmapImage CreateDefaultSteamIcon()
        {
            try
            {
                Debug.WriteLine("[SteamAvatarConverter] Creating default Steam icon");
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("pack://application:,,,/HackHelper;component/Resources/steam_default.png", UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                Debug.WriteLine("[SteamAvatarConverter] Default icon loaded successfully");
                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SteamAvatarConverter] Failed to load default Steam icon: {ex.Message}");
                return null;
            }
        }
    }
}