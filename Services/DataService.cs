using Execor.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace Execor.Services
{
    public class DataService
    {
        private static readonly string LaunchersFile = Path.Combine(PathManager.AppDataFolder, "launchers.json");
        private static readonly string PasswordsFile = Path.Combine(PathManager.AppDataFolder, "passwords.dat");
        private static readonly string SettingsFile = Path.Combine(PathManager.AppDataFolder, "settings.json");
                private static readonly string CustomThemesFile = Path.Combine(PathManager.AppDataFolder, "customThemes.json");
                private static readonly string OverlayProfilesFile = Path.Combine(PathManager.AppDataFolder, "overlayProfiles.json"); // New file for overlay profiles
                
        
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
        
                // Overlay Profile methods
                public List<OverlayProfile> LoadOverlayProfiles()
                {
                    if (!File.Exists(OverlayProfilesFile))
                    {
                                        // Create a default profile if none exists
                                        var defaultProfile = new OverlayProfile { Name = "Default" };
                                        // Optionally, add some default widgets here
                                        defaultProfile.Widgets.Add(new WidgetConfig { WidgetType = "TimeDate", Position = new Point(50, 50), Size = new Size(200, 100) }); // Add a default TimeDate widget
                defaultProfile.Widgets.Add(new WidgetConfig { WidgetType = "SystemMetrics", Position = new Point(50, 200), Size = new Size(250, 200) }); // Add a default SystemMetrics widget
                defaultProfile.Widgets.Add(new WidgetConfig { WidgetType = "MediaInfo", Position = new Point(50, 420), Size = new Size(300, 150) }); // Add a default MediaInfo widget
                defaultProfile.Widgets.Add(new WidgetConfig { WidgetType = "CustomMessage", Position = new Point(50, 600), Size = new Size(200, 50), CustomMessage = "Hello, Execor!" }); // Add a default CustomMessage widget
                defaultProfile.Widgets.Add(new WidgetConfig { WidgetType = "SessionDuration", Position = new Point(50, 670), Size = new Size(180, 70) }); // Add a default SessionDuration widget
                                        var profiles = new List<OverlayProfile> { defaultProfile };
                                        SaveOverlayProfiles(profiles);
                                        return profiles;                    }
        
                    try
                    {
                        string json = File.ReadAllText(OverlayProfilesFile);
                        return JsonConvert.DeserializeObject<List<OverlayProfile>>(json) ?? new List<OverlayProfile>();
                    }
                    catch (Exception ex)
                    {
                        // Log or handle error, return default in case of corruption
                        Console.WriteLine($"Error loading overlay profiles: {ex.Message}");
                        return new List<OverlayProfile> { new OverlayProfile { Name = "Default" } };
                    }
                }
        
                public void SaveOverlayProfiles(List<OverlayProfile> profiles)
                {
                    string json = JsonConvert.SerializeObject(profiles, Formatting.Indented);
                    File.WriteAllText(OverlayProfilesFile, json);
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
        