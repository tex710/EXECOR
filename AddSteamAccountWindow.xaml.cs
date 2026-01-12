using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HackHelper.Models;

namespace HackHelper
{
    public partial class AddSteamAccountWindow : Window
    {
        public SteamAccount? NewAccount { get; private set; }
        private bool _isEditMode = false;
        private SteamAccount? _editingAccount = null;

        public AddSteamAccountWindow()
        {
            InitializeComponent();
        }

        // For editing existing accounts
        public AddSteamAccountWindow(SteamAccount account) : this()
        {
            _isEditMode = true;
            _editingAccount = account;
            Title = "Edit Steam Account";
            AccountNameTextBox.Text = account.AccountName;
            UsernameTextBox.Text = account.Username;
            PasswordBox.Password = account.Password ?? string.Empty;
        }

        // Called after InitializeComponent in edit mode
        public void SetEditMode()
        {
            Title = "Edit Steam Account";
            // Find the button by walking the visual tree
            var contentGrid = (Grid)Content;
            var mainBorder = (Border)contentGrid.Children[1];
            var stackPanel = (StackPanel)mainBorder.Child;
            var buttonGrid = (Grid)stackPanel.Children[4];
            var addButton = (Button)buttonGrid.Children[1];
            addButton.Content = "Save Changes";
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(AccountNameTextBox.Text))
            {
                MessageBox.Show("Please enter an account name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AccountNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                MessageBox.Show("Please enter a Steam username.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameTextBox.Focus();
                return;
            }

            // Create or update account
            if (_isEditMode && _editingAccount != null)
            {
                // Update existing account
                _editingAccount.AccountName = AccountNameTextBox.Text.Trim();
                _editingAccount.Username = UsernameTextBox.Text.Trim();
                _editingAccount.Password = string.IsNullOrWhiteSpace(PasswordBox.Password) ? null : PasswordBox.Password;
                NewAccount = _editingAccount;
            }
            else
            {
                // Create new account
                NewAccount = new SteamAccount
                {
                    Id = Guid.NewGuid().ToString(),
                    AccountName = AccountNameTextBox.Text.Trim(),
                    Username = UsernameTextBox.Text.Trim(),
                    Password = string.IsNullOrWhiteSpace(PasswordBox.Password) ? null : PasswordBox.Password,
                    DateAdded = DateTime.Now,
                    LastUsed = null
                };
            }

            DialogResult = true;
            Close();
        }
    }
}