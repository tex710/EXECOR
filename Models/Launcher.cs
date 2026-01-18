using System;
using System.Collections.Generic;

namespace Execor.Models
{
    public class Launcher
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ExePath { get; set; }
        public string IconBase64 { get; set; }
        public string Website { get; set; }
        public string GameType { get; set; } // "CS2", "CSGO", or "Other"
        public int LaunchCount { get; set; }
        public bool IsPinned { get; set; } // NEW: Pinning support

        // Track launch history with timestamps
        public List<DateTime> LaunchHistory { get; set; }

        // Helper property for last launch time
        public DateTime? LastLaunchTime => LaunchHistory != null && LaunchHistory.Count > 0
            ? LaunchHistory[LaunchHistory.Count - 1]
            : (DateTime?)null;

        public Launcher()
        {
            Id = Guid.NewGuid().ToString();
            LaunchCount = 0;
            GameType = "CS2"; // Default to CS2
            LaunchHistory = new List<DateTime>(); // Initialize launch history
            IsPinned = false; // Default not pinned
        }
    }
}