using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Execor.Models
{
    public class Settings
    {
        public bool LaunchOnStartup { get; set; } = false;              // default settings
        public bool MinimizeToTray { get; set; } = false;
        public bool ClipboardAutoClear { get; set; } = true;
        public int ClipboardTimeout { get; set; } = 10;  // in seconds
        public string SelectedTheme { get; set; } = "Red Hot (Default)";
        public bool ShowVersionInTitle { get; set; } = false; 
        public bool ShowToastNotifications { get; set; } = true;

        // Overlay Settings
        public bool OverlayEnabled { get; set; } = false;
        public string OverlayHotkey { get; set; } = "Ctrl+Shift+F10"; // Default hotkey
        public double OverlayOpacity { get; set; } = 0.8; // Default opacity

        // Auto-hide Overlay Settings
        public bool AutoHideOverlay { get; set; } = false;
        public int AutoHideTimeoutSeconds { get; set; } = 30; // 30 seconds inactivity
        public List<string> AutoHideGamesList { get; set; } = new List<string>(); // List of game EXEs

        public Settings()
        {
            LaunchOnStartup = false;
            MinimizeToTray = false;
            ClipboardAutoClear = true;
            ClipboardTimeout = 60;

            // Initialize new overlay settings
            OverlayEnabled = false;
            OverlayHotkey = "Ctrl+Shift+F10";
            OverlayOpacity = 0.8;

            // Initialize new auto-hide settings
            AutoHideOverlay = false;
            AutoHideTimeoutSeconds = 30;
            AutoHideGamesList = new List<string>();
        }
    }
}
