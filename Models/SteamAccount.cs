using System;

namespace HackHelper.Models
{
    public class SteamAccount
    {
        public string Id { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Password { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime? LastUsed { get; set; }
        public int LoginCount { get; set; } = 0;

        // Display property for UI
        public string DisplayName => $"{AccountName} ({Username})";

        public string LastUsedDisplay => LastUsed.HasValue
            ? $"Last used: {LastUsed.Value:MMM dd, yyyy}"
            : "Never used";

        public string LoginCountDisplay => $"🚀 {LoginCount} logins";
    }
}