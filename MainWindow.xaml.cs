using HackHelper.Models;
using HackHelper.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace HackHelper
{
    

    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
            {
                return Visibility.Collapsed; // Has icon, hide default
            }
            return Visibility.Visible; // No icon, show default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class MainWindow : Window
    {
        private DataService dataService;
        private List<Launcher> launchers;
        private List<PasswordEntry> passwords;
        private Settings settings;
        private DispatcherTimer clipboardTimer;

        // NEW: Selection tracking
        private Launcher selectedLauncher = null;
        private PasswordEntry selectedPassword = null;

        public MainWindow()
        {
            InitializeComponent();
            dataService = new DataService();
            LoadData();

            // Apply saved theme
            ThemeManager.ApplyTheme(settings.SelectedTheme);

            // Setup render transform for animations
            this.RenderTransform = new ScaleTransform(1.0, 1.0);
            this.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        private void LoadData()
        {
            launchers = dataService.LoadLaunchers();
            passwords = dataService.LoadPasswords();
            settings = dataService.LoadSettings();
            RefreshLaunchersList();
            RefreshPasswordsList();
        }

        private void RefreshLaunchersList(string searchQuery = "")
        {
            var filteredLaunchers = launchers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                filteredLaunchers = filteredLaunchers.Where(l =>
                    l.Name.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    l.ExePath.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0
                );
            }

            // NEW: Sort by pinned status first, then by name
            filteredLaunchers = filteredLaunchers.OrderByDescending(l => l.IsPinned).ThenBy(l => l.Name);

            var cs2Launchers = filteredLaunchers.Where(l => l.GameType == "CS2").ToList();
            var csgoLaunchers = filteredLaunchers.Where(l => l.GameType == "CSGO").ToList();
            var otherLaunchers = filteredLaunchers.Where(l => l.GameType == "Other").ToList();

            CS2ListBox.ItemsSource = null;
            CS2ListBox.ItemsSource = cs2Launchers;

            CSGOListBox.ItemsSource = null;
            CSGOListBox.ItemsSource = csgoLaunchers;

            OtherListBox.ItemsSource = null;
            OtherListBox.ItemsSource = otherLaunchers;
        }

        private void RefreshPasswordsList(string searchQuery = "")
        {
            var filteredPasswords = passwords.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                filteredPasswords = filteredPasswords.Where(p =>
                    p.ServiceName.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Username.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Email.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0
                );
            }

            PasswordsListBox.ItemsSource = null;
            PasswordsListBox.ItemsSource = filteredPasswords.ToList();
        }

        // SEARCH HANDLERS
        private void LauncherSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshLaunchersList(LauncherSearchBox.Text);
        }

        private void PasswordSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshPasswordsList(PasswordSearchBox.Text);
        }

        // NEW: SELECTION TRACKING
        private void Loader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Launcher launcher)
            {
                selectedLauncher = launcher;
                selectedPassword = null; // Clear password selection
                UpdateEditButtons();
            }
        }

        private void Password_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is PasswordEntry password)
            {
                selectedPassword = password;
                selectedLauncher = null; // Clear launcher selection
                UpdateEditButtons();
            }
        }

        // NEW: Update button states based on selection
        private void UpdateEditButtons()
        {
            if (selectedLauncher != null)
            {
                EditButton.IsEnabled = true;
                PinLoaderButton.IsEnabled = true;
                PinLoaderButton.Content = selectedLauncher.IsPinned ? "📌 Unpin" : "📌 Pin";
            }
            else if (selectedPassword != null)
            {
                EditButton.IsEnabled = true;
                PinLoaderButton.IsEnabled = false;
            }
            else
            {
                EditButton.IsEnabled = false;
                PinLoaderButton.IsEnabled = false;
            }
        }

        // NEW: Unified Edit button handler
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (selectedLauncher != null)
            {
                EditLauncher(selectedLauncher);
            }
            else if (selectedPassword != null)
            {
                EditPasswordEntry(selectedPassword);
            }
        }

        // NEW: Edit Launcher
        private void EditLauncher(Launcher launcher)
        {
            var dialog = new EditLauncherDialog(launcher)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                dataService.UpdateLauncher(dialog.EditedLauncher);
                launchers = dataService.LoadLaunchers(); // Reload
                RefreshLaunchersList(LauncherSearchBox.Text);
                selectedLauncher = null;
                UpdateEditButtons();

                ToastService.Success($"{dialog.EditedLauncher.Name} updated", "Changes saved");
            }
        }

        // NEW: Edit Password
        private void EditPasswordEntry(PasswordEntry password)
        {
            var dialog = new EditPasswordDialog(password)
            {
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                dataService.UpdatePassword(dialog.EditedPassword);
                passwords = dataService.LoadPasswords(); // Reload
                RefreshPasswordsList(PasswordSearchBox.Text);
                selectedPassword = null;
                UpdateEditButtons();

                ToastService.Success($"{dialog.EditedPassword.ServiceName} updated", "Password updated");
            }
        }

        // NEW: Pin/Unpin Loader
        private void PinLoader_Click(object sender, RoutedEventArgs e)
        {
            if (selectedLauncher != null)
            {
                dataService.ToggleLauncherPin(selectedLauncher.Id);
                launchers = dataService.LoadLaunchers(); // Reload
                RefreshLaunchersList(LauncherSearchBox.Text);

                string message = selectedLauncher.IsPinned ? "unpinned" : "pinned";
                ToastService.Info($"{selectedLauncher.Name} {message}", "Pin status updated");

                selectedLauncher = null;
                UpdateEditButtons();
            }
        }

        private void ApplyStartupSetting()
        {
            string appName = "LauncherLoader";
            string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (settings.LaunchOnStartup)
                {
                    key.SetValue(appName, appPath);
                }
                else
                {
                    if (key.GetValue(appName) != null)
                    {
                        key.DeleteValue(appName);
                    }
                }
            }
        }

        private void StartClipboardClearTimer()
        {
            if (!settings.ClipboardAutoClear) return;

            clipboardTimer?.Stop();
            clipboardTimer = new DispatcherTimer();
            clipboardTimer.Interval = TimeSpan.FromSeconds(settings.ClipboardTimeout);
            clipboardTimer.Tick += (s, e) =>
            {
                Clipboard.Clear();
                clipboardTimer.Stop();
            };
            clipboardTimer.Start();
        }

        // ANIMATION METHODS
        private void AnimateMinimize()
        {
            // Just scale, no fade
            var scaleDown = new DoubleAnimation
            {
                From = 1.0,
                To = 0.01,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            scaleDown.Completed += (s, e) =>
            {
                var transform = (ScaleTransform)this.RenderTransform;

                // Remove animation clocks FIRST
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, null);

                // Then reset values
                transform.ScaleX = 1.0;
                transform.ScaleY = 1.0;

                this.WindowState = WindowState.Minimized;
            };

            ((ScaleTransform)this.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, scaleDown);
            ((ScaleTransform)this.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, scaleDown);
        }

        private void AnimateClose()
        {
            // Just scale down, no fade
            var scaleDown = new DoubleAnimation
            {
                From = 1.0,
                To = 0.01,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            scaleDown.Completed += (s, e) =>
            {
                var transform = (ScaleTransform)this.RenderTransform;
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, null);

                Application.Current.Shutdown();
            };

            ((ScaleTransform)this.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, scaleDown);
            ((ScaleTransform)this.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, scaleDown);
        }

        // LAUNCHER METHODS
        private void AddLauncher_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddLauncherDialog();
            if (dialog.ShowDialog() == true)
            {
                launchers.Add(dialog.NewLauncher);
                dataService.SaveLaunchers(launchers);
                RefreshLaunchersList(LauncherSearchBox.Text);

                // TOAST notification
                ToastService.Success($"{dialog.NewLauncher.Name} added", "Loader added to library");
            }
        }

        private void LaunchLoader_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var launcher = button?.Tag as Launcher;

            if (launcher != null)
            {
                try
                {
                    launcher.LaunchCount++;

                    // Track launch timestamp
                    if (launcher.LaunchHistory == null)
                        launcher.LaunchHistory = new List<DateTime>();

                    launcher.LaunchHistory.Add(DateTime.Now);

                    dataService.SaveLaunchers(launchers);
                    RefreshLaunchersList(LauncherSearchBox.Text);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = launcher.ExePath,
                        UseShellExecute = true
                    });

                    // TOAST notification
                    ToastService.Success($"{launcher.Name} launched", "Started successfully");
                }
                catch (Exception ex)
                {
                    // TOAST for errors
                    ToastService.Error("Launch failed", ex.Message);
                }
            }
        }

        private void VisitWebsite_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var launcher = button?.Tag as Launcher;

            if (launcher != null && !string.IsNullOrWhiteSpace(launcher.Website))
            {
                try
                {
                    string url = launcher.Website;
                    if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                    {
                        url = "https://" + url;
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });

                    // TOAST notification
                    ToastService.Info("Opening website", $"Launching {launcher.Name} website");
                }
                catch (Exception ex)
                {
                    // TOAST for errors
                    ToastService.Error("Website error", ex.Message);
                }
            }
        }

        private void DeleteLauncher_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var launcher = button?.Tag as Launcher;

            if (launcher != null)
            {
                var result = MessageBox.Show($"Delete '{launcher.Name}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    launchers.Remove(launcher);
                    dataService.SaveLaunchers(launchers);
                    RefreshLaunchersList(LauncherSearchBox.Text);

                    // TOAST notification
                    ToastService.Warning($"{launcher.Name} deleted", "Removed from library");
                }
            }
        }

        // PASSWORD METHODS
        private void AddPassword_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddPasswordDialog();
            if (dialog.ShowDialog() == true)
            {
                passwords.Add(dialog.NewPassword);
                dataService.SavePasswords(passwords);
                RefreshPasswordsList(PasswordSearchBox.Text);

                // TOAST notification
                ToastService.Success($"{dialog.NewPassword.ServiceName} added", "Password saved securely");
            }
        }

        private void CopyPassword_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var password = button?.Tag as PasswordEntry;

            if (password != null)
            {
                Clipboard.SetText(password.Password);
                StartClipboardClearTimer();

                // TOAST with countdown and progress bar
                if (settings.ClipboardAutoClear)
                {
                    ToastService.Show(
                        "Password copied",
                        $"Clearing in {settings.ClipboardTimeout} seconds...",
                        ToastType.Info,
                        settings.ClipboardTimeout,
                        showProgress: true
                    );
                }
                else
                {
                    ToastService.Success("Password copied", "Ready to paste");
                }
            }
        }

        private void DeletePassword_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var password = button?.Tag as PasswordEntry;

            if (password != null)
            {
                var result = MessageBox.Show($"Delete password for '{password.ServiceName}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    passwords.Remove(password);
                    dataService.SavePasswords(passwords);
                    RefreshPasswordsList(PasswordSearchBox.Text);

                    // TOAST notification
                    ToastService.Warning($"{password.ServiceName} deleted", "Password removed");
                }
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow
            {
                Owner = this
            };
            settingsWindow.ShowDialog();

            // Reload settings after window closes in case they changed
            settings = dataService.LoadSettings();
        }

        private void Statistics_Click(object sender, RoutedEventArgs e)
        {
            var statisticsWindow = new StatisticsWindow(launchers)
            {
                Owner = this
            };
            statisticsWindow.ShowDialog();
        }

        private void ThemeEditor_Click(object sender, RoutedEventArgs e)
        {
            var themeEditorWindow = new ThemeEditorWindow
            {
                Owner = this
            };
            themeEditorWindow.ShowDialog();
        }

        // WINDOW CONTROL METHODS
        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, null);
            }
            else
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            AnimateMinimize();
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized
        ? WindowState.Normal
        : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            AnimateClose();
        }
    }
}