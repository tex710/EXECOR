using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HackHelper
{
    public partial class SaleTrackerWindow : Window
    {
        private ObservableCollection<SaleItem> _allSales = new ObservableCollection<SaleItem>();
        private ObservableCollection<SaleItem> _filteredSales = new ObservableCollection<SaleItem>();
        private bool _wishlistFilterActive = false;
        private static readonly HttpClient _httpClient = new HttpClient();
        private bool _isLoading = false;
        private Services.SteamAuthService? _steamAuth;
        private List<string> _userWishlist = new List<string>();

        public SaleTrackerWindow()
        {
            InitializeComponent();
            SalesListBox.ItemsSource = _filteredSales;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Initialize Steam auth with API key from settings (optional)
            var settings = Services.SteamSettings.Load();
            _steamAuth = new Services.SteamAuthService(settings.ApiKey);

            UpdateAuthUI();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _steamAuth?.Dispose();
            Close();
        }

        private async void SteamLogin_Click(object sender, RoutedEventArgs e)
        {
            if (_steamAuth == null) return;

            if (_steamAuth.IsAuthenticated)
            {
                // Logout
                _steamAuth.Logout();
                _userWishlist.Clear();
                UpdateAuthUI();
                ApplyFilters();
            }
            else
            {
                // Login
                SteamLoginButton.Content = "🔄 Logging in...";
                SteamLoginButton.IsEnabled = false;

                bool success = await _steamAuth.AuthenticateAsync();

                if (success)
                {
                    // Fetch wishlist
                    await LoadWishlistAsync();
                    UpdateAuthUI();
                    ApplyFilters();
                }
                else
                {
                    MessageBox.Show("Failed to authenticate with Steam. Please try again.", "Authentication Failed",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    SteamLoginButton.Content = "🎮 Login with Steam";
                    SteamLoginButton.IsEnabled = true;
                }
            }
        }

        private async Task LoadWishlistAsync()
        {
            if (_steamAuth == null || !_steamAuth.IsAuthenticated) return;

            try
            {
                _userWishlist = await _steamAuth.GetWishlistAsync();

                // Update existing sales with wishlist status
                foreach (var sale in _allSales)
                {
                    sale.IsWishlisted = _userWishlist.Contains(sale.AppId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load wishlist: {ex.Message}");
                MessageBox.Show("Failed to load wishlist. Make sure your Steam profile is public.", "Wishlist Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateAuthUI()
        {
            if (_steamAuth?.IsAuthenticated == true)
            {
                SteamLoginButton.Content = $"👤 {_steamAuth.PersonaName ?? "Logout"}";
                SteamUserText.Text = $"Logged in as: {_steamAuth.PersonaName}";
                SteamUserText.Visibility = Visibility.Visible;
            }
            else
            {
                SteamLoginButton.Content = "🎮 Login with Steam";
                SteamUserText.Visibility = Visibility.Collapsed;
            }
            SteamLoginButton.IsEnabled = true;
        }

        private async void RefreshSales_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;

            _isLoading = true;
            StatusText.Text = "Loading sales from Steam...";
            StatusText.Visibility = Visibility.Visible;

            try
            {
                await LoadSteamSalesData();
                StatusText.Visibility = Visibility.Collapsed;
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error loading sales: {ex.Message}";
                MessageBox.Show($"Failed to load Steam sales: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task LoadSteamSalesData()
        {
            _allSales.Clear();

            try
            {
                // Get featured sales from Steam
                var featuredUrl = "https://store.steampowered.com/api/featuredcategories";
                var featuredResponse = await _httpClient.GetStringAsync(featuredUrl);
                var featuredData = JObject.Parse(featuredResponse);

                var salesList = new List<SaleItem>();

                // Process specials (games on sale)
                if (featuredData["specials"]?["items"] is JArray specials)
                {
                    foreach (var item in specials.Take(20)) // Limit to 20 items
                    {
                        try
                        {
                            var appId = item["id"]?.ToString();
                            if (string.IsNullOrEmpty(appId)) continue;

                            var name = item["name"]?.ToString() ?? "Unknown Game";
                            var discountPercent = item["discount_percent"]?.Value<int>() ?? 0;
                            var originalPrice = item["original_price"]?.Value<int>() ?? 0;
                            var finalPrice = item["final_price"]?.Value<int>() ?? 0;

                            // Convert cents to dollars
                            var originalPriceStr = originalPrice > 0 ? $"${originalPrice / 100.0:F2}" : "Free";
                            var finalPriceStr = finalPrice > 0 ? $"${finalPrice / 100.0:F2}" : "Free";

                            var sale = new SaleItem
                            {
                                GameName = name,
                                CurrentPrice = finalPriceStr,
                                OriginalPrice = originalPriceStr,
                                DiscountPercentage = discountPercent > 0 ? $"-{discountPercent}%" : "0%",
                                SaleEndDate = "Check Steam for end date",
                                AppId = appId,
                                IsWishlisted = _userWishlist.Contains(appId)
                            };

                            salesList.Add(sale);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error parsing sale item: {ex.Message}");
                            continue;
                        }
                    }
                }

                // If we didn't get enough items, try to get top sellers on sale
                if (salesList.Count < 10)
                {
                    await AddTopSellersOnSale(salesList);
                }

                // Add all sales to the collection
                foreach (var sale in salesList.OrderByDescending(s => GetDiscountValue(s.DiscountPercentage)))
                {
                    _allSales.Add(sale);
                }

                if (_allSales.Count == 0)
                {
                    // Add some fallback popular games if API fails
                    AddFallbackGames();
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadSteamSalesData: {ex.Message}");
                // Add fallback games if Steam API fails
                AddFallbackGames();
                ApplyFilters();
                throw;
            }
        }

        private async Task AddTopSellersOnSale(List<SaleItem> salesList)
        {
            try
            {
                // Get top sellers
                var topSellersUrl = "https://store.steampowered.com/api/featuredcategories";
                var response = await _httpClient.GetStringAsync(topSellersUrl);
                var data = JObject.Parse(response);

                if (data["top_sellers"]?["items"] is JArray topSellers)
                {
                    foreach (var item in topSellers.Take(10))
                    {
                        try
                        {
                            var discountPercent = item["discount_percent"]?.Value<int>() ?? 0;
                            if (discountPercent > 0) // Only add if on sale
                            {
                                var appId = item["id"]?.ToString();
                                if (string.IsNullOrEmpty(appId)) continue;

                                var name = item["name"]?.ToString() ?? "Unknown Game";
                                var originalPrice = item["original_price"]?.Value<int>() ?? 0;
                                var finalPrice = item["final_price"]?.Value<int>() ?? 0;

                                var originalPriceStr = originalPrice > 0 ? $"${originalPrice / 100.0:F2}" : "Free";
                                var finalPriceStr = finalPrice > 0 ? $"${finalPrice / 100.0:F2}" : "Free";

                                var sale = new SaleItem
                                {
                                    GameName = name,
                                    CurrentPrice = finalPriceStr,
                                    OriginalPrice = originalPriceStr,
                                    DiscountPercentage = $"-{discountPercent}%",
                                    SaleEndDate = "Check Steam for end date",
                                    AppId = appId,
                                    IsWishlisted = _userWishlist.Contains(appId)
                                };

                                salesList.Add(sale);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error parsing top seller: {ex.Message}");
                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting top sellers: {ex.Message}");
            }
        }

        private void AddFallbackGames()
        {
            // Popular games that are frequently on sale
            var fallbackGames = new List<SaleItem>
            {
                new SaleItem
                {
                    GameName = "Counter-Strike 2",
                    CurrentPrice = "Free",
                    OriginalPrice = "Free",
                    DiscountPercentage = "100%",
                    SaleEndDate = "Always Available",
                    AppId = "730",
                    IsWishlisted = false
                },
                new SaleItem
                {
                    GameName = "Click 'Refresh Sales' to load current Steam sales",
                    CurrentPrice = "$0.00",
                    OriginalPrice = "$0.00",
                    DiscountPercentage = "0%",
                    SaleEndDate = "N/A",
                    AppId = "0",
                    IsWishlisted = false
                }
            };

            foreach (var game in fallbackGames)
            {
                _allSales.Add(game);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ToggleWishlistFilter_Click(object sender, RoutedEventArgs e)
        {
            _wishlistFilterActive = !_wishlistFilterActive;
            WishlistFilterButton.Content = _wishlistFilterActive ? "✅ Wishlist Only" : "🌟 Wishlist Only";
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var searchText = SearchBox.Text?.ToLower() ?? "";

            var filtered = _allSales.Where(s =>
                (string.IsNullOrWhiteSpace(searchText) || s.GameName.ToLower().Contains(searchText)) &&
                (!_wishlistFilterActive || s.IsWishlisted)
            ).ToList();

            _filteredSales.Clear();
            foreach (var item in filtered)
            {
                _filteredSales.Add(item);
            }

            UpdateStatistics();
        }

        private void SortByDiscount_Click(object sender, RoutedEventArgs e)
        {
            var sorted = _filteredSales.OrderByDescending(s => GetDiscountValue(s.DiscountPercentage)).ToList();
            _filteredSales.Clear();
            foreach (var item in sorted)
            {
                _filteredSales.Add(item);
            }
        }

        private void SortByPrice_Click(object sender, RoutedEventArgs e)
        {
            var sorted = _filteredSales.OrderBy(s => GetPriceValue(s.CurrentPrice)).ToList();
            _filteredSales.Clear();
            foreach (var item in sorted)
            {
                _filteredSales.Add(item);
            }
        }

        private void SortByEndDate_Click(object sender, RoutedEventArgs e)
        {
            var sorted = _filteredSales.OrderBy(s => GetDaysRemaining(s.SaleEndDate)).ToList();
            _filteredSales.Clear();
            foreach (var item in sorted)
            {
                _filteredSales.Add(item);
            }
        }

        private void ViewOnSteam_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SaleItem sale)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = $"https://store.steampowered.com/app/{sale.AppId}",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open Steam page: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateStatistics()
        {
            TotalGamesText.Text = _filteredSales.Count.ToString();

            if (_filteredSales.Any())
            {
                var avgDiscount = _filteredSales
                    .Select(s => GetDiscountValue(s.DiscountPercentage))
                    .Where(d => d > 0)
                    .DefaultIfEmpty(0)
                    .Average();
                AvgDiscountText.Text = $"{avgDiscount:F0}%";

                var bestDeal = _filteredSales
                    .Where(s => GetDiscountValue(s.DiscountPercentage) > 0)
                    .OrderByDescending(s => GetDiscountValue(s.DiscountPercentage))
                    .FirstOrDefault();

                if (bestDeal != null)
                {
                    BestDealText.Text = $"{bestDeal.GameName} ({bestDeal.DiscountPercentage})";
                }
                else
                {
                    BestDealText.Text = "N/A";
                }
            }
            else
            {
                AvgDiscountText.Text = "0%";
                BestDealText.Text = "N/A";
            }
        }

        private int GetDiscountValue(string discount)
        {
            if (discount == "100%") return 100;

            var numStr = discount.Replace("-", "").Replace("%", "").Trim();
            return int.TryParse(numStr, out int value) ? value : 0;
        }

        private decimal GetPriceValue(string price)
        {
            if (price == "Free") return 0;

            var numStr = price.Replace("$", "").Trim();
            return decimal.TryParse(numStr, out decimal value) ? value : 0;
        }

        private int GetDaysRemaining(string saleEndDate)
        {
            if (saleEndDate == "Always Available") return int.MaxValue;

            // Extract number of days from "Sale ends in X days"
            var parts = saleEndDate.Split(' ');
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i + 1] == "days" || parts[i + 1] == "day")
                {
                    if (int.TryParse(parts[i], out int days))
                        return days;
                }
            }
            return int.MaxValue;
        }
    }

    // Model class for sale items
    public class SaleItem : INotifyPropertyChanged
    {
        private string _gameName = "";
        private string _currentPrice = "";
        private string _originalPrice = "";
        private string _discountPercentage = "";
        private string _saleEndDate = "";
        private string _appId = "";
        private bool _isWishlisted;

        public string GameName
        {
            get => _gameName;
            set { _gameName = value; OnPropertyChanged(nameof(GameName)); }
        }

        public string CurrentPrice
        {
            get => _currentPrice;
            set { _currentPrice = value; OnPropertyChanged(nameof(CurrentPrice)); }
        }

        public string OriginalPrice
        {
            get => _originalPrice;
            set { _originalPrice = value; OnPropertyChanged(nameof(OriginalPrice)); }
        }

        public string DiscountPercentage
        {
            get => _discountPercentage;
            set { _discountPercentage = value; OnPropertyChanged(nameof(DiscountPercentage)); }
        }

        public string SaleEndDate
        {
            get => _saleEndDate;
            set { _saleEndDate = value; OnPropertyChanged(nameof(SaleEndDate)); }
        }

        public string AppId
        {
            get => _appId;
            set { _appId = value; OnPropertyChanged(nameof(AppId)); }
        }

        public bool IsWishlisted
        {
            get => _isWishlisted;
            set { _isWishlisted = value; OnPropertyChanged(nameof(IsWishlisted)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}