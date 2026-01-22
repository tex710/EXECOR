using System.Configuration;
using System.Data;
using System.Windows;
using Execor.Windows; // Added for OverlayWindow

namespace Execor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static OverlayWindow OverlayWindowInstance { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Instantiate the OverlayWindow but don't show it yet. Its visibility is controlled by its internal settings.
            OverlayWindowInstance = new OverlayWindow();
            OverlayWindowInstance.Show(); // Show it initially, its visibility will be handled by OverlayWindow itself based on settings
        }

        protected override void OnExit(ExitEventArgs e)
        {
            OverlayWindowInstance?.Close();
            base.OnExit(e);
        }
    }
}
