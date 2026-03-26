using Execor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Execor.Windows
{
    public partial class ManageTagsWindow : Window
    {
        private readonly SteamAccount _account;
        private AccountTag? _editingTag = null;
        private string _selectedColor = "#4CAF50";

        // Preset badge colors
        private static readonly List<string> PresetColors = new()
        {
            "#4CAF50", // Green
            "#2196F3", // Blue
            "#FF5722", // Red-orange
            "#FF9800", // Orange
            "#9C27B0", // Purple
            "#00BCD4", // Cyan
            "#F44336", // Red
            "#FFEB3B", // Yellow
            "#E91E63", // Pink
            "#607D8B", // Grey-blue
        };

        public ManageTagsWindow(SteamAccount account)
        {
            InitializeComponent();
            _account = account;
            AccountLabel.Text = $"Account: {account.AccountName} ({account.Username})";
            BuildColorPicker();
            RefreshTagsList();
        }

        // ── Color picker ─────────────────────────────────────────────────────
        private void BuildColorPicker()
        {
            ColorPickerPanel.Children.Clear();
            foreach (var hex in PresetColors)
            {
                var btn = new Border
                {
                    Width = 24,
                    Height = 24,
                    CornerRadius = new CornerRadius(12),
                    Margin = new Thickness(0, 0, 8, 0),
                    Cursor = Cursors.Hand,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)),
                    BorderThickness = new Thickness(hex == _selectedColor ? 2 : 0),
                    BorderBrush = Brushes.White,
                    Tag = hex,
                    ToolTip = hex
                };
                btn.MouseLeftButtonDown += ColorSwatch_Click;
                ColorPickerPanel.Children.Add(btn);
            }
        }

        private void ColorSwatch_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.Tag is string hex)
            {
                _selectedColor = hex;
                foreach (Border child in ColorPickerPanel.Children)
                    child.BorderThickness = new Thickness(child.Tag as string == hex ? 2 : 0);
            }
        }

        // ── Tags list ─────────────────────────────────────────────────────────
        private void RefreshTagsList()
        {
            TagsList.ItemsSource = null;
            TagsList.ItemsSource = _account.Tags;
            NoTagsLabel.Visibility = _account.Tags.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Countdown toggle ──────────────────────────────────────────────────
        private void EnableCountdownCheck_Changed(object sender, RoutedEventArgs e)
        {
            CountdownPanel.Visibility = EnableCountdownCheck.IsChecked == true
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // ── Parse duration from the amount box + unit dropdown ────────────────
        private bool TryParseDuration(out TimeSpan duration)
        {
            duration = TimeSpan.Zero;

            if (!double.TryParse(CountdownAmountBox.Text.Trim(), out double amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid positive number for the duration.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CountdownAmountBox.Focus();
                return false;
            }

            var unit = (CountdownUnitBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Days";
            duration = unit switch
            {
                "Hours" => TimeSpan.FromHours(amount),
                "Minutes" => TimeSpan.FromMinutes(amount),
                _ => TimeSpan.FromDays(amount),
            };

            return true;
        }

        // ── Save (add or update) tag ──────────────────────────────────────────
        private void SaveTag_Click(object sender, RoutedEventArgs e)
        {
            var label = TagLabelBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(label))
            {
                MessageBox.Show("Please enter a tag label.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                TagLabelBox.Focus();
                return;
            }

            DateTime? expiresAt = null;
            if (EnableCountdownCheck.IsChecked == true)
            {
                if (!TryParseDuration(out var duration)) return;
                expiresAt = DateTime.Now + duration;
            }

            if (_editingTag != null)
            {
                _editingTag.Label = label;
                _editingTag.Color = _selectedColor;
                _editingTag.ExpiresAt = expiresAt;
                _editingTag = null;
                FormTitle.Text = "ADD NEW TAG";
                SaveTagButton.Content = "+ ADD TAG";
            }
            else
            {
                if (_account.Tags.Any(t => t.Label.Equals(label, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("A tag with that label already exists on this account.", "Duplicate",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _account.Tags.Add(new AccountTag
                {
                    Label = label,
                    Color = _selectedColor,
                    ExpiresAt = expiresAt
                });
            }

            ClearForm();
            RefreshTagsList();
        }

        // ── Edit existing tag ─────────────────────────────────────────────────
        private void EditTag_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is AccountTag tag)
            {
                _editingTag = tag;
                TagLabelBox.Text = tag.Label;
                _selectedColor = tag.Color;
                BuildColorPicker();

                if (tag.ExpiresAt.HasValue)
                {
                    EnableCountdownCheck.IsChecked = true;
                    // Show remaining time as days (rounded), default to days unit
                    var remaining = tag.ExpiresAt.Value - DateTime.Now;
                    if (remaining.TotalMinutes < 60)
                    {
                        CountdownAmountBox.Text = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes)).ToString();
                        CountdownUnitBox.SelectedIndex = 2; // Minutes
                    }
                    else if (remaining.TotalHours < 24)
                    {
                        CountdownAmountBox.Text = Math.Max(1, (int)Math.Ceiling(remaining.TotalHours)).ToString();
                        CountdownUnitBox.SelectedIndex = 1; // Hours
                    }
                    else
                    {
                        CountdownAmountBox.Text = Math.Max(1, (int)Math.Ceiling(remaining.TotalDays)).ToString();
                        CountdownUnitBox.SelectedIndex = 0; // Days
                    }
                }
                else
                {
                    EnableCountdownCheck.IsChecked = false;
                }

                FormTitle.Text = "EDIT TAG";
                SaveTagButton.Content = "💾 SAVE CHANGES";
            }
        }

        // ── Delete tag ────────────────────────────────────────────────────────
        private void DeleteTag_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is AccountTag tag)
            {
                _account.Tags.Remove(tag);
                if (_editingTag?.Id == tag.Id) ClearForm();
                RefreshTagsList();
            }
        }

        // ── Clear form ────────────────────────────────────────────────────────
        private void ClearForm_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void ClearForm()
        {
            _editingTag = null;
            TagLabelBox.Text = string.Empty;
            EnableCountdownCheck.IsChecked = false;
            CountdownAmountBox.Text = "180";
            CountdownUnitBox.SelectedIndex = 0;
            _selectedColor = "#4CAF50";
            BuildColorPicker();
            FormTitle.Text = "ADD NEW TAG";
            SaveTagButton.Content = "+ ADD TAG";
        }

        // ── Window chrome ─────────────────────────────────────────────────────
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
