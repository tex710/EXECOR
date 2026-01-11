using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HackHelper.Services
{
    public static class IconExtractor
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static string SaveIconAsBase64(string exePath)
        {
            try
            {
                if (!File.Exists(exePath))
                {
                    MessageBox.Show("File does not exist!", "Debug");
                    return null;
                }

                IntPtr hIcon = ExtractIcon(IntPtr.Zero, exePath, 0);

                if (hIcon == IntPtr.Zero || hIcon == new IntPtr(1))
                {
                    MessageBox.Show("Failed to extract icon!", "Debug");
                    return null;
                }

                BitmapSource iconBitmap = Imaging.CreateBitmapSourceFromHIcon(
                    hIcon,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                DestroyIcon(hIcon);

                // Convert to PNG and then to Base64
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(iconBitmap));

                using (MemoryStream ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    byte[] bytes = ms.ToArray();
                    string base64 = Convert.ToBase64String(bytes);

                    return base64;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}", "Error");
                return null;
            }
        }

        public static BitmapImage Base64ToImage(string base64String)
        {
            try
            {
                if (string.IsNullOrEmpty(base64String))
                    return null;

                byte[] bytes = Convert.FromBase64String(base64String);
                using (MemoryStream memory = new MemoryStream(bytes))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Image conversion failed: {ex.Message}", "Error");
                return null;
            }
        }
    }
}