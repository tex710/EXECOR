using HackHelper.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace HackHelper.Services
{
    public class ThemeManager
    {
        public static List<Theme> GetAvailableThemes()
        {
            return new List<Theme>
            {
                Theme.Red,
                Theme.BluePink,
                Theme.Purple,
                Theme.Green,
                Theme.Cyan,
                Theme.Orange
            };
        }

        public static void ApplyTheme(string themeName)
        {
            // First, check default themes
            var theme = GetAvailableThemes().FirstOrDefault(t => t.Name == themeName);

            // If not found, check custom themes
            if (theme == null)
            {
                var customThemes = new DataService().LoadCustomThemes();
                var custom = customThemes.FirstOrDefault(ct => ct.Name == themeName);
                if (custom != null)
                {
                    theme = new Theme
                    {
                        Name = custom.Name,
                        PrimaryColor = custom.PrimaryColor,
                        SecondaryColor = custom.SecondaryColor,
                        BackgroundColor = custom.BackgroundColor,
                        SurfaceColor = custom.SurfaceColor,
                        BorderColor = custom.BorderColor,
                        TextColor = custom.TextColor,
                        SubTextColor = custom.SubTextColor
                    };
                }
            }

            ApplyTheme(theme ?? Theme.Red);
        }

        public static void ApplyTheme(Theme theme)
        {
            var app = Application.Current;

            // Update application resources
            app.Resources["PrimaryColor"] = (Color)ColorConverter.ConvertFromString(theme.PrimaryColor);
            app.Resources["PrimaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.PrimaryColor));

            app.Resources["SecondaryColor"] = (Color)ColorConverter.ConvertFromString(theme.SecondaryColor);
            app.Resources["SecondaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.SecondaryColor));

            app.Resources["BackgroundColor"] = (Color)ColorConverter.ConvertFromString(theme.BackgroundColor);
            app.Resources["BackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.BackgroundColor));

            app.Resources["SurfaceColor"] = (Color)ColorConverter.ConvertFromString(theme.SurfaceColor);
            app.Resources["SurfaceBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.SurfaceColor));

            app.Resources["BorderColor"] = (Color)ColorConverter.ConvertFromString(theme.BorderColor);
            app.Resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.BorderColor));

            app.Resources["TextColor"] = (Color)ColorConverter.ConvertFromString(theme.TextColor);
            app.Resources["TextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.TextColor));

            app.Resources["SubTextColor"] = (Color)ColorConverter.ConvertFromString(theme.SubTextColor);
            app.Resources["SubTextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.SubTextColor));
        }
    }
}