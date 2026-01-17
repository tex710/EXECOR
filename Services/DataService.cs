using HackHelper.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HackHelper.Services
{
    public class DataService
    {
        private static readonly string LaunchersFile = Path.Combine(PathManager.AppDataFolder, "launchers.json");
        private static readonly string PasswordsFile = Path.Combine(PathManager.AppDataFolder, "passwords.dat");
        private static readonly string SettingsFile = Path.Combine(PathManager.AppDataFolder, "settings.json");
        private static readonly string CustomThemesFile = Path.Combine(PathManager.AppDataFolder, "customThemes.json");
        

        public DataService()
        {
        }

        // Launcher methods
        public List<Launcher> LoadLaunchers()
        {
            if (!File.Exists(LaunchersFile))
                return new List<Launcher>();

            string json = File.ReadAllText(LaunchersFile);
            return JsonConvert.DeserializeObject<List<Launcher>>(json) ?? new List<Launcher>();
        }

        public void SaveLaunchers(List<Launcher> launchers)
        {
            string json = JsonConvert.SerializeObject(launchers, Formatting.Indented);
            File.WriteAllText(LaunchersFile, json);
        }

        public void UpdateLauncher(Launcher updatedLauncher)
        {
            var launchers = LoadLaunchers();
            var index = launchers.FindIndex(l => l.Id == updatedLauncher.Id);

            if (index >= 0)
            {
                launchers[index] = updatedLauncher;
                SaveLaunchers(launchers);
            }
        }

        

        // Password methods
        public List<PasswordEntry> LoadPasswords()
        {
            if (!File.Exists(PasswordsFile))
                return new List<PasswordEntry>();

            try
            {
                string encryptedData = File.ReadAllText(PasswordsFile);
                string decryptedJson = EncryptionService.Decrypt(encryptedData);
                return JsonConvert.DeserializeObject<List<PasswordEntry>>(decryptedJson) ?? new List<PasswordEntry>();
            }
            catch
            {
                return new List<PasswordEntry>();
            }
        }

        public void SavePasswords(List<PasswordEntry> passwords)
        {
            string json = JsonConvert.SerializeObject(passwords, Formatting.Indented);
            string encryptedData = EncryptionService.Encrypt(json);
            File.WriteAllText(PasswordsFile, encryptedData);
        }

        public void UpdatePassword(PasswordEntry updatedPassword)
        {
            var passwords = LoadPasswords();
            var index = passwords.FindIndex(p => p.Id == updatedPassword.Id);

            if (index >= 0)
            {
                passwords[index] = updatedPassword;
                SavePasswords(passwords);
            }
        }

        // Settings methods
        public Settings LoadSettings()
        {
            if (!File.Exists(SettingsFile))
            {
                var defaultSettings = new Settings
                {
                    SelectedTheme = "Blue & Pink (Default)",
                    ClipboardAutoClear = true,
                    ClipboardTimeout = 30,
                    LaunchOnStartup = false
                };
                SaveSettings(defaultSettings);
                return defaultSettings;
            }

            string json = File.ReadAllText(SettingsFile);
            return JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
        }

        public void SaveSettings(Settings settings)
        {
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(SettingsFile, json);
        }

        // Custom Themes methods
        public List<CustomTheme> LoadCustomThemes()
        {
            if (!File.Exists(CustomThemesFile))
                return new List<CustomTheme>();

            try
            {
                string json = File.ReadAllText(CustomThemesFile);
                return JsonConvert.DeserializeObject<List<CustomTheme>>(json) ?? new List<CustomTheme>();
            }
            catch
            {
                return new List<CustomTheme>();
            }
        }

        public void SaveCustomThemes(List<CustomTheme> customThemes)
        {
            string json = JsonConvert.SerializeObject(customThemes, Formatting.Indented);
            File.WriteAllText(CustomThemesFile, json);
        }

        public void TogglePasswordPin(string id)
        {
            var list = LoadPasswords();
            var item = list.FirstOrDefault(p => p.Id == id);
            if (item != null)
            {
                item.IsPinned = !item.IsPinned;
                SavePasswords(list);
            }
        }

        public void ToggleLauncherPin(string id)
        {
            var list = LoadLaunchers();
            var item = list.FirstOrDefault(l => l.Id == id);
            if (item != null)
            {
                item.IsPinned = !item.IsPinned;
                SaveLaunchers(list);
            }
        }

    }
}