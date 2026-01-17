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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace HackHelper
{
    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
                return Visibility.Collapsed;
            return Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public partial class MainWindow : Window
    {
        private DataService dataService;
        private List<Launcher> launchers;
        private List<PasswordEntry> passwords;
        private Settings settings;
        private DispatcherTimer clipboardTimer;

        private SteamAccountManager steamAccountManager;
        private List<SteamAccount> steamAccounts;

        // Selection tracking
        private Launcher selectedLauncher = null;
        private PasswordEntry selectedPassword = null;
        private SteamAccount selectedSteamAccount = null;
        

        public MainWindow()
        {
            InitializeComponent();
            dataService = new DataService();
            steamAccountManager = new SteamAccountManager();
            LoadData();
            ThemeManager.ApplyTheme(settings.SelectedTheme);
            ApplyTitleVersion();
            this.RenderTransform = new ScaleTransform(1.0, 1.0);
            this.RenderTransformOrigin = new Point(0.5, 0.5);

            if (settings.MinimizeToTray)
            {
                // Delay slightly so window initializes properly
                Dispatcher.InvokeAsync(() =>
                {
                    this.WindowState = WindowState.Minimized;
                    this.ShowInTaskbar = false; // hide from taskbar
                }, DispatcherPriority.ApplicationIdle);
            }
        }

        private void LoadData()
        {
            launchers = dataService.LoadLaunchers();
            passwords = dataService.LoadPasswords();
            settings = dataService.LoadSettings();
            steamAccounts = steamAccountManager.GetAllAccounts();

            RefreshLaunchersList();
            RefreshPasswordsList();
            RefreshSteamAccountsList();
        }

        private const string AppVersion = "v1🎉"; // manual version

        private void ApplyTitleVersion()
        {
            // Load settings
            var dataService = new DataService();
            var settings = dataService.LoadSettings();

            if (settings.ShowVersionInTitle)
                TitleTextBlock.Text = $"EXECOR {AppVersion}";
            else
                TitleTextBlock.Text = "EXECOR";
        }

        public void UpdateTitleVersion()
        {
            var dataService = new DataService();
            var settings = dataService.LoadSettings();

            if (settings.ShowVersionInTitle)
                TitleTextBlock.Text = $"EXECOR {AppVersion}";
            else
                TitleTextBlock.Text = "EXECOR";
        }



        #region Refresh Lists
        private void RefreshLaunchersList(string searchQuery = "")
        {
            var q = launchers.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(searchQuery))
                q = q.Where(l => l.Name.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 l.ExePath.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0);
            q = q.OrderByDescending(l => l.IsPinned).ThenBy(l => l.Name);

            CS2ListBox.ItemsSource = q.Where(l => l.GameType == "CS2").ToList();
            CSGOListBox.ItemsSource = q.Where(l => l.GameType == "CSGO").ToList();
            OtherListBox.ItemsSource = q.Where(l => l.GameType == "Other").ToList();
        }

        private void RefreshPasswordsList(string searchQuery = "")
        {
            var q = passwords.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(searchQuery))
                q = q.Where(p => p.ServiceName.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 p.Username.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 p.Email.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0);
            q = q.OrderByDescending(p => p.IsPinned).ThenBy(p => p.ServiceName);

            PasswordsListBox.ItemsSource = q.ToList();
        }

        private void RefreshSteamAccountsList(string searchQuery = "")
        {
            var q = steamAccounts.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(searchQuery))
                q = q.Where(a => a.AccountName.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 a.Username.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) >= 0);
            q = q.OrderByDescending(a => a.IsPinned).ThenBy(a => a.AccountName);

            SteamAccountsListBox.ItemsSource = q.ToList();
        }
        #endregion

        #region Search Boxes
        private void LauncherSearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => RefreshLaunchersList(LauncherSearchBox.Text);

        private void PasswordSearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => RefreshPasswordsList(PasswordSearchBox.Text);

        private void SteamAccountSearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => RefreshSteamAccountsList(SteamAccountSearchBox.Text);
        #endregion

        #region Selection
        private void Loader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as Border)?.DataContext is Launcher l)
            {
                selectedLauncher = l; selectedPassword = null; selectedSteamAccount = null;
                UpdateEditButtons();
            }
        }

        private void Password_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as Border)?.DataContext is PasswordEntry p)
            {
                selectedPassword = p; selectedLauncher = null; selectedSteamAccount = null;
                UpdateEditButtons();
            }
        }

        private void SteamAccount_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as Border)?.DataContext is SteamAccount a)
            {
                selectedSteamAccount = a; selectedLauncher = null; selectedPassword = null;
                UpdateEditButtons();
            }
        }
        #endregion

        #region Pin / Unpin  (UNIFIED)
        private void PinItem_Click(object sender, RoutedEventArgs e)
        {
            if (selectedLauncher != null)
            {
                dataService.ToggleLauncherPin(selectedLauncher.Id);
                launchers = dataService.LoadLaunchers();
                RefreshLaunchersList(LauncherSearchBox.Text);

                var updated = launchers.First(l => l.Id == selectedLauncher.Id);
                ToastService.Info($"{updated.Name} {(updated.IsPinned ? "pinned" : "unpinned")}", "Pin status updated");
            }
            else if (selectedPassword != null)
            {
                dataService.TogglePasswordPin(selectedPassword.Id);
                passwords = dataService.LoadPasswords();
                RefreshPasswordsList(PasswordSearchBox.Text);

                var updated = passwords.First(p => p.Id == selectedPassword.Id);
                ToastService.Info($"{updated.ServiceName} {(updated.IsPinned ? "pinned" : "unpinned")}", "Pin status updated");
            }
            else if (selectedSteamAccount != null)
            {
                steamAccountManager.TogglePin(selectedSteamAccount.Id);
                steamAccounts = steamAccountManager.GetAllAccounts();
                RefreshSteamAccountsList(SteamAccountSearchBox.Text);

                var updated = steamAccounts.First(a => a.Id == selectedSteamAccount.Id);
                ToastService.Info($"{updated.AccountName} {(updated.IsPinned ? "pinned" : "unpinned")}", "Pin status updated");
            }

            ClearSelection();
        }
        #endregion

        #region Buttons
        private void UpdateEditButtons()
        {
            bool anything = selectedLauncher != null || selectedPassword != null || selectedSteamAccount != null;
            EditButton.IsEnabled = anything;
            PinLoaderButton.IsEnabled = anything;

            if (selectedLauncher != null)
                PinLoaderButton.Content = selectedLauncher.IsPinned ? "📌 Unpin" : "📌 Pin";
            else if (selectedPassword != null)
                PinLoaderButton.Content = selectedPassword.IsPinned ? "📌 Unpin" : "📌 Pin";
            else if (selectedSteamAccount != null)
                PinLoaderButton.Content = selectedSteamAccount.IsPinned ? "📌 Unpin" : "📌 Pin";
            else
                PinLoaderButton.Content = "📌 Pin";
        }

        public void UpdateRPCStatus(bool isEnabled)
        {
            Dispatcher.Invoke(() =>
            {
                // Find the ellipse in the button's template
                var ellipse = FindVisualChild<Ellipse>(DiscordRPCButton);
                if (ellipse != null)
                {
                    if (isEnabled)
                    {
                        ellipse.Fill = new SolidColorBrush(Color.FromRgb(67, 181, 129)); // Green
                    }
                    else
                    {
                        ellipse.Fill = new SolidColorBrush(Color.FromRgb(128, 128, 128)); // Gray
                    }
                }
            });
        }

        private void ClearSelection()
        {
            selectedLauncher = null; selectedPassword = null; selectedSteamAccount = null;
            UpdateEditButtons();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (selectedLauncher != null) EditLauncher(selectedLauncher);
            else if (selectedPassword != null) EditPasswordEntry(selectedPassword);
            else if (selectedSteamAccount != null) EditSteamAccount(selectedSteamAccount);
        }
        #endregion

        #region Edit Helpers
        private void EditLauncher(Launcher l)
        {
            var d = new AddLauncherDialog(l) { Owner = this };
            if (d.ShowDialog() == true && d.NewLauncher != null)
            {
                dataService.UpdateLauncher(d.NewLauncher);
                launchers = dataService.LoadLaunchers();
                RefreshLaunchersList(LauncherSearchBox.Text);
                ClearSelection();
                ToastService.Success($"{d.NewLauncher.Name} updated", "Changes saved");
            }
        }

        private void EditPasswordEntry(PasswordEntry p)
        {
            var d = new AddPasswordDialog(p) { Owner = this };
            if (d.ShowDialog() == true && d.NewPassword != null)
            {
                dataService.UpdatePassword(d.NewPassword);
                passwords = dataService.LoadPasswords();
                RefreshPasswordsList(PasswordSearchBox.Text);
                ClearSelection();
                ToastService.Success($"{d.NewPassword.ServiceName} updated", "Password updated");
            }
        }

        private void EditSteamAccount(SteamAccount a)
        {
            var d = new AddSteamAccountWindow(a) { Owner = this };
            if (d.ShowDialog() == true && d.NewAccount != null)
            {
                steamAccountManager.UpdateAccount(d.NewAccount);
                steamAccounts = steamAccountManager.GetAllAccounts();
                RefreshSteamAccountsList(SteamAccountSearchBox.Text);
                ClearSelection();
                ToastService.Success($"{d.NewAccount.AccountName} updated", "Account updated");
            }
        }
        #endregion

        #region Clipboard
        private void StartClipboardClearTimer()
        {
            if (!settings.ClipboardAutoClear) return;
            clipboardTimer?.Stop();
            clipboardTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(settings.ClipboardTimeout) };
            clipboardTimer.Tick += (_, __) => { Clipboard.Clear(); clipboardTimer.Stop(); };
            clipboardTimer.Start();
        }
        #endregion

        #region Window Controls
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) MaximizeButton_Click(sender, null);
            else this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => AnimateMinimize();

        private void MaximizeButton_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void CloseButton_Click(object sender, RoutedEventArgs e) => AnimateClose();

        private void CustomResizeGrip_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            Width = Math.Max(MinWidth, Width + e.HorizontalChange);
            Height = Math.Max(MinHeight, Height + e.VerticalChange);
        }
        #endregion

        #region Animations
        private void AnimateMinimize()
        {
            var anim = new DoubleAnimation(1.0, 0.01, TimeSpan.FromMilliseconds(150))
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            anim.Completed += (_, __) =>
            {
                var t = (ScaleTransform)RenderTransform;
                t.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                t.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                t.ScaleX = t.ScaleY = 1.0;
                WindowState = WindowState.Minimized;
            };
            ((ScaleTransform)RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            ((ScaleTransform)RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        private void AnimateClose()
        {
            var anim = new DoubleAnimation(1.0, 0.01, TimeSpan.FromMilliseconds(200))
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            anim.Completed += (_, __) => Application.Current.Shutdown();
            ((ScaleTransform)RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            ((ScaleTransform)RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }
        #endregion

        #region Add / Launch / Delete Buttons (unchanged)
        private void AddLauncher_Click(object sender, RoutedEventArgs e)
        {
            var d = new AddLauncherDialog { Owner = this };
            if (d.ShowDialog() == true) { launchers.Add(d.NewLauncher); dataService.SaveLaunchers(launchers); RefreshLaunchersList(); ToastService.Success($"{d.NewLauncher.Name} added", "Loader added"); }
        }
        private void LaunchLoader_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Launcher l)
            {
                try
                {
                    l.LaunchCount++;
                    l.LaunchHistory ??= new List<DateTime>();
                    l.LaunchHistory.Add(DateTime.Now);
                    dataService.SaveLaunchers(launchers);
                    RefreshLaunchersList();
                    Process.Start(new ProcessStartInfo { FileName = l.ExePath, UseShellExecute = true });
                    ToastService.Success($"{l.Name} launched", "Started");
                }
                catch (Exception ex) { ToastService.Error("Launch failed", ex.Message); }
            }
        }
        private void VisitWebsite_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Launcher l && !string.IsNullOrWhiteSpace(l.Website))
            {
                try
                {
                    var url = l.Website.StartsWith("http") ? l.Website : "https://" + l.Website;
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                    ToastService.Info("Opening website", $"{l.Name} website");
                }
                catch (Exception ex) { ToastService.Error("Website error", ex.Message); }
            }
        }
        private void DeleteLauncher_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Launcher l && MessageBox.Show($"Delete '{l.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                launchers.Remove(l); dataService.SaveLaunchers(launchers); RefreshLaunchersList();
                ToastService.Warning($"{l.Name} deleted", "Removed");
            }
        }
        private void AddPassword_Click(object sender, RoutedEventArgs e)
        {
            var d = new AddPasswordDialog { Owner = this };
            if (d.ShowDialog() == true) { passwords.Add(d.NewPassword); dataService.SavePasswords(passwords); RefreshPasswordsList(); ToastService.Success($"{d.NewPassword.ServiceName} added", "Password saved"); }
        }
        private void CopyPassword_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is PasswordEntry p)
            {
                Clipboard.SetText(p.Password); StartClipboardClearTimer();
                ToastService.Show("Password copied", settings.ClipboardAutoClear ? $"Clearing in {settings.ClipboardTimeout}s…" : "Ready to paste",
                                  ToastType.Info, settings.ClipboardAutoClear ? settings.ClipboardTimeout : 2, showProgress: settings.ClipboardAutoClear);
            }
        }
        private void DeletePassword_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is PasswordEntry p && MessageBox.Show($"Delete password for '{p.ServiceName}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                passwords.Remove(p); dataService.SavePasswords(passwords); RefreshPasswordsList();
                ToastService.Warning($"{p.ServiceName} deleted", "Password removed");
            }
        }
        private void AddSteamAccount_Click(object sender, RoutedEventArgs e)
        {
            var d = new AddSteamAccountWindow { Owner = this };
            if (d.ShowDialog() == true) { steamAccountManager.AddAccount(d.NewAccount); steamAccounts = steamAccountManager.GetAllAccounts(); RefreshSteamAccountsList(); ToastService.Success($"{d.NewAccount.AccountName} added", "Account saved"); }
        }
        private void CopySteamUsername_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SteamAccount a) { Clipboard.SetText(a.Username); TempButtonCheck(sender as Button); }
        }
        private void CopySteamPassword_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SteamAccount a)
            {
                if (string.IsNullOrEmpty(a.Password)) { MessageBox.Show("No password saved.", "No Password", MessageBoxButton.OK, MessageBoxImage.Information); return; }
                Clipboard.SetText(a.Password); TempButtonCheck(sender as Button);
            }
        }
        private async void SwitchSteamAccount_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SteamAccount a)
            {
                var btn = sender as Button; btn.IsEnabled = false;
                ToastService.Info("Switching account…", "Please wait");
                await Task.Run(() => SteamRegistryManager.PerformFullAccountSwitch(a.Username, a.Password, launchSteam: true));
                steamAccountManager.RecordAccountUsage(a.Id);
                steamAccounts = steamAccountManager.GetAllAccounts(); RefreshSteamAccountsList();
                ToastService.Success($"Switched to {a.AccountName}", "Steam is launching");
                btn.IsEnabled = true;
            }
        }
        private void DeleteSteamAccount_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SteamAccount a && MessageBox.Show($"Delete Steam account '{a.AccountName}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                steamAccountManager.DeleteAccount(a.Id); steamAccounts = steamAccountManager.GetAllAccounts(); RefreshSteamAccountsList();
                ToastService.Warning($"{a.AccountName} deleted", "Account removed");
            }
        }
        #endregion

        #region Misc Buttons
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            // Pass 'this' to SettingsWindow so it can refresh title in real time
            var settingsWindow = new SettingsWindow(this)
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }

        private void Statistics_Click(object sender, RoutedEventArgs e)
            => new StatisticsWindow(launchers) { Owner = this }.ShowDialog();

        private void ThemeEditor_Click(object sender, RoutedEventArgs e)
            => new ThemeEditorWindow { Owner = this }.ShowDialog();

        private void RPCWindow_Click(object sender, RoutedEventArgs e)
            => new DiscordRPCWindow { Owner = this }.ShowDialog();

        private void SaleTracker_Click(object sender, RoutedEventArgs e)
            => new SaleTrackerWindow { Owner = this }.ShowDialog();
        #endregion

        #region Helper
        private async void TempButtonCheck(Button btn)
        {
            var orig = btn.Content; btn.Content = "✓";
            await Task.Delay(1000);
            btn.Content = orig;
        }

        // Helper method to find child elements in visual tree
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
        #endregion
    }
}