using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackHelper.Models
{
    public class Settings
    {
        public bool LaunchOnStartup { get; set; }
        public bool MinimizeToTray { get; set; }
        public bool ClipboardAutoClear { get; set; }
        public int ClipboardTimeout { get; set; } // in seconds
        public string SelectedTheme { get; set; } = "Blue & Pink (Default)";
        public Settings()
        {
            LaunchOnStartup = false;
            MinimizeToTray = false;
            ClipboardAutoClear = true;
            ClipboardTimeout = 60;
        }
    }
}
