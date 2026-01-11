using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HackHelper.Models;
using HackHelper.Services;

namespace HackHelper
{
    public partial class AddPasswordDialog : Window
    {
        public PasswordEntry NewPassword { get; private set; }
        private bool isPasswordVisible = false;
        private string selectedIconBase64 = null;

        public AddPasswordDialog()
        {
            InitializeComponent();
            UpdatePasswordStrength(string.Empty);
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ServiceTextBox.Text) ||
                string.IsNullOrWhiteSpace(UsernameTextBox.Text) ||
                string.IsNullOrWhiteSpace(GetCurrentPassword()))
            {
                MessageBox.Show("Please fill in Service Name, Username, and Password!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Don't auto-fetch favicon - it can freeze the app
            // User can manually choose an icon if they want

            NewPassword = new PasswordEntry
            {
                ServiceName = ServiceTextBox.Text,
                Username = UsernameTextBox.Text,
                Email = EmailTextBox.Text,
                Password = GetCurrentPassword(),
                IconBase64 = selectedIconBase64
            };

            DialogResult = true;
            Close();
        }

        private void ChooseIcon_Click(object sender, RoutedEventArgs e)
        {
            var iconPicker = new IconPickerWindow
            {
                Owner = this
            };

            if (iconPicker.ShowDialog() == true)
            {
                selectedIconBase64 = iconPicker.SelectedIconBase64;

                // Update preview
                if (!string.IsNullOrEmpty(selectedIconBase64))
                {
                    try
                    {
                        IconPreviewImage.Source = IconExtractor.Base64ToImage(selectedIconBase64);
                        IconPreviewBorder.Visibility = Visibility.Visible;
                    }
                    catch
                    {
                        // If conversion fails, hide preview
                        IconPreviewBorder.Visibility = Visibility.Collapsed;
                    }
                }
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

        private void AutoGenerate_Click(object sender, RoutedEventArgs e)
        {
            string generatedPassword = GenerateSecurePassword(16);

            if (isPasswordVisible)
            {
                PasswordTextBox.Text = generatedPassword;
            }
            else
            {
                PasswordBox.Password = generatedPassword;
            }
        }

        private void ShowHidePassword_Click(object sender, RoutedEventArgs e)
        {
            if (isPasswordVisible)
            {
                // Switch to hidden mode
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordBoxBorder.Visibility = Visibility.Visible;
                PasswordTextBoxBorder.Visibility = Visibility.Collapsed;
                ShowHideButton.Content = "👁 Show";
                isPasswordVisible = false;
            }
            else
            {
                // Switch to visible mode
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBoxBorder.Visibility = Visibility.Collapsed;
                PasswordTextBoxBorder.Visibility = Visibility.Visible;
                ShowHideButton.Content = "🙈 Hide";
                isPasswordVisible = true;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isPasswordVisible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
            }
            UpdatePasswordStrength(PasswordBox.Password);
        }

        private void PasswordTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (isPasswordVisible)
            {
                PasswordBox.Password = PasswordTextBox.Text;
            }
            UpdatePasswordStrength(PasswordTextBox.Text);
        }

        private void UpdatePasswordStrength(string password)
        {
            var result = PasswordStrengthService.CalculateStrength(password);

            StrengthText.Text = result.StrengthText;
            StrengthText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(result.Color));

            // Animate the strength bar
            double targetWidth = (this.ActualWidth > 0 ? this.ActualWidth - 100 : 400) * (result.ProgressPercentage / 100.0);

            DoubleAnimation widthAnim = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            StrengthBar.BeginAnimation(System.Windows.FrameworkElement.WidthProperty, widthAnim);
            StrengthBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(result.Color));
        }

        private string GetCurrentPassword()
        {
            return isPasswordVisible ? PasswordTextBox.Text : PasswordBox.Password;
        }

        private string GenerateSecurePassword(int length)
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";
            const string symbols = "!@#$%^&*()-_=+[]{}|;:,.<>?";

            string allChars = uppercase + lowercase + numbers + symbols;

            using (var rng = RandomNumberGenerator.Create())
            {
                var password = new StringBuilder();

                // Ensure at least one character from each category
                password.Append(GetRandomChar(uppercase, rng));
                password.Append(GetRandomChar(lowercase, rng));
                password.Append(GetRandomChar(numbers, rng));
                password.Append(GetRandomChar(symbols, rng));

                // Fill the rest randomly
                for (int i = 4; i < length; i++)
                {
                    password.Append(GetRandomChar(allChars, rng));
                }

                // Shuffle the password to randomize positions
                return new string(password.ToString().OrderBy(c => GetRandomNumber(rng)).ToArray());
            }
        }

        private char GetRandomChar(string chars, RandomNumberGenerator rng)
        {
            byte[] randomNumber = new byte[1];
            rng.GetBytes(randomNumber);
            int index = randomNumber[0] % chars.Length;
            return chars[index];
        }

        private int GetRandomNumber(RandomNumberGenerator rng)
        {
            byte[] randomNumber = new byte[4];
            rng.GetBytes(randomNumber);
            return BitConverter.ToInt32(randomNumber, 0);
        }
    }
}