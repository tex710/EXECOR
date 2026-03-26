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

            // Instantiate OverlayWindow — its constructor loads settings and sets
            // its own visibility, so we must NOT call Show() here unconditionally.
            OverlayWindowInstance = new OverlayWindow();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            OverlayWindowInstance?.Close();
            base.OnExit(e);
        }
    }
}