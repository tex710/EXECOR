using Execor.Models;
using Execor.Services;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace Execor
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

        // Edit-mode constructor
        public AddSteamAccountWindow(SteamAccount account) : this()
        {
            _isEditMode = true;
            _editingAccount = account;

            /* ----  LOOK & FEEL SWITCH  ---- */
            Title = "Edit Steam Account";
            TitleTextBlock.Text = "✏️ EDIT ACCOUNT";
            AddAccountButton.Content = "💾 Save Changes";
            /* -------------------------------- */

            // Populate fields
            AccountNameTextBox.Text = account.AccountName;
            UsernameTextBox.Text = account.Username;
            PasswordBox.Password = account.Password ?? string.Empty;
            SteamIdTextBox.Text = account.SteamId ?? string.Empty;

            UpdatePasswordStrength(account.Password ?? string.Empty);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
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

            string? password = _isPasswordVisible
                ? PasswordTextBox.Text
                : PasswordBox.Password;

            if (_isEditMode && _editingAccount != null)
            {
                _editingAccount.AccountName = AccountNameTextBox.Text.Trim();
                _editingAccount.Username = UsernameTextBox.Text.Trim();
                _editingAccount.Password = string.IsNullOrWhiteSpace(password) ? null : password;
                _editingAccount.SteamId = string.IsNullOrWhiteSpace(SteamIdTextBox.Text) ? null : SteamIdTextBox.Text.Trim();
                NewAccount = _editingAccount;
            }
            else
            {
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
                UpdatePasswordStrength(PasswordBox.Password);
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPasswordVisible)
                UpdatePasswordStrength(PasswordTextBox.Text);
        }

        private void ShowHidePassword_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBoxBorder.Visibility = Visibility.Collapsed;
                PasswordTextBoxBorder.Visibility = Visibility.Visible;
                ShowHideButton.Content = "🙈 Hide";
                PasswordTextBox.Focus();
                PasswordTextBox.CaretIndex = PasswordTextBox.Text.Length;
            }
            else
            {
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
                PasswordTextBox.Text = generatedPassword;
            else
                PasswordBox.Password = generatedPassword;
            UpdatePasswordStrength(generatedPassword);
        }

        private string GenerateSecurePassword(int length)
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*-_=+";
            const string allChars = uppercase + lowercase + digits + special;

            using var rng = RandomNumberGenerator.Create();
            var password = new StringBuilder(length);

            password.Append(uppercase[GetRandomIndex(rng, uppercase.Length)]);
            password.Append(lowercase[GetRandomIndex(rng, lowercase.Length)]);
            password.Append(digits[GetRandomIndex(rng, digits.Length)]);
            password.Append(special[GetRandomIndex(rng, special.Length)]);

            for (int i = 4; i < length; i++)
                password.Append(allChars[GetRandomIndex(rng, allChars.Length)]);

            return new string(password.ToString().OrderBy(x => GetRandomIndex(rng, length)).ToArray());
        }

        private int GetRandomIndex(RandomNumberGenerator rng, int max)
        {
            byte[] data = new byte[4];
            rng.GetBytes(data);
            return Math.Abs(BitConverter.ToInt32(data, 0)) % max;
        }

        private void UpdatePasswordStrength(string password)
        {
            var result = PasswordStrengthService.CalculateStrength(password);

            double containerWidth = StrengthBarContainer.ActualWidth > 0
                ? StrengthBarContainer.ActualWidth - 2
                : 458; 

            double targetWidth = containerWidth * (result.ProgressPercentage / 100.0);

            // Animate the strength bar
            DoubleAnimation widthAnim = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            StrengthBar.BeginAnimation(WidthProperty, widthAnim);

            var parsedColor = (Color)ColorConverter.ConvertFromString(result.Color);
            
            if (result.Strength == PasswordStrength.VeryStrong)
            {
                // Use Primary -> Secondary gradient for Very Strong
                var primaryColor = (Color)FindResource("PrimaryColor");
                var secondaryColor = (Color)FindResource("SecondaryColor");

                var gradientBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0)
                };
                gradientBrush.GradientStops.Add(new GradientStop(primaryColor, 0));
                gradientBrush.GradientStops.Add(new GradientStop(secondaryColor, 1));
                StrengthBar.Background = gradientBrush;

                // Apply gradient to text
                var textGradient = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0)
                };
                textGradient.GradientStops.Add(new GradientStop(primaryColor, 0));
                textGradient.GradientStops.Add(new GradientStop(secondaryColor, 1));
                StrengthText.Foreground = textGradient;

                // Update glow with primary color
                if (StrengthBar.Effect is DropShadowEffect glow)
                {
                    glow.Color = primaryColor;
                    glow.Opacity = 0.8;
                }
            }
            else
            {
                // Use subtle gradient for other strengths
                var gradientBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0)
                };
                gradientBrush.GradientStops.Add(new GradientStop(parsedColor, 0));
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255,
                    (byte)Math.Min(255, parsedColor.R + 30),
                    (byte)Math.Min(255, parsedColor.G + 30),
                    (byte)Math.Min(255, parsedColor.B + 30)), 1));
                StrengthBar.Background = gradientBrush;
                
                // Solid color for text
                StrengthText.Foreground = new SolidColorBrush(parsedColor);

                // Update the glow effect color
                if (StrengthBar.Effect is DropShadowEffect glow)
                {
                    glow.Color = parsedColor;
                    glow.Opacity = result.ProgressPercentage > 60 ? 0.8 : 0.4;
                }
            }
            StrengthText.Text = result.StrengthText;
        }
    }
}