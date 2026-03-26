using System;

namespace Execor.Models
{
    public class AccountTag
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Label { get; set; } = string.Empty;
        public string Color { get; set; } = "#4CAF50"; // default green
        public DateTime? ExpiresAt { get; set; }

        public bool HasCountdown => ExpiresAt.HasValue;
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.Now;

        public string CountdownDisplay
        {
            get
            {
                if (!ExpiresAt.HasValue) return string.Empty;
                var remaining = ExpiresAt.Value - DateTime.Now;
                if (remaining <= TimeSpan.Zero) return "Expired";
                if (remaining.TotalDays >= 1) return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
                if (remaining.TotalHours >= 1) return $"{remaining.Hours}h {remaining.Minutes}m";
                return $"{remaining.Minutes}m {remaining.Seconds}s";
            }
        }

        // For badge display in the UI
        public string BadgeText => HasCountdown ? $"{Label}  ⏱ {CountdownDisplay}" : Label;
    }
}
