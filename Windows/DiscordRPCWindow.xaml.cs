using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DiscordRPC;
using DiscordRPC.Logging;

namespace Execor
{
    public partial class DiscordRPCWindow : Window
    {
        private DiscordRpcClient client;
        private DateTime startTime;
        private const string CLIENT_ID = "1461939653795119367";

        public DiscordRPCWindow()
        {
            InitializeComponent();
            startTime = DateTime.UtcNow;
            LoadSettings();

            // Initialize main window indicator
            UpdateMainWindowStatus();
        }

        private void InitializeClient()
        {
            try
            {
                // Initialize the client
                client = new DiscordRpcClient(CLIENT_ID);

                // Set up event handlers
                client.OnReady += (sender, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        UpdateStatus(true, "Connected");
                        UpdateMainWindowStatus();
                    });
                };

                client.OnConnectionFailed += (sender, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        UpdateStatus(false, "Connection Failed");
                        UpdateMainWindowStatus();
                        MessageBox.Show("Failed to connect to Discord. Make sure Discord is running.",
                            "Connection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                };

                client.OnError += (sender, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        UpdateStatus(false, "Error");
                        UpdateMainWindowStatus();
                    });
                };

                client.OnClose += (sender, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        UpdateStatus(false, "Disconnected");
                        UpdateMainWindowStatus();
                    });
                };

                // Set logger (optional)
                client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

                // Connect
                client.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Discord RPC: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus(false, "Error");
                UpdateMainWindowStatus();
            }
        }

        private void UpdateStatus(bool connected, string status)
        {
            StatusText.Text = status;

            if (connected)
            {
                StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(67, 181, 129)); // Green
            }
            else
            {
                StatusIndicator.Fill = new SolidColorBrush(Color.FromRgb(128, 128, 128)); // Gray
            }
        }

        private void UpdateMainWindowStatus()
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.UpdateRPCStatus(EnableRPCCheckBox.IsChecked.GetValueOrDefault() &&
                                          client != null &&
                                          client.IsInitialized);
            }
        }

        private void UpdatePresence_Click(object sender, RoutedEventArgs e)
        {
            if (!EnableRPCCheckBox.IsChecked.GetValueOrDefault())
            {
                MessageBox.Show("Please enable Discord Rich Presence first.",
                    "RPC Disabled", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (client == null || !client.IsInitialized)
            {
                MessageBox.Show("Not connected to Discord. Please enable RPC first.",
                    "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var presence = new RichPresence();

                // Set details and state
                if (!string.IsNullOrWhiteSpace(DetailsTextBox.Text))
                    presence.Details = DetailsTextBox.Text;

                if (!string.IsNullOrWhiteSpace(StateTextBox.Text))
                    presence.State = StateTextBox.Text;

                // Set images
                presence.Assets = new Assets();

                if (!string.IsNullOrWhiteSpace(LargeImageKeyTextBox.Text))
                {
                    presence.Assets.LargeImageKey = LargeImageKeyTextBox.Text;

                    if (!string.IsNullOrWhiteSpace(LargeImageTextTextBox.Text))
                        presence.Assets.LargeImageText = LargeImageTextTextBox.Text;
                }

                if (!string.IsNullOrWhiteSpace(SmallImageKeyTextBox.Text))
                {
                    presence.Assets.SmallImageKey = SmallImageKeyTextBox.Text;

                    if (!string.IsNullOrWhiteSpace(SmallImageTextTextBox.Text))
                        presence.Assets.SmallImageText = SmallImageTextTextBox.Text;
                }

                // Set timestamp if enabled
                if (ShowElapsedTimeCheckBox.IsChecked.GetValueOrDefault())
                {
                    presence.Timestamps = new Timestamps()
                    {
                        Start = startTime
                    };
                }

                // Update the presence
                client.SetPresence(presence);

                MessageBox.Show("Rich Presence updated successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating presence: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearPresence_Click(object sender, RoutedEventArgs e)
        {
            if (client != null && client.IsInitialized)
            {
                try
                {
                    client.ClearPresence();
                    MessageBox.Show("Rich Presence cleared!",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error clearing presence: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EnableRPCCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (EnableRPCCheckBox.IsChecked.GetValueOrDefault())
            {
                // Enable RPC
                InitializeClient();
                startTime = DateTime.UtcNow; // Reset start time
            }
            else
            {
                // Disable RPC
                if (client != null)
                {
                    try
                    {
                        client.ClearPresence();
                        client.Dispose();
                        client = null;
                        UpdateStatus(false, "Disconnected");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error disconnecting: {ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            UpdateMainWindowStatus();
            SaveSettings();
        }

        private void LoadSettings()
        {
            // Load saved settings from your configuration
            // Example using Properties.Settings (you'll need to add these to your project):
            /*
            EnableRPCCheckBox.IsChecked = Properties.Settings.Default.EnableRPC;
            DetailsTextBox.Text = Properties.Settings.Default.RPCDetails;
            StateTextBox.Text = Properties.Settings.Default.RPCState;
            LargeImageKeyTextBox.Text = Properties.Settings.Default.RPCLargeImageKey;
            LargeImageTextTextBox.Text = Properties.Settings.Default.RPCLargeImageText;
            SmallImageKeyTextBox.Text = Properties.Settings.Default.RPCSmallImageKey;
            SmallImageTextTextBox.Text = Properties.Settings.Default.RPCSmallImageText;
            ShowElapsedTimeCheckBox.IsChecked = Properties.Settings.Default.RPCShowElapsedTime;
            */
        }

        private void SaveSettings()
        {
            // Save settings to your configuration
            // Example using Properties.Settings:
            /*
            Properties.Settings.Default.EnableRPC = EnableRPCCheckBox.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.RPCDetails = DetailsTextBox.Text;
            Properties.Settings.Default.RPCState = StateTextBox.Text;
            Properties.Settings.Default.RPCLargeImageKey = LargeImageKeyTextBox.Text;
            Properties.Settings.Default.RPCLargeImageText = LargeImageTextTextBox.Text;
            Properties.Settings.Default.RPCSmallImageKey = SmallImageKeyTextBox.Text;
            Properties.Settings.Default.RPCSmallImageText = SmallImageTextTextBox.Text;
            Properties.Settings.Default.RPCShowElapsedTime = ShowElapsedTimeCheckBox.IsChecked.GetValueOrDefault();
            Properties.Settings.Default.Save();
            */
        }

        #region Window Controls

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Clean up the client when window closes
            if (client != null)
            {
                try
                {
                    client.Dispose();
                }
                catch { }
            }
        }

        #endregion
    }
}