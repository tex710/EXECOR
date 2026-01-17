using System;
using System.IO;
using Newtonsoft.Json;

namespace HackHelper.Services
{
    /// <summary>
    /// Helper class to store Steam API settings
    /// </summary>
    public class SteamSettings
    {
        public string? ApiKey { get; set; }

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HackHelper",
            "steam_settings.json"
        );

        /// <summary>
        /// Loads Steam settings from disk
        /// </summary>
        public static SteamSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<SteamSettings>(json) ?? new SteamSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load Steam settings: {ex.Message}");
            }

            return new SteamSettings();
        }

        /// <summary>
        /// Saves Steam settings to disk
        /// </summary>
        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save Steam settings: {ex.Message}");
            }
        }
    }
}