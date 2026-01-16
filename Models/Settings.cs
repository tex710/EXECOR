using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackHelper.Models
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

        public Settings()
        {
            LaunchOnStartup = false;
            MinimizeToTray = false;
            ClipboardAutoClear = true;
            ClipboardTimeout = 60;
        }
    }
}
