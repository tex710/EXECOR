using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Execor.Widgets
{
    /// <summary>
    /// Interaction logic for TimeDateWidget.xaml
    /// </summary>
    public partial class TimeDateWidget : UserControl
    {
        private DispatcherTimer _timer;

        public TimeDateWidget()
        {
            InitializeComponent();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Initial update
            UpdateDateTime();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateDateTime();
        }

        private void UpdateDateTime()
        {
            TimeTextBlock.Text = DateTime.Now.ToString("HH:mm:ss");
            DateTextBlock.Text = DateTime.Now.ToString("dd/MM/yyyy");
        }

        // Stop the timer when the widget is unloaded to prevent memory leaks
        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _timer.Stop();
        }
    }
}
