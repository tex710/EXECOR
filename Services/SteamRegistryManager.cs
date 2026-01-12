using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace HackHelper.Services
{
    public class SteamRegistryManager
    {
        private const string STEAM_REGISTRY_PATH = @"Software\Valve\Steam";
        private const string STEAM_PROCESS_NAME = "steam";

        /* ----------  existing helpers unchanged  ---------- */
        public static string GetSteamPath()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(STEAM_REGISTRY_PATH))
                    if (key != null)
                    {
                        var steamPath = key.GetValue("SteamPath") as string;
                        if (!string.IsNullOrEmpty(steamPath)) return steamPath;
                    }

                string[] commonPaths = {
                    @"C:\Program Files (x86)\Steam",
                    @"C:\Program Files\Steam",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam")
                };
                foreach (var path in commonPaths)
                    if (Directory.Exists(path) && File.Exists(Path.Combine(path, "steam.exe")))
                        return path;
            }
            catch (Exception ex) { throw new Exception($"Failed to find Steam installation: {ex.Message}"); }
            throw new Exception("Steam installation not found.");
        }

        public static bool IsSteamRunning() => Process.GetProcessesByName(STEAM_PROCESS_NAME).Length > 0;

        public static bool CloseSteam(int timeoutSeconds = 15)
        {
            var procs = Process.GetProcessesByName(STEAM_PROCESS_NAME);
            if (procs.Length == 0) return true;

            // First attempt: graceful shutdown
            foreach (var p in procs)
            {
                try
                {
                    if (!p.HasExited)
                    {
                        p.CloseMainWindow();
                    }
                }
                catch { }
            }

            // Wait for graceful shutdown
            var sw = Stopwatch.StartNew();
            while (IsSteamRunning() && sw.Elapsed.TotalSeconds < timeoutSeconds)
            {
                Thread.Sleep(500);
            }

            // If still running, force kill all Steam processes
            if (IsSteamRunning())
            {
                procs = Process.GetProcessesByName(STEAM_PROCESS_NAME);
                foreach (var p in procs)
                {
                    try
                    {
                        if (!p.HasExited)
                        {
                            p.Kill();
                            p.WaitForExit(2000); // Wait up to 2 seconds for kill to complete
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to kill Steam process {p.Id}: {ex.Message}");
                    }
                }

                // Final check after force kill
                Thread.Sleep(1000);
            }

            return !IsSteamRunning();
        }

        public static void LaunchSteam()
        {
            var steamExe = Path.Combine(GetSteamPath(), "steam.exe");
            if (!File.Exists(steamExe)) throw new Exception("steam.exe not found.");
            Process.Start(new ProcessStartInfo { FileName = steamExe, UseShellExecute = true });
        }

        public static void SwitchAccount(string username, string password = null)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(STEAM_REGISTRY_PATH, true) ??
                             throw new Exception("Steam registry key not found."))
            {
                key.SetValue("AutoLoginUser", username, RegistryValueKind.String);
                key.SetValue("LastGameNameUsed", username, RegistryValueKind.String);
                key.SetValue("RememberPassword", string.IsNullOrEmpty(password) ? 0 : 1, RegistryValueKind.DWord);
            }
        }

        public static string GetCurrentAutoLoginUser()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(STEAM_REGISTRY_PATH))
                return key?.GetValue("AutoLoginUser") as string ?? string.Empty;
        }

        public static void ClearAutoLogin()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(STEAM_REGISTRY_PATH, true))
            {
                key?.SetValue("AutoLoginUser", string.Empty, RegistryValueKind.String);
                key?.SetValue("RememberPassword", 0, RegistryValueKind.DWord);
            }
        }

        public static SteamInfo GetSteamInfo() =>
            new SteamInfo
            {
                IsInstalled = Directory.Exists(GetSteamPath()),
                InstallPath = GetSteamPath(),
                IsRunning = IsSteamRunning(),
                CurrentAutoLoginUser = GetCurrentAutoLoginUser()
            };

        /* ----------  THE ONLY METHOD YOU ACTUALLY CALL  ---------- */
        public static void PerformFullAccountSwitch(string username, string password = null, bool launchSteam = true)
        {
            // 1. kill Steam with better error handling
            if (IsSteamRunning())
            {
                Debug.WriteLine("[Steam] Attempting to close Steam...");

                bool closed = CloseSteam(15); // Increased timeout to 15 seconds

                if (!closed)
                {
                    // More detailed error message
                    var remainingProcs = Process.GetProcessesByName(STEAM_PROCESS_NAME);
                    var procInfo = string.Join(", ", remainingProcs.Select(p => $"PID:{p.Id}"));
                    throw new Exception($"Failed to close Steam after 15 seconds. Remaining processes: {procInfo}. Try closing Steam manually.");
                }

                Debug.WriteLine("[Steam] Steam closed successfully.");
            }

            Thread.Sleep(1500); // Increased wait time

            // 2. registry (same as SAM)
            SwitchAccount(username, password);

            if (!launchSteam) return;

            Thread.Sleep(500);

            // 3. start Steam *without* cached credentials -> forces login box
            ClearAutoLogin();
            var steamExe = Path.Combine(GetSteamPath(), "steam.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = steamExe,
                Arguments = "-noreactlogin",   // old-style login prompt
                UseShellExecute = true
            });

            // 4. wait until window exists AND is ready, then inject
            Thread.Sleep(2000);   // let process start
            if (!string.IsNullOrEmpty(password))
            {
                try { SteamLoginInjector.InjectLogin(username, password); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Steam] Injection failed: {ex.Message}");
                }
            }
        }
    }

    public class SteamInfo
    {
        public bool IsInstalled { get; set; }
        public string InstallPath { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
        public string CurrentAutoLoginUser { get; set; } = string.Empty;
    }
}