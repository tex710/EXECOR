using Execor.Models;
using Execor.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic; // Added for List
using System.Collections.ObjectModel; // Added for ObservableCollection
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input; // Added for Key and KeyInterop
using System.Windows.Interop; // Added for HwndSource
using System.Windows.Input;
using System.IO;

namespace Execor
{
    public partial class SettingsWindow : Window
    {
        private DataService dataService;
        private Settings settings;
        private List<OverlayProfile> _overlayProfiles; // All profiles
        private OverlayProfile _currentProfile; // The currently active profile

        // Property to expose widgets to XAML
        public ObservableCollection<WidgetConfig> ConfigurableWidgets { get; set; }

        // Reference to MainWindow
        private MainWindow mainWindow;

        private const string AppVersion = "v1.2.3"; // manually update version here

        // Constructor that accepts MainWindow reference
        public SettingsWindow(MainWindow mw)
        {
            InitializeComponent();
            mainWindow = mw;
            
            this.DataContext = this; // Set DataContext for binding ConfigurableWidgets

            dataService = new DataService();
            _overlayProfiles = dataService.LoadOverlayProfiles();
            // Assuming the first profile is the "current" one for now. A proper UI for profile selection is needed.
            _currentProfile = _overlayProfiles.FirstOrDefault() ?? new OverlayProfile { Name = "Default" };
            ConfigurableWidgets = new ObservableCollection<WidgetConfig>(_currentProfile.Widgets);

            LoadSettings();

            // Optional: update title immediately when opening settings
            mainWindow?.UpdateTitleVersion();
        }

        // Keep parameterless constructor if needed
        public SettingsWindow()
        {
            InitializeComponent();
            this.DataContext = this; // Set DataContext for binding ConfigurableWidgets

            dataService = new DataService();
            _overlayProfiles = dataService.LoadOverlayProfiles();
            _currentProfile = _overlayProfiles.FirstOrDefault() ?? new OverlayProfile { Name = "Default" };
            ConfigurableWidgets = new ObservableCollection<WidgetConfig>(_currentProfile.Widgets);
            
            LoadSettings();
        }

        private void LoadSettings()
        {
            settings = dataService.LoadSettings();

            LaunchOnStartupCheckBox.IsChecked = settings.LaunchOnStartup;
            MinimizeToTrayCheckBox.IsChecked = settings.MinimizeToTray;
            ClipboardAutoClearCheckBox.IsChecked = settings.ClipboardAutoClear;
            ShowVersionCheckBox.IsChecked = settings.ShowVersionInTitle;
            ShowToastCheckBox.IsChecked = settings.ShowToastNotifications;

            // Overlay Settings (global)
            OverlayEnabledCheckBox.IsChecked = settings.OverlayEnabled;
            OverlayHotkeyTextBox.Text = settings.OverlayHotkey;
            OverlayOpacitySlider.Value = settings.OverlayOpacity;

            // Auto-hide Overlay Settings
            AutoHideOverlayCheckBox.IsChecked = settings.AutoHideOverlay;
            // Select ComboBoxItem for AutoHideTimeoutComboBox
            foreach (ComboBoxItem item in AutoHideTimeoutComboBox.Items)
            {
                if (int.Parse(item.Tag.ToString()) == settings.AutoHideTimeoutSeconds)
                {
                    AutoHideTimeoutComboBox.SelectedItem = item;
                    break;
                }
            }
            AutoHideGamesTextBox.Text = string.Join(", ", settings.AutoHideGamesList);


            foreach (ComboBoxItem item in ClipboardTimeoutComboBox.Items)
            {
                if (int.Parse(item.Tag.ToString()) == settings.ClipboardTimeout)
                {
                    ClipboardTimeoutComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            settings.LaunchOnStartup = LaunchOnStartupCheckBox.IsChecked == true;
            settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked == true;
            settings.ClipboardAutoClear = ClipboardAutoClearCheckBox.IsChecked == true;
            settings.ShowVersionInTitle = ShowVersionCheckBox.IsChecked == true;
            settings.ShowToastNotifications = ShowToastCheckBox.IsChecked == true;

            // Overlay Settings (global)
            settings.OverlayEnabled = OverlayEnabledCheckBox.IsChecked == true;
            settings.OverlayHotkey = OverlayHotkeyTextBox.Text;
            settings.OverlayOpacity = OverlayOpacitySlider.Value;

            // Auto-hide Overlay Settings
            settings.AutoHideOverlay = AutoHideOverlayCheckBox.IsChecked == true;
            if (AutoHideTimeoutComboBox.SelectedItem is ComboBoxItem autoHideItem)
            {
                settings.AutoHideTimeoutSeconds = int.Parse(autoHideItem.Tag.ToString());
            }
            settings.AutoHideGamesList = AutoHideGamesTextBox.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                                         .Select(s => s.Trim())
                                                         .Where(s => !string.IsNullOrWhiteSpace(s))
                                                         .ToList();


            if (ClipboardTimeoutComboBox.SelectedItem is ComboBoxItem item)
            {
                settings.ClipboardTimeout = int.Parse(item.Tag.ToString());
            }

            dataService.SaveSettings(settings);

            // Save widget configurations
            _currentProfile.Widgets = new List<WidgetConfig>(ConfigurableWidgets); // Update current profile's widgets
            // Find the index of the current profile in the list of all profiles
            var index = _overlayProfiles.FindIndex(p => p.Id == _currentProfile.Id);
            if (index != -1)
            {
                _overlayProfiles[index] = _currentProfile;
            }
            else
            {
                _overlayProfiles.Add(_currentProfile);
            }
            dataService.SaveOverlayProfiles(_overlayProfiles); // Save all profiles


            // Refresh main window title immediately
            mainWindow?.UpdateTitleVersion();

            // Refresh OverlayWindow settings and widgets
            App.OverlayWindowInstance?.RefreshOverlaySettings();
            App.OverlayWindowInstance?.RefreshOverlayWidgets(); // New method to add to OverlayWindow

            ApplyStartupSetting();
            this.Close();
        }

        private void ApplyStartupSetting()
        {
            string appName = "Execor";
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

        // Hotkey registration for settings window for overlay hotkey input
        private int _hotkeyId; // This id is local to the settings window and not related to the global hotkey id
        private HwndSource _hotkeyHwndSource;

        private void OverlayHotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            OverlayHotkeyTextBox.Text = "Press a key combination...";
            OverlayHotkeyTextBox.IsReadOnly = true; // Prevent direct text input
            _hotkeyHwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (_hotkeyHwndSource != null)
            {
                // Hook into the message loop of the settings window itself to capture key presses
                _hotkeyHwndSource.AddHook(HotkeyHwndHook);
            }
        }

        private void OverlayHotkeyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_hotkeyHwndSource != null)
            {
                _hotkeyHwndSource.RemoveHook(HotkeyHwndHook);
                _hotkeyHwndSource = null;
            }
            if (OverlayHotkeyTextBox.Text == "Press a key combination...")
            {
                OverlayHotkeyTextBox.Text = settings.OverlayHotkey; // Revert if nothing was pressed
            }
            OverlayHotkeyTextBox.IsReadOnly = false;
        }

