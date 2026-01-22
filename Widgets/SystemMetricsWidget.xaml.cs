using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Execor.Widgets
{
    /// <summary>
    /// Interaction logic for SystemMetricsWidget.xaml
    /// </summary>
    public partial class SystemMetricsWidget : UserControl
    {
        private DispatcherTimer _timer;

        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramCounter;
        private PerformanceCounter _netDownCounter;
        private PerformanceCounter _netUpCounter;

        public SystemMetricsWidget()
        {
            InitializeComponent();
            InitializePerformanceCounters();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Update every second
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Initial update
            UpdateMetrics();
        }

        private void InitializePerformanceCounters()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            // Find the active network interface for network counters
            string networkInterface = GetActiveNetworkInterface();
            if (!string.IsNullOrEmpty(networkInterface))
            {
                _netDownCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", networkInterface);
                _netUpCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", networkInterface);
            }
        }

        private string GetActiveNetworkInterface()
        {
            // This is a simplified way to get a network interface.
            // In a real application, you might want to allow the user to select one.
            var ni = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                                     n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback &&
                                     n.GetIPProperties().GatewayAddresses.Any());
            return ni?.Description;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateMetrics();
        }

        private void UpdateMetrics()
        {
            // CPU Usage
            CpuUsageTextBlock.Text = $"{_cpuCounter.NextValue():0.0}%";

            // RAM Usage
            long totalRam = GetTotalPhysicalMemory();
            long availableRam = (long)_ramCounter.NextValue();
            long usedRam = totalRam - availableRam;
            double usedRamPercent = (double)usedRam / totalRam * 100;
            RamUsageTextBlock.Text = $"{usedRam / 1024}GB / {totalRam / 1024}GB ({usedRamPercent:0.0}%)"; // Display in GB

            // Network Usage
            if (_netDownCounter != null && _netUpCounter != null)
            {
                // Convert bytes/sec to KB/s
                NetDownTextBlock.Text = $"{(_netDownCounter.NextValue() / 1024):0.0} KB/s";
                NetUpTextBlock.Text = $"{(_netUpCounter.NextValue() / 1024):0.0} KB/s";
            }
        }

        private long GetTotalPhysicalMemory()
        {
            // Get total RAM once (it doesn't change)
            using (var mos = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
            {
                foreach (var o in mos.Get())
                {
                    return Convert.ToInt64(o["TotalPhysicalMemory"]) / (1024 * 1024); // Return in MB
                }
            }
            return 0;
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _timer.Stop();
            _cpuCounter?.Dispose();
            _ramCounter?.Dispose();
            _netDownCounter?.Dispose();
            _netUpCounter?.Dispose();
        }
    }
}
