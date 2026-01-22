using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Execor.Widgets
{
    /// <summary>
    /// Interaction logic for SessionDurationWidget.xaml
    /// </summary>
    public partial class SessionDurationWidget : UserControl
    {
        private DispatcherTimer _timer;
        private DateTime _sessionStartTime;

        public SessionDurationWidget()
        {
            InitializeComponent();
            _sessionStartTime = DateTime.Now; // Start time is when the widget is initialized

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Update every second
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Initial update
            UpdateDuration();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateDuration();
        }

        private void UpdateDuration()
        {
            TimeSpan elapsed = DateTime.Now - _sessionStartTime;
            DurationTextBlock.Text = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }

        // Stop the timer when the widget is unloaded to prevent memory leaks
        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _timer.Stop();
        }
    }
}