        private void OverlayHotkeyTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // This event is primarily used to prevent normal key input, actual hotkey capture is via HwndHook
            e.Handled = true; 
        }

        private IntPtr HotkeyHwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_KEYDOWN = 0x0100;
            const int WM_SYSKEYDOWN = 0x0104; // For Alt key

            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
            {
                Key key = KeyInterop.KeyFromVirtualKey(wParam.ToInt32());

                // Check for modifier keys
                bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                bool alt = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
                bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                bool win = (Keyboard.Modifiers & ModifierKeys.Windows) == ModifierKeys.Windows;

                // Build the hotkey string
                string hotkeyString = "";
                if (ctrl) hotkeyString += "Ctrl+";
                if (alt) hotkeyString += "Alt+";
                if (shift) hotkeyString += "Shift+";
                if (win) hotkeyString += "Win+";

                // Filter out modifier keys themselves to avoid "Ctrl+Ctrl"
                if (key != Key.LeftCtrl && key != Key.RightCtrl &&
                    key != Key.LeftAlt && key != Key.RightAlt &&
                    key != Key.LeftShift && key != Key.RightShift &&
                    key != Key.LWin && key != Key.RWin &&
                    key != Key.System) // Exclude System key (often Alt)
                {
                    hotkeyString += key.ToString();
                }

                // Ensure a valid key is captured and not just modifiers
                if (!string.IsNullOrEmpty(hotkeyString) && hotkeyString.Last() != '+')
                {
                    OverlayHotkeyTextBox.Text = hotkeyString;
                    settings.OverlayHotkey = hotkeyString; // Update setting immediately
                    // Unhook to stop capturing and allow the textbox to lose focus
                    if (_hotkeyHwndSource != null)
                    {
                        _hotkeyHwndSource.RemoveHook(HotkeyHwndHook);
                        _hotkeyHwndSource = null;
                    }
                    OverlayHotkeyTextBox.IsReadOnly = false;
                    // Programmatically unfocus the textbox
                    var scope = FocusManager.GetFocusScope(OverlayHotkeyTextBox);
                    FocusManager.SetFocusedElement(scope, null);
                    handled = true; // Mark event as handled
                }
                else if (hotkeyString.EndsWith("+") && key == Key.System) // Handle Alt + non-modifier key
                {
                     // If only modifiers are pressed and it's a System key (Alt), ignore for now
                     // This prevents just "Alt+" from being set as the hotkey
                     handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
