using System.Windows;
using System.Linq;
using HackHelper.Models;
using Microsoft.Win32;

namespace HackHelper
{
    public partial class EditLauncherDialog : Window
    {
        public Launcher EditedLauncher { get; private set; }
        private Launcher originalLauncher;

        public EditLauncherDialog(Launcher launcher)
        {
            InitializeComponent();
            originalLauncher = launcher;
            LoadLauncherData();
        }

        private void LoadLauncherData()
        {
            NameTextBox.Text = originalLauncher.Name;
            PathTextBox.Text = originalLauncher.ExePath;
            WebsiteTextBox.Text = originalLauncher.Website;

            // Set the correct game type
            foreach (System.Windows.Controls.ComboBoxItem item in GameTypeComboBox.Items)
            {
                if (item.Tag.ToString() == originalLauncher.GameType)
                {
                    GameTypeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                string.IsNullOrWhiteSpace(PathTextBox.Text))
            {
                MessageBox.Show("Please fill in Loader Name and Executable Path!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create updated launcher with same ID and launch history
            EditedLauncher = new Launcher
            {
                Id = originalLauncher.Id,
                Name = NameTextBox.Text,
                ExePath = PathTextBox.Text,
                IconBase64 = originalLauncher.IconBase64,
                Website = WebsiteTextBox.Text,
                GameType = (GameTypeComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag.ToString() ?? "CS2",
                LaunchCount = originalLauncher.LaunchCount,
                LaunchHistory = originalLauncher.LaunchHistory,
                IsPinned = originalLauncher.IsPinned
            };

            DialogResult = true;
            Close();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Loader Executable"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                PathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}