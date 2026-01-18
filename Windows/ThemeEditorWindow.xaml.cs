using Execor.Models;
using Execor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace Execor
{
    public partial class ThemeEditorWindow : Window
    {
        private DataService dataService;
        private Settings settings;
        private List<CustomTheme> customThemes;

        public ThemeEditorWindow()
        {
            InitializeComponent();
            dataService = new DataService();
            settings = dataService.LoadSettings();
            customThemes = dataService.LoadCustomThemes();
            LoadDefaultThemes();
            LoadCustomThemeSlots();
            LoadCurrentTheme();
        }

        private void LoadDefaultThemes()
        {
            var themes = ThemeManager.GetAvailableThemes();
            DefaultThemeComboBox.ItemsSource = themes.Select(t => t.Name).ToList();

            // Select current theme if it's a default theme
            var currentIndex = themes.FindIndex(t => t.Name == settings.SelectedTheme);
            if (currentIndex >= 0)
            {
                DefaultThemeComboBox.SelectedIndex = currentIndex;
            }
        }

        private void LoadCustomThemeSlots()
        {
            // Only show themes that actually exist
            var themeNames = customThemes
                .OrderBy(ct => ct.SlotNumber)
                .Select(ct => ct.Name)
                .ToList();

            CustomThemeComboBox.ItemsSource = themeNames;

            // Select first theme if any exist
            if (themeNames.Count > 0 && CustomThemeComboBox.SelectedIndex < 0)
            {
                CustomThemeComboBox.SelectedIndex = 0;
            }

            // Update the combo box placeholder or disable if empty
            CustomThemeComboBox.IsEnabled = themeNames.Count > 0;
        }

        private void LoadCurrentTheme()
        {
            var themes = ThemeManager.GetAvailableThemes();
            var currentTheme = themes.FirstOrDefault(t => t.Name == settings.SelectedTheme) ?? Theme.BluePink;

            PopulateColorFields(currentTheme);
            UpdateColorPreviews();
            UpdatePalette();
        }

        private void PopulateColorFields(Theme theme)
        {
            ThemeNameTextBox.Text = theme.Name;
            PrimaryColorTextBox.Text = theme.PrimaryColor;
            SecondaryColorTextBox.Text = theme.SecondaryColor;
            BackgroundColorTextBox.Text = theme.BackgroundColor;
            SurfaceColorTextBox.Text = theme.SurfaceColor;
            BorderColorTextBox.Text = theme.BorderColor;
            TextColorTextBox.Text = theme.TextColor;
            SubTextColorTextBox.Text = theme.SubTextColor;
        }

        private void DefaultThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DefaultThemeComboBox.SelectedItem is string themeName)
            {
                var themes = ThemeManager.GetAvailableThemes();
                var theme = themes.FirstOrDefault(t => t.Name == themeName);
                if (theme != null)
                {
                    PopulateColorFields(theme);
                    UpdateColorPreviews();
                    UpdatePalette();
                }
            }
        }

        private void CustomThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Just for slot selection - actual loading happens on button click
        }

        private void ApplyDefaultTheme_Click(object sender, RoutedEventArgs e)
        {
            if (DefaultThemeComboBox.SelectedItem is string themeName)
            {
                ThemeManager.ApplyTheme(themeName);
                settings.SelectedTheme = themeName;
                dataService.SaveSettings(settings);
                ToastService.Success("Theme applied", $"{themeName} is now active");
            }
        }

        private void LoadCustomTheme_Click(object sender, RoutedEventArgs e)
        {
            if (CustomThemeComboBox.SelectedItem is string themeName)
            {
                var customTheme = customThemes.FirstOrDefault(ct => ct.Name == themeName);

                if (customTheme != null)
                {
                    var theme = new Theme
                    {
                        Name = customTheme.Name,
                        PrimaryColor = customTheme.PrimaryColor,
                        SecondaryColor = customTheme.SecondaryColor,
                        BackgroundColor = customTheme.BackgroundColor,
                        SurfaceColor = customTheme.SurfaceColor,
                        BorderColor = customTheme.BorderColor,
                        TextColor = customTheme.TextColor,
                        SubTextColor = customTheme.SubTextColor
                    };

                    PopulateColorFields(theme);
                    UpdateColorPreviews();
                    UpdatePalette();
                    ToastService.Info("Custom theme loaded", $"Loaded '{customTheme.Name}'");
                }
            }
            else
            {
                ToastService.Warning("No selection", "Please select a custom theme to load");
            }
        }

        private void DeleteCustomTheme_Click(object sender, RoutedEventArgs e)
        {
            if (CustomThemeComboBox.SelectedItem is string themeName)
            {
                var customTheme = customThemes.FirstOrDefault(ct => ct.Name == themeName);

                if (customTheme != null)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Delete custom theme '{customTheme.Name}'?",
                        "Confirm Delete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        customThemes.Remove(customTheme);

                        // Reassign slot numbers to keep them sequential
                        ReassignSlotNumbers();

                        dataService.SaveCustomThemes(customThemes);
                        LoadCustomThemeSlots(); // Refresh the display
                        ToastService.Warning("Theme deleted", $"Removed '{customTheme.Name}'");
                    }
                }
            }
            else
            {
                ToastService.Info("No selection", "Please select a custom theme to delete");
            }
        }

        private void ReassignSlotNumbers()
        {
            // Keep slot numbers sequential after deletion
            var sortedThemes = customThemes.OrderBy(ct => ct.SlotNumber).ToList();
            for (int i = 0; i < sortedThemes.Count; i++)
            {
                sortedThemes[i].SlotNumber = i + 1;
            }
        }

        private void ColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateColorPreviews();
            UpdatePalette();
        }

        private void UpdateColorPreviews()
        {
            UpdatePreview(PrimaryColorPreview, PrimaryColorTextBox.Text);
            UpdatePreview(SecondaryColorPreview, SecondaryColorTextBox.Text);
            UpdatePreview(BackgroundColorPreview, BackgroundColorTextBox.Text);
            UpdatePreview(SurfaceColorPreview, SurfaceColorTextBox.Text);
            UpdatePreview(BorderColorPreview, BorderColorTextBox.Text);
            UpdatePreview(TextColorPreview, TextColorTextBox.Text);
            UpdatePreview(SubTextColorPreview, SubTextColorTextBox.Text);
        }

        private void UpdatePreview(Border preview, string colorHex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorHex);
                preview.Background = new SolidColorBrush(color);
            }
            catch
            {
                preview.Background = new SolidColorBrush(Colors.Gray);
            }
        }

        private void ColorPreview_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string colorType)
            {
                using (ColorDialog colorDialog = new ColorDialog())
                {
                    // Get current color
                    string currentHex = GetColorTextBoxByType(colorType).Text;
                    try
                    {
                        var currentColor = (Color)ColorConverter.ConvertFromString(currentHex);
                        colorDialog.Color = System.Drawing.Color.FromArgb(currentColor.A, currentColor.R, currentColor.G, currentColor.B);
                    }
                    catch { }

                    colorDialog.FullOpen = true;

                    if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        var selectedColor = colorDialog.Color;
                        string hexColor = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";
                        GetColorTextBoxByType(colorType).Text = hexColor;
                    }
                }
            }
        }

        private void UpdatePalette()
        {

        }

        private System.Windows.Controls.TextBox GetColorTextBoxByType(string type)
        {
            return type switch
            {
                "Primary" => PrimaryColorTextBox,
                "Secondary" => SecondaryColorTextBox,
                "Background" => BackgroundColorTextBox,
                "Surface" => SurfaceColorTextBox,
                "Border" => BorderColorTextBox,
                "Text" => TextColorTextBox,
                "SubText" => SubTextColorTextBox,
                _ => PrimaryColorTextBox
            };
        }

        private void PreviewTheme_Click(object sender, RoutedEventArgs e)
        {
            var theme = CreateThemeFromFields();
            ThemeManager.ApplyTheme(theme);
            ToastService.Info("Preview applied", "This is temporary - save to make permanent");
        }

        private void SaveToSlot_Click(object sender, RoutedEventArgs e)
        {
            string themeName = ThemeNameTextBox.Text.Trim();

            // Validate theme name
            if (string.IsNullOrEmpty(themeName))
            {
                ToastService.Warning("Invalid name", "Please enter a theme name");
                return;
            }

            // Check if name is already used
            var existingThemeWithName = customThemes.FirstOrDefault(ct =>
                ct.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));

            if (existingThemeWithName != null)
            {
                // Ask user if they want to overwrite
                var result = System.Windows.MessageBox.Show(
                    $"A theme named '{themeName}' already exists.\n\nDo you want to overwrite it?",
                    "Theme Name Already Exists",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Overwrite the existing theme
                    UpdateExistingTheme(existingThemeWithName);
                    return;
                }
                else
                {
                    ToastService.Info("Save cancelled", "Please choose a different name");
                    return;
                }
            }

            // Create new theme with next slot number
            int nextSlot = customThemes.Count > 0 ? customThemes.Max(ct => ct.SlotNumber) + 1 : 1;
            SaveThemeToSlot(nextSlot);
        }

        private void UpdateExistingTheme(CustomTheme existingTheme)
        {
            // Update the existing theme's colors
            existingTheme.PrimaryColor = PrimaryColorTextBox.Text;
            existingTheme.SecondaryColor = SecondaryColorTextBox.Text;
            existingTheme.BackgroundColor = BackgroundColorTextBox.Text;
            existingTheme.SurfaceColor = SurfaceColorTextBox.Text;
            existingTheme.BorderColor = BorderColorTextBox.Text;
            existingTheme.TextColor = TextColorTextBox.Text;
            existingTheme.SubTextColor = SubTextColorTextBox.Text;

            dataService.SaveCustomThemes(customThemes);
            LoadCustomThemeSlots();

            // Select the updated theme
            CustomThemeComboBox.SelectedItem = existingTheme.Name;

            ToastService.Success("Theme updated", $"'{existingTheme.Name}' has been updated");
        }

        private void SaveThemeToSlot(int slotNumber)
        {
            // Create new custom theme
            var customTheme = new CustomTheme
            {
                Id = Guid.NewGuid().ToString(),
                SlotNumber = slotNumber,
                Name = ThemeNameTextBox.Text.Trim(),
                PrimaryColor = PrimaryColorTextBox.Text,
                SecondaryColor = SecondaryColorTextBox.Text,
                BackgroundColor = BackgroundColorTextBox.Text,
                SurfaceColor = SurfaceColorTextBox.Text,
                BorderColor = BorderColorTextBox.Text,
                TextColor = TextColorTextBox.Text,
                SubTextColor = SubTextColorTextBox.Text
            };

            customThemes.Add(customTheme);
            dataService.SaveCustomThemes(customThemes);

            // Refresh the combo box to reflect updated names
            LoadCustomThemeSlots();
            CustomThemeComboBox.SelectedItem = customTheme.Name;

            ToastService.Success("Theme saved", $"'{customTheme.Name}' has been saved");
        }

        private Theme CreateThemeFromFields()
        {
            return new Theme
            {
                Name = ThemeNameTextBox.Text,
                PrimaryColor = PrimaryColorTextBox.Text,
                SecondaryColor = SecondaryColorTextBox.Text,
                BackgroundColor = BackgroundColorTextBox.Text,
                SurfaceColor = SurfaceColorTextBox.Text,
                BorderColor = BorderColorTextBox.Text,
                TextColor = TextColorTextBox.Text,
                SubTextColor = SubTextColorTextBox.Text
            };
        }

        private void ResetToDefault_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "Reset to default Red Hot theme?",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                PopulateColorFields(Theme.BluePink);
                UpdateColorPreviews();
                UpdatePalette();
                ToastService.Info("Reset complete", "Colors reset to default");
            }
        }

        private void ApplyAndClose_Click(object sender, RoutedEventArgs e)
        {
            var theme = CreateThemeFromFields();
            ThemeManager.ApplyTheme(theme);

            // Save as current theme name
            settings.SelectedTheme = theme.Name;
            dataService.SaveSettings(settings);

            ToastService.Success("Theme applied", "Your theme is now active");
            this.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Restore original theme
            ThemeManager.ApplyTheme(settings.SelectedTheme);
            this.Close();
        }

        private void ImportTheme_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get theme code from clipboard
                string clipboardText = System.Windows.Clipboard.GetText();

                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    ToastService.Warning("Clipboard empty", "Please copy a theme code first");
                    return;
                }

                // Try to decode from Base64
                byte[] jsonBytes = Convert.FromBase64String(clipboardText);
                string json = System.Text.Encoding.UTF8.GetString(jsonBytes);

                // Deserialize JSON
                var themeData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);

                // Validate and extract colors
                string name = themeData.GetProperty("name").GetString();
                string primary = themeData.GetProperty("primary").GetString();
                string secondary = themeData.GetProperty("secondary").GetString();
                string background = themeData.GetProperty("background").GetString();
                string surface = themeData.GetProperty("surface").GetString();
                string border = themeData.GetProperty("border").GetString();
                string text = themeData.GetProperty("text").GetString();
                string subtext = themeData.GetProperty("subtext").GetString();

                // Validate hex colors
                ValidateHexColor(primary);
                ValidateHexColor(secondary);
                ValidateHexColor(background);
                ValidateHexColor(surface);
                ValidateHexColor(border);
                ValidateHexColor(text);
                ValidateHexColor(subtext);

                // Populate the textboxes
                ThemeNameTextBox.Text = name;
                PrimaryColorTextBox.Text = primary;
                SecondaryColorTextBox.Text = secondary;
                BackgroundColorTextBox.Text = background;
                SurfaceColorTextBox.Text = surface;
                BorderColorTextBox.Text = border;
                TextColorTextBox.Text = text;
                SubTextColorTextBox.Text = subtext;

                // Update previews
                UpdateColorPreviews();
                UpdatePalette();

                ToastService.Success("Theme imported", $"Loaded '{name}' from clipboard");
            }
            catch (FormatException)
            {
                ToastService.Error("Import failed", "Invalid theme code format");
            }
            catch (System.Text.Json.JsonException)
            {
                ToastService.Error("Import failed", "Invalid theme code structure");
            }
            catch (Exception ex)
            {
                ToastService.Error("Import failed", $"Error: {ex.Message}");
            }
        }

        private void ExportTheme_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create theme code in JSON format
                var themeCode = new
                {
                    name = ThemeNameTextBox.Text,
                    primary = PrimaryColorTextBox.Text,
                    secondary = SecondaryColorTextBox.Text,
                    background = BackgroundColorTextBox.Text,
                    surface = SurfaceColorTextBox.Text,
                    border = BorderColorTextBox.Text,
                    text = TextColorTextBox.Text,
                    subtext = SubTextColorTextBox.Text
                };

                // Convert to JSON string
                string json = System.Text.Json.JsonSerializer.Serialize(themeCode);

                // Encode to Base64 for compact sharing
                byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
                string base64Code = Convert.ToBase64String(jsonBytes);

                // Copy to clipboard
                System.Windows.Clipboard.SetText(base64Code);

                ToastService.Success("Theme exported", "Theme code copied to clipboard!");
            }
            catch (Exception ex)
            {
                ToastService.Error("Export failed", $"Error: {ex.Message}");
            }
        }

        private void ValidateHexColor(string hexColor)
        {
            if (string.IsNullOrWhiteSpace(hexColor))
            {
                throw new ArgumentException("Color cannot be empty");
            }

            // Try to parse the color to validate it
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hexColor);
            }
            catch
            {
                throw new ArgumentException($"Invalid hex color: {hexColor}");
            }
        }
    }
}