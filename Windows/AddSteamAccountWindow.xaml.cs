using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HackHelper.Models;

namespace HackHelper
{
    public partial class AddSteamAccountWindow : Window
    {
        public SteamAccount? NewAccount { get; private set; }
        private bool _isEditMode = false;
        private SteamAccount? _editingAccount = null;
        private bool _isPasswordVisible = false;

        public AddSteamAccountWindow()
        {
            InitializeComponent();
            UpdatePasswordStrength(string.Empty);
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
            SteamIdTextBox.Text = account.SteamId ?? string.Empty;
            UpdatePasswordStrength(account.Password ?? string.Empty);
        }

        // Called after InitializeComponent in edit mode
        public void SetEditMode()
        {
            Title = "Edit Steam Account";
            // Find the button by walking the visual tree
            var contentGrid = (Grid)Content;
            var mainBorder = (Border)contentGrid.Children[1];
            var dockPanel = (DockPanel)mainBorder.Child;
            var buttonStackPanel = (StackPanel)dockPanel.Children[0];
            var addButton = (Button)buttonStackPanel.Children[0];
            addButton.Content = "💾 Save Changes";
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

            // Get password from visible field or hidden field
            string? password = _isPasswordVisible
                ? PasswordTextBox.Text
                : PasswordBox.Password;

            // Create or update account
            if (_isEditMode && _editingAccount != null)
            {
                // Update existing account
                _editingAccount.AccountName = AccountNameTextBox.Text.Trim();
                _editingAccount.Username = UsernameTextBox.Text.Trim();
                _editingAccount.Password = string.IsNullOrWhiteSpace(password) ? null : password;
                _editingAccount.SteamId = string.IsNullOrWhiteSpace(SteamIdTextBox.Text) ? null : SteamIdTextBox.Text.Trim();
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
                    Password = string.IsNullOrWhiteSpace(password) ? null : password,
                    SteamId = string.IsNullOrWhiteSpace(SteamIdTextBox.Text) ? null : SteamIdTextBox.Text.Trim(),
                    DateAdded = DateTime.Now,
                    LastUsed = null
                };
            }

            DialogResult = true;
            Close();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isPasswordVisible)
            {
                UpdatePasswordStrength(PasswordBox.Password);
            }
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPasswordVisible)
            {
                UpdatePasswordStrength(PasswordTextBox.Text);
            }
        }

        private void ShowHidePassword_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                // Show password as text
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBoxBorder.Visibility = Visibility.Collapsed;
                PasswordTextBoxBorder.Visibility = Visibility.Visible;
                ShowHideButton.Content = "🙈 Hide";
                PasswordTextBox.Focus();
                PasswordTextBox.CaretIndex = PasswordTextBox.Text.Length;
            }
            else
            {
                // Hide password
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBoxBorder.Visibility = Visibility.Collapsed;
                PasswordBoxBorder.Visibility = Visibility.Visible;
                ShowHideButton.Content = "👁 Show";
                PasswordBox.Focus();
            }
        }

        private void AutoGenerate_Click(object sender, RoutedEventArgs e)
        {
            string generatedPassword = GenerateSecurePassword(16);

            if (_isPasswordVisible)
            {
                PasswordTextBox.Text = generatedPassword;
            }
            else
            {
                PasswordBox.Password = generatedPassword;
            }

            UpdatePasswordStrength(generatedPassword);
        }

        private string GenerateSecurePassword(int length)
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*-_=+";
            const string allChars = uppercase + lowercase + digits + special;

            using (var rng = RandomNumberGenerator.Create())
            {
                var password = new StringBuilder(length);
                var data = new byte[length];

                // Ensure at least one of each type
                password.Append(uppercase[GetRandomIndex(rng, uppercase.Length)]);
                password.Append(lowercase[GetRandomIndex(rng, lowercase.Length)]);
                password.Append(digits[GetRandomIndex(rng, digits.Length)]);
                password.Append(special[GetRandomIndex(rng, special.Length)]);

                // Fill the rest randomly
                for (int i = 4; i < length; i++)
                {
                    password.Append(allChars[GetRandomIndex(rng, allChars.Length)]);
                }

                // Shuffle the password
                return new string(password.ToString().OrderBy(x => GetRandomIndex(rng, length)).ToArray());
            }
        }

        private int GetRandomIndex(RandomNumberGenerator rng, int max)
        {
            byte[] data = new byte[4];
            rng.GetBytes(data);
            return Math.Abs(BitConverter.ToInt32(data, 0)) % max;
        }

        private void UpdatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                StrengthText.Text = "No Password";
                StrengthText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
                AnimateStrengthBar(0, "#6B7280");
                return;
            }

            int score = CalculatePasswordStrength(password);
            double targetWidth = 0;
            string color = "#6B7280";
            string strengthText = "No Password";

            if (score < 3)
            {
                strengthText = "Weak";
                color = "#EF4444"; // Red
                targetWidth = 100;
            }
            else if (score < 4)
            {
                strengthText = "Medium";
                color = "#F59E0B"; // Orange
                targetWidth = 200;
            }
            else
            {
                strengthText = "Strong";
                color = "#10B981"; // Green
                targetWidth = 300;
            }

            StrengthText.Text = strengthText;
            StrengthText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            AnimateStrengthBar(targetWidth, color);
        }

        private int CalculatePasswordStrength(string password)
        {
            int score = 0;

            if (password.Length >= 8) score++;
            if (password.Length >= 12) score++;
            if (password.Any(char.IsUpper)) score++;
            if (password.Any(char.IsLower)) score++;
            if (password.Any(char.IsDigit)) score++;
            if (password.Any(ch => !char.IsLetterOrDigit(ch))) score++;

            return score;
        }

        private void AnimateStrengthBar(double targetWidth, string color)
        {
            var widthAnimation = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            StrengthBar.BeginAnimation(WidthProperty, widthAnimation);
            StrengthBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }
    }
}