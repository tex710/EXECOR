using System;

namespace HackHelper.Models
{
    public enum ToastType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class ToastNotification
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public ToastType Type { get; set; }
        public int DurationSeconds { get; set; }
        public bool ShowProgress { get; set; }
        public DateTime CreatedAt { get; set; }

        public ToastNotification()
        {
            CreatedAt = DateTime.Now;
            DurationSeconds = 3;
            ShowProgress = false;
        }
    }
}