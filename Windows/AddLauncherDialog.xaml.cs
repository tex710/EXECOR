using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using HackHelper.Services;
using HackHelper.Models;

namespace HackHelper
{
    public partial class AddLauncherDialog : Window
    {
        public Launcher NewLauncher { get; private set; }
        private bool _isEditMode = false;
        private Launcher _editingLauncher = null;

        // Constructor for Add mode
        public AddLauncherDialog()
        {
            InitializeComponent();
        }

        // Constructor for Edit mode
        public AddLauncherDialog(Launcher launcher) : this()
        {
            _isEditMode = true;
            _editingLauncher = launcher;

            Title = "Edit Loader";
            TitleTextBlock.Text = "✏️ EDIT LOADER";
            ActionButton.Content = "💾 Save Changes";

            // Populate fields
            NameTextBox.Text = launcher.Name;
            PathTextBox.Text = launcher.ExePath;
            WebsiteTextBox.Text = launcher.Website ?? string.Empty;

            // Set game type
            switch (launcher.GameType)
            {
                case "CS2":
                    GameTypeComboBox.SelectedIndex = 0;
                    break;
                case "CSGO":
                    GameTypeComboBox.SelectedIndex = 1;
                    break;
                case "Other":
                    GameTypeComboBox.SelectedIndex = 2;
                    break;
                default:
                    GameTypeComboBox.SelectedIndex = 0;
                    break;
            }
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
                    NameTextBox.Text = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                }
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(NameTextBox.Text) || string.IsNullOrWhiteSpace(PathTextBox.Text))
            {
                MessageBox.Show("Please fill in all required fields!", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(PathTextBox.Text))
            {
                MessageBox.Show("The selected file does not exist.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Determine game type based on selection
            string gameType = "CS2";
            if (GameTypeComboBox.SelectedIndex == 1)
                gameType = "CSGO";
            else if (GameTypeComboBox.SelectedIndex == 2)
                gameType = "Other";

            // Create or update launcher
            if (_isEditMode && _editingLauncher != null)
            {
                // Update existing launcher
                _editingLauncher.Name = NameTextBox.Text.Trim();
                _editingLauncher.ExePath = PathTextBox.Text.Trim();
                _editingLauncher.Website = string.IsNullOrWhiteSpace(WebsiteTextBox.Text)
                    ? null
                    : WebsiteTextBox.Text.Trim();
                _editingLauncher.GameType = gameType;

                // Update icon if path changed
                if (_editingLauncher.ExePath != PathTextBox.Text.Trim())
                {
                    _editingLauncher.IconBase64 = IconExtractor.SaveIconAsBase64(PathTextBox.Text);
                }

                NewLauncher = _editingLauncher;
            }
            else
            {
                // Create new launcher
                string iconData = IconExtractor.SaveIconAsBase64(PathTextBox.Text);

                NewLauncher = new Launcher
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = NameTextBox.Text.Trim(),
                    ExePath = PathTextBox.Text.Trim(),
                    IconBase64 = iconData,
                    Website = string.IsNullOrWhiteSpace(WebsiteTextBox.Text)
                        ? null
                        : WebsiteTextBox.Text.Trim(),
                    GameType = gameType,
                    LaunchCount = 0,
                    IsPinned = false
                };
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }
    }
}