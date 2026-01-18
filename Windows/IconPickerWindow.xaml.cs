using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Execor
{
    public partial class IconPickerWindow : Window
    {
        public string SelectedIconBase64 { get; private set; }
        private string currentIconBase64 = null;

        private readonly string[] popularEmojis = new[]
        {
            "🔒", "🔑", "🌐", "📧", "💼", "🎮", "📱", "💳",
            "🏦", "🏠", "🎵", "📺", "🎬", "📷", "☁️", "🗃️",
            "💻", "🖥️", "⚙️", "🔧", "🎯", "📊", "📈", "💰",
            "🛒", "🎨", "📝", "📚", "🎓", "🏆", "⭐", "❤️",
            "🔔", "📮", "📬", "🗂️", "📋", "📌", "🔐", "🛡️"
        };

        public IconPickerWindow()
        {
            InitializeComponent();
            LoadEmojis();
        }

        private void LoadEmojis()
        {
            foreach (string emoji in popularEmojis)
            {
                Button emojiButton = new Button
                {
                    Content = emoji,
                    Style = (Style)FindResource("EmojiButton")
                };
                emojiButton.Click += EmojiButton_Click;
                EmojiPanel.Children.Add(emojiButton);
            }
        }

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string emoji = button.Content.ToString();
                currentIconBase64 = EmojiToBase64(emoji);
                ShowPreview(emoji, isEmoji: true);
            }
        }

        private void UploadCustomIcon_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.ico)|*.png;*.jpg;*.jpeg;*.bmp;*.ico",
                Title = "Select Icon Image"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    byte[] imageBytes = File.ReadAllBytes(openFileDialog.FileName);
                    currentIconBase64 = Convert.ToBase64String(imageBytes);

                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(openFileDialog.FileName);
                    bitmap.DecodePixelWidth = 64;
                    bitmap.EndInit();

                    PreviewImage.Source = bitmap;
                    PreviewBorder.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowPreview(string emoji, bool isEmoji)
        {
            if (isEmoji)
            {
                // Create a visual representation of the emoji with proper background
                DrawingVisual visual = new DrawingVisual();
                using (DrawingContext context = visual.RenderOpen())
                {
                    // Draw transparent background
                    context.DrawRectangle(new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)), null, new Rect(0, 0, 64, 64));

                    FormattedText text = new FormattedText(
                        emoji,
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(new FontFamily("Segoe UI Emoji"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                        48,
                        Brushes.White,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    // Center the text
                    double x = (64 - text.Width) / 2;
                    double y = (64 - text.Height) / 2;
                    context.DrawText(text, new Point(x, y));
                }

                RenderTargetBitmap bitmap = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(visual);

                PreviewImage.Source = bitmap;
                PreviewBorder.Visibility = Visibility.Visible;
            }
        }

        private string EmojiToBase64(string emoji)
        {
            // Render emoji to image and convert to base64
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                // Draw transparent background
                context.DrawRectangle(new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)), null, new Rect(0, 0, 64, 64));

                FormattedText text = new FormattedText(
                    emoji,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI Emoji"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                    48,
                    Brushes.White,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                // Center the emoji
                double x = (64 - text.Width) / 2;
                double y = (64 - text.Height) / 2;
                context.DrawText(text, new Point(x, y));
            }

            RenderTargetBitmap bitmap = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (MemoryStream stream = new MemoryStream())
            {
                encoder.Save(stream);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentIconBase64))
            {
                MessageBox.Show("Please select an icon first!", "No Icon Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedIconBase64 = currentIconBase64;
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