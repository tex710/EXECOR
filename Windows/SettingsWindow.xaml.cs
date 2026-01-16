using HackHelper.Models;
using HackHelper.Services;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HackHelper
{
    public partial class SettingsWindow : Window
    {
        private DataService dataService;
        private Settings settings;

        public SettingsWindow()
        {
            InitializeComponent();
            dataService = new DataService();
            LoadSettings();
        }

        private void LoadSettings()
        {
            settings = dataService.LoadSettings();

            LaunchOnStartupCheckBox.IsChecked = settings.LaunchOnStartup;
            MinimizeToTrayCheckBox.IsChecked = settings.MinimizeToTray;
            ClipboardAutoClearCheckBox.IsChecked = settings.ClipboardAutoClear;

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

            if (ClipboardTimeoutComboBox.SelectedItem is ComboBoxItem item)
            {
                settings.ClipboardTimeout = int.Parse(item.Tag.ToString());
            }

            

            dataService.SaveSettings(settings);
            ApplyStartupSetting();

           
            this.Close();
        }

        private void ApplyStartupSetting()
        {
            string appName = "HackHelper";
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

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        

    }
}