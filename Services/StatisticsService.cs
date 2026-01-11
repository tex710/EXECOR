using HackHelper.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HackHelper.Services
{
    public class StatisticsService
    {
        private List<Launcher> launchers;

        public StatisticsService(List<Launcher> launchers)
        {
            this.launchers = launchers;
        }

        // Total number of launches across all loaders
        public int GetTotalLaunches()
        {
            return launchers.Sum(l => l.LaunchCount);
        }

        // Total number of loaders
        public int GetTotalLoaders()
        {
            return launchers.Count;
        }

        // Most used game type
        public string GetMostUsedGame()
        {
            if (launchers.Count == 0) return "None";

            var gameGroups = launchers
                .GroupBy(l => l.GameType)
                .Select(g => new { Game = g.Key, TotalLaunches = g.Sum(l => l.LaunchCount) })
                .OrderByDescending(g => g.TotalLaunches)
                .FirstOrDefault();

            return gameGroups?.Game ?? "None";
        }

        // Top N most launched loaders
        public List<Launcher> GetTopLaunchers(int count = 5)
        {
            return launchers
                .OrderByDescending(l => l.LaunchCount)
                .Take(count)
                .ToList();
        }

        // Game distribution (CS2, CSGO, Other)
        public Dictionary<string, int> GetGameDistribution()
        {
            return launchers
                .GroupBy(l => l.GameType)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(l => l.LaunchCount)
                );
        }

        // Game distribution percentages
        public Dictionary<string, double> GetGameDistributionPercentages()
        {
            var total = GetTotalLaunches();
            if (total == 0) return new Dictionary<string, double>();

            var distribution = GetGameDistribution();
            return distribution.ToDictionary(
                kvp => kvp.Key,
                kvp => (double)kvp.Value / total * 100
            );
        }

        // Recent activity (last N launches with timestamps)
        public List<LauncherActivity> GetRecentActivity(int count = 10)
        {
            var activities = new List<LauncherActivity>();

            foreach (var launcher in launchers)
            {
                if (launcher.LaunchHistory != null)
                {
                    foreach (var timestamp in launcher.LaunchHistory)
                    {
                        activities.Add(new LauncherActivity
                        {
                            LauncherName = launcher.Name,
                            GameType = launcher.GameType,
                            Timestamp = timestamp
                        });
                    }
                }
            }

            return activities
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToList();
        }

        // Launches in last N days
        public int GetLaunchesInLastDays(int days)
        {
            var cutoff = DateTime.Now.AddDays(-days);
            int count = 0;

            foreach (var launcher in launchers)
            {
                if (launcher.LaunchHistory != null)
                {
                    count += launcher.LaunchHistory.Count(t => t >= cutoff);
                }
            }

            return count;
        }

        // Most active day of week
        public string GetMostActiveDay()
        {
            var dayCounts = new Dictionary<DayOfWeek, int>();

            foreach (var launcher in launchers)
            {
                if (launcher.LaunchHistory != null)
                {
                    foreach (var timestamp in launcher.LaunchHistory)
                    {
                        if (!dayCounts.ContainsKey(timestamp.DayOfWeek))
                            dayCounts[timestamp.DayOfWeek] = 0;

                        dayCounts[timestamp.DayOfWeek]++;
                    }
                }
            }

            if (dayCounts.Count == 0) return "No data";

            var mostActive = dayCounts.OrderByDescending(kvp => kvp.Value).First();
            return mostActive.Key.ToString();
        }
    }

    // Helper class for recent activity
    public class LauncherActivity
    {
        public string LauncherName { get; set; }
        public string GameType { get; set; }
        public DateTime Timestamp { get; set; }
    }
}