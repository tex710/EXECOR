using Execor.Models;
using Execor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Execor
{
    public partial class StatisticsWindow : Window
    {
        private List<Launcher> launchers;
        private StatisticsService statsService;

        public StatisticsWindow(List<Launcher> launchers)
        {
            InitializeComponent();
            this.launchers = launchers;
            this.statsService = new StatisticsService(launchers);
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            // Overview Stats
            TotalLaunchesText.Text = statsService.GetTotalLaunches().ToString();
            TotalLoadersText.Text = statsService.GetTotalLoaders().ToString();
            MostUsedGameText.Text = statsService.GetMostUsedGame();

            // Top 5 Loaders
            var topLaunchers = statsService.GetTopLaunchers(5);
            var topLaunchersWithRank = topLaunchers.Select((l, index) => new
            {
                Rank = (index + 1).ToString(),
                Name = l.Name,
                GameType = l.GameType,
                LaunchCount = l.LaunchCount
            }).ToList();
            TopLoadersListBox.ItemsSource = topLaunchersWithRank;

            // Game Distribution
            var distribution = statsService.GetGameDistributionPercentages();
            var distributionData = distribution.Select(kvp => new
            {
                GameType = kvp.Key,
                Percentage = kvp.Value,
                PercentageText = $"{kvp.Value:F1}%",
                BarWidthFull = (kvp.Value / 100.0) * 600 // 600px is the width of the bar container
            }).ToList();
            GameDistributionListBox.ItemsSource = distributionData;

            // Recent Activity
            var recentActivity = statsService.GetRecentActivity(10);
            var activityData = recentActivity.Select(a => new
            {
                LauncherName = a.LauncherName,
                GameType = a.GameType,
                TimeAgo = GetTimeAgo(a.Timestamp)
            }).ToList();
            RecentActivityListBox.ItemsSource = activityData;
        }

        private string GetTimeAgo(DateTime timestamp)
        {
            var timeSpan = DateTime.Now - timestamp;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)}w ago";

            return timestamp.ToString("MMM dd, yyyy");
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}