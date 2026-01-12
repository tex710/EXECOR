using System.Windows;
using Microsoft.Win32;
using HackHelper.Services;
using HackHelper.Models;

namespace HackHelper
{
    public partial class AddLauncherDialog : Window
    {
        public Launcher NewLauncher { get; private set; }

        public AddLauncherDialog()
        {
            InitializeComponent();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Launcher Executable"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                PathTextBox.Text = openFileDialog.FileName;

                // Auto-fill name if empty
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    NameTextBox.Text = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                }
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text) || string.IsNullOrWhiteSpace(PathTextBox.Text))
            {
                MessageBox.Show("Please fill in all fields!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string iconData = IconExtractor.SaveIconAsBase64(PathTextBox.Text);

            // Determine game type based on selection
            string gameType = "CS2";
            if (GameTypeComboBox.SelectedIndex == 1)
                gameType = "CSGO";
            else if (GameTypeComboBox.SelectedIndex == 2)
                gameType = "Other";

            NewLauncher = new Launcher
            {
                Name = NameTextBox.Text,
                ExePath = PathTextBox.Text,
                IconBase64 = iconData,
                Website = WebsiteTextBox.Text,
                GameType = gameType
            };

            DialogResult = true;
            Close();
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