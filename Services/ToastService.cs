using Execor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Execor.Services
{
    public static class ToastService
    {
        private static List<ToastWindow> activeToasts = new List<ToastWindow>();
        private const int MaxToasts = 5;
        private const int ToastSpacing = 100;

        public static void Show(string title, string message, ToastType type = ToastType.Info, int durationSeconds = 3, bool showProgress = false)
        {
            // Load settings
            var settings = new DataService().LoadSettings();
            if (!settings.ShowToastNotifications)
                return; // skip showing toast if disabled

            Application.Current.Dispatcher.Invoke(() =>
            {
                var notification = new ToastNotification
                {
                    Title = title,
                    Message = message,
                    Type = type,
                    DurationSeconds = durationSeconds,
                    ShowProgress = showProgress
                };

                var toast = new ToastWindow(notification);

                // Remove closed toasts from tracking
                activeToasts = activeToasts.Where(t => t.IsLoaded).ToList();

                // Limit number of toasts
                if (activeToasts.Count >= MaxToasts)
                {
                    var oldest = activeToasts.First();
                    oldest.Close();
                    activeToasts.Remove(oldest);
                }

                // Stack toasts vertically
                var workingArea = SystemParameters.WorkArea;
                var topPosition = 20;

                foreach (var existingToast in activeToasts)
                {
                    topPosition += ToastSpacing;
                }

                toast.Top = topPosition;

                activeToasts.Add(toast);
                toast.Closed += (s, e) => activeToasts.Remove(toast);

                toast.Show();
            });
        }

        // Convenience methods
        public static void Success(string title, string message = "", int duration = 3)
            => Show(title, message, ToastType.Success, duration);

        public static void Error(string title, string message = "", int duration = 5)
            => Show(title, message, ToastType.Error, duration);

        public static void Warning(string title, string message = "", int duration = 4)
            => Show(title, message, ToastType.Warning, duration);

        public static void Info(string title, string message = "", int duration = 3)
            => Show(title, message, ToastType.Info, duration);
    }
}
