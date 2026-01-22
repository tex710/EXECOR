using Execor.Models;
using Execor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Execor.Windows
{
    /// <summary>
    /// Interaction logic for OverlayWindow.xaml
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private DataService _dataService;
        private Settings _settings;
        private List<OverlayProfile> _overlayProfiles;
        private OverlayProfile _currentProfile;

        // Hotkey related
        private const int HOTKEY_ID = 9000; // Unique ID for our hotkey
        private HwndSource _source;

        private WidgetManager _widgetManager; // New member

        // Auto-hide related
        private DispatcherTimer _autoHideTimer;
        private DateTime _lastActivityTime;
        private bool _isOverlayHiddenByAutoHide = false;

        private bool _isEditMode = false;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                var helper = new WindowInteropHelper(this);
                if (_isEditMode)
                {
                    // Remove WS_EX_TRANSPARENT to allow interaction
                    var extendedStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
                    SetWindowLong(helper.Handle, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
                    // Also make sure it's not WS_EX_NOACTIVATE if we want to click
                    SetWindowLong(helper.Handle, GWL_EXSTYLE, extendedStyle & ~WS_EX_NOACTIVATE);
                }
                else
                {
                    // Add WS_EX_TRANSPARENT to make it click-through again
                    var extendedStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
                    SetWindowLong(helper.Handle, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
                }
                _widgetManager.SetEditMode(_isEditMode); // Notify WidgetManager of edit mode change
            }
        }

        // Win32 API imports for making window click-through and handling hotkeys
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Win32 API imports for auto-hide functionality
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        // Window styles
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x00000020;
        const int WS_EX_TOOLWINDOW = 0x00000080; // Not to appear in taskbar
        const int WS_EX_NOACTIVATE = 0x08000000; // Not to be activated when clicked

        // Hotkey modifier keys
        const uint MOD_NONE = 0x0000;
        const uint MOD_ALT = 0x0001;
        const uint MOD_CONTROL = 0x0002;
        const uint MOD_SHIFT = 0x0004;
        const uint MOD_WIN = 0x0008;

        // Virtual key codes (partial list, others can be added as needed)
        const uint VK_F10 = 0x79; // F10 key

        // Hotkey parsing
        private (uint Modifiers, uint KeyCode) ParseHotkey(string hotkeyString)
        {
            uint modifiers = MOD_NONE;
            uint keyCode = 0;

            if (string.IsNullOrWhiteSpace(hotkeyString)) return (modifiers, keyCode);

            var parts = hotkeyString.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(p => p.Trim())
                                    .ToList();

            foreach (var part in parts)
            {
                switch (part.ToLower())
                {
                    case "ctrl":
                        modifiers |= MOD_CONTROL;
                        break;
                    case "alt":
                        modifiers |= MOD_ALT;
                        break;
                    case "shift":
                        modifiers |= MOD_SHIFT;
                        break;
                    case "win":
                        modifiers |= MOD_WIN;
                        break;
                    default:
                        // Assuming the last part is the key itself
                        // This is a simplified approach; a more robust solution would map common key names
                        // For F10 as default, we can directly assign it.
                        if (part.Equals("F10", StringComparison.OrdinalIgnoreCase))
                        {
                            keyCode = VK_F10;
                        }
                        // Add more key mappings as needed
                        break;
                }
            }
            return (modifiers, keyCode);
        }

        private void SetWindowExTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                if (wParam.ToInt32() == HOTKEY_ID)
                {
                    ToggleOverlayVisibility();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void RegisterOverlayHotkey()
        {
            UnregisterOverlayHotkey(); // Ensure any existing hotkey is unregistered

            var (modifiers, keyCode) = ParseHotkey(_settings.OverlayHotkey);
            var helper = new WindowInteropHelper(this);
            if (RegisterHotKey(helper.Handle, HOTKEY_ID, modifiers, keyCode))
            {
                // Hotkey registered successfully
            }
            else
            {
                // Handle hotkey registration failure (e.g., already in use)
                ToastService.Error("Hotkey failed", $"Failed to register overlay hotkey: {_settings.OverlayHotkey}. It might be in use by another application.");
            }
        }

        private void UnregisterOverlayHotkey()
        {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
        }

        private void ToggleOverlayVisibility()
        {
            if (Visibility == Visibility.Visible && IsEditMode)
            {
                // Currently visible and in edit mode -> go to hidden
                Visibility = Visibility.Collapsed;
                IsEditMode = false; // Ensure edit mode is off when hidden
            }
            else if (Visibility == Visibility.Visible && !IsEditMode)
            {
                // Currently visible and read-only -> go to edit mode
                IsEditMode = true;
                ToastService.Info("Overlay Edit Mode", "Widgets are now interactive.");
            }
            else // Visibility == Visibility.Collapsed
            {
                // Currently hidden -> go to visible (read-only)
                Visibility = Visibility.Visible;
                IsEditMode = false; // Start in read-only mode
                ToastService.Info("Overlay Visible", "Press hotkey again to enter edit mode.");
            }
            _settings.OverlayEnabled = (Visibility == Visibility.Visible); // Update setting based on current visibility
            _dataService.SaveSettings(_settings); // Save the new state
        }
        public OverlayWindow()
        {
            InitializeComponent();
            _dataService = new DataService();
            _settings = _dataService.LoadSettings();
            _overlayProfiles = _dataService.LoadOverlayProfiles();
            _currentProfile = _overlayProfiles.FirstOrDefault() ?? new OverlayProfile { Name = "Default" };

            // Initialize WidgetManager
            _widgetManager = new WidgetManager(OverlayCanvas, _currentProfile.Widgets);
            _widgetManager.WidgetsUpdated += SaveCurrentOverlayProfile; // Hook up event to save changes

            // Set window opacity from settings
            this.Opacity = _settings.OverlayOpacity;
            // Set window to be visible or collapsed based on settings
            this.Visibility = _settings.OverlayEnabled ? Visibility.Visible : Visibility.Collapsed;

            InitializeAutoHide(); // Initialize auto-hide logic
        }

        private void SaveCurrentOverlayProfile(List<WidgetConfig> updatedWidgets)
        {
            // Update the current profile's widgets with the latest state from WidgetManager
            _currentProfile.Widgets = updatedWidgets;
            // Find the index of the current profile in the list of all profiles
            var index = _overlayProfiles.FindIndex(p => p.Id == _currentProfile.Id);
            if (index != -1)
            {
                _overlayProfiles[index] = _currentProfile;
            }
            else
            {
                // If for some reason the current profile is not in the list, add it
                _overlayProfiles.Add(_currentProfile);
            }
            _dataService.SaveOverlayProfiles(_overlayProfiles);
            ToastService.Info("Overlay Saved", "Widget positions updated.");
        }

        public void RefreshOverlaySettings()
        {
            _settings = _dataService.LoadSettings(); // Reload settings

            // Apply opacity
            this.Opacity = _settings.OverlayOpacity;

            // Re-register hotkey in case it changed
            RegisterOverlayHotkey();

            // Update visibility based on settings (if not already handled by ToggleOverlayVisibility)
            if (_settings.OverlayEnabled && this.Visibility == Visibility.Collapsed)
            {
                this.Visibility = Visibility.Visible;
            }
            else if (!_settings.OverlayEnabled && this.Visibility == Visibility.Visible)
            {
                this.Visibility = Visibility.Collapsed;
            }

            // Start/Stop auto-hide monitoring
            if (_settings.AutoHideOverlay)
            {
                StartAutoHideMonitoring();
            }
            else
            {
                StopAutoHideMonitoring();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the window handle
            var helper = new WindowInteropHelper(this);
            _source = HwndSource.FromHwnd(helper.Handle);
            _source.AddHook(HwndHook);

            // Make window click-through
            SetWindowExTransparent(helper.Handle);

            // Register hotkey
            RegisterOverlayHotkey();

            // Load and display widgets
            _widgetManager.LoadWidgets(); // Call WidgetManager to load widgets
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _source.RemoveHook(HwndHook);
            _source = null;
            UnregisterOverlayHotkey();
        }


        public void RefreshOverlayWidgets()
        {
            _overlayProfiles = _dataService.LoadOverlayProfiles();
            _currentProfile = _overlayProfiles.FirstOrDefault() ?? new OverlayProfile { Name = "Default" };
            // Pass the updated widgets list to the widget manager
            _widgetManager = new WidgetManager(OverlayCanvas, _currentProfile.Widgets); // Re-initialize or update WidgetManager's list
            _widgetManager.WidgetsUpdated += SaveCurrentOverlayProfile; // Re-hook event
            _widgetManager.LoadWidgets(); // Reload widgets on the canvas
        }

        private void InitializeAutoHide()
        {
            _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) }; // Check every second
            _autoHideTimer.Tick += AutoHideTimer_Tick;

            if (_settings.AutoHideOverlay)
            {
                StartAutoHideMonitoring();
            }
        }

        private void StartAutoHideMonitoring()
        {
            _lastActivityTime = GetLastInputTime();
            _autoHideTimer.Start();
        }

        private void StopAutoHideMonitoring()
        {
            _autoHideTimer.Stop();
            // Ensure overlay is visible again if it was hidden by auto-hide
            if (_isOverlayHiddenByAutoHide && Visibility == Visibility.Collapsed && !IsEditMode)
            {
                Visibility = Visibility.Visible;
                _isOverlayHiddenByAutoHide = false;
            }
        }

        private void AutoHideTimer_Tick(object sender, EventArgs e)
        {
            if (IsEditMode) return; // Don't auto-hide if in edit mode

            // Check for inactivity
            DateTime currentActivityTime = GetLastInputTime();
            if (currentActivityTime != _lastActivityTime)
            {
                _lastActivityTime = currentActivityTime;
                // User is active, ensure overlay is visible if it was hidden by inactivity
                if (_isOverlayHiddenByAutoHide && Visibility == Visibility.Collapsed)
                {
                    Visibility = Visibility.Visible;
                    _isOverlayHiddenByAutoHide = false;
                }
            }
            else
            {
                // Check if inactive for too long
                if ((DateTime.Now - _lastActivityTime).TotalSeconds > _settings.AutoHideTimeoutSeconds)
                {
                    if (Visibility == Visibility.Visible)
                    {
                        Visibility = Visibility.Collapsed;
                        _isOverlayHiddenByAutoHide = true;
                    }
                }
            }

            // Check foreground window
            string foregroundProcessName = GetForegroundProcessName();
            if (_settings.AutoHideGamesList.Any(gameExe => foregroundProcessName.Equals(gameExe, StringComparison.OrdinalIgnoreCase)))
            {
                if (Visibility == Visibility.Visible)
                {
                    Visibility = Visibility.Collapsed;
                    _isOverlayHiddenByAutoHide = true;
                }
            }
            else
            {
                // If not in a hidden game, and not hidden by inactivity, ensure visible
                if (_isOverlayHiddenByAutoHide && (DateTime.Now - _lastActivityTime).TotalSeconds <= _settings.AutoHideTimeoutSeconds)
                {
                     if (Visibility == Visibility.Collapsed && !IsEditMode)
                     {
                         Visibility = Visibility.Visible;
                         _isOverlayHiddenByAutoHide = false;
                     }
                }
            }
        }

        private DateTime GetLastInputTime()
        {
            LASTINPUTINFO lii = new LASTINPUTINFO();
            lii.cbSize = (uint)Marshal.SizeOf(lii);
            if (GetLastInputInfo(ref lii))
            {
                // dwTime is in milliseconds from system boot
                // Environment.TickCount is also in milliseconds from system boot
                // Difference between Environment.TickCount and lii.dwTime gives inactivity time in ms.
                return DateTime.Now.AddMilliseconds(-(Environment.TickCount - lii.dwTime));
            }
            return DateTime.Now; // Fallback
        }

        private string GetForegroundProcessName()
        {
            IntPtr hwnd = GetForegroundWindow();
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            if (pid == 0) return string.Empty;

            try
            {
                System.Diagnostics.Process foregroundProcess = System.Diagnostics.Process.GetProcessById((int)pid);
                return foregroundProcess.ProcessName + ".exe"; // Return with .exe extension for comparison
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
