using Execor.Models;
using Execor.Services;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace Execor
{
    public partial class AddPasswordDialog : Window
    {
        public PasswordEntry NewPassword { get; private set; }
        private bool isPasswordVisible = false;
        private string selectedIconBase64 = null;
        private bool _isEditMode = false;
        private PasswordEntry _editingPassword = null;

        // Constructor for Add mode
        public AddPasswordDialog()
        {
            InitializeComponent();
            UpdatePasswordStrength(string.Empty);
        }

        // Constructor for Edit mode
        public AddPasswordDialog(PasswordEntry password) : this()
        {
            _isEditMode = true;
            _editingPassword = password;

            Title = "Edit Password";
            TitleTextBlock.Text = "✏️ EDIT PASSWORD";
            ActionButton.Content = "💾 Save Changes";

            // Populate fields
            ServiceTextBox.Text = password.ServiceName;
            UsernameTextBox.Text = password.Username;
            EmailTextBox.Text = password.Email ?? string.Empty;
            PasswordBox.Password = password.Password;
            selectedIconBase64 = password.IconBase64;

            // Show icon preview if exists
            if (!string.IsNullOrEmpty(selectedIconBase64))
            {
                try
                {
                    IconPreviewImage.Source = IconExtractor.Base64ToImage(selectedIconBase64);
                    IconPreviewBorder.Visibility = Visibility.Visible;
                }
                catch
                {
                    IconPreviewBorder.Visibility = Visibility.Collapsed;
                }
            }

            UpdatePasswordStrength(password.Password);
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

            // Create or update password entry
            if (_isEditMode && _editingPassword != null)
            {
                // Update existing password
                _editingPassword.ServiceName = ServiceTextBox.Text.Trim();
                _editingPassword.Username = UsernameTextBox.Text.Trim();
                _editingPassword.Email = string.IsNullOrWhiteSpace(EmailTextBox.Text)
                    ? null
                    : EmailTextBox.Text.Trim();
                _editingPassword.Password = GetCurrentPassword();
                _editingPassword.IconBase64 = selectedIconBase64;

                NewPassword = _editingPassword;
            }
            else
            {
                // Create new password
                NewPassword = new PasswordEntry
                {
                    ServiceName = ServiceTextBox.Text.Trim(),
                    Username = UsernameTextBox.Text.Trim(),
                    Email = string.IsNullOrWhiteSpace(EmailTextBox.Text)
                        ? null
                        : EmailTextBox.Text.Trim(),
                    Password = GetCurrentPassword(),
                    IconBase64 = selectedIconBase64
                };
            }

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

            // Calculate target width based on the actual container width
            // Use StrengthBarContainer.ActualWidth, which is the Border containing the strength bar
            double containerWidth = StrengthBarContainer.ActualWidth > 0
                ? StrengthBarContainer.ActualWidth - 2 // Subtract 2 for the border
                : 458; // Fallback width

            double targetWidth = containerWidth * (result.ProgressPercentage / 100.0);

            // Animate the strength bar
            DoubleAnimation widthAnim = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            StrengthBar.BeginAnimation(System.Windows.FrameworkElement.WidthProperty, widthAnim);

            // Create gradient background based on strength
            var color = (Color)ColorConverter.ConvertFromString(result.Color);
            Brush gradientBrush;

            if (result.StrengthText == "Very Strong")
            {
                // Use Primary -> Secondary gradient for Very Strong
                var primaryColor = (Color)FindResource("PrimaryColor");
                var secondaryColor = (Color)FindResource("SecondaryColor");

                gradientBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0)
                };
                ((LinearGradientBrush)gradientBrush).GradientStops.Add(new GradientStop(primaryColor, 0));
                ((LinearGradientBrush)gradientBrush).GradientStops.Add(new GradientStop(secondaryColor, 1));

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
                gradientBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 0)
                };
                ((LinearGradientBrush)gradientBrush).GradientStops.Add(new GradientStop(color, 0));
                ((LinearGradientBrush)gradientBrush).GradientStops.Add(new GradientStop(Color.FromArgb(255,
                    (byte)Math.Min(255, color.R + 30),
                    (byte)Math.Min(255, color.G + 30),
                    (byte)Math.Min(255, color.B + 30)), 1));

                // Solid color for text
                StrengthText.Foreground = new SolidColorBrush(color);

                // Update the glow effect color
                if (StrengthBar.Effect is DropShadowEffect glow)
                {
                    glow.Color = color;
                    glow.Opacity = result.ProgressPercentage > 60 ? 0.8 : 0.4;
                }
            }

            StrengthBar.Background = gradientBrush;
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