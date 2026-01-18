using Execor.Models;
using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Execor
{
    public partial class ToastWindow : Window
    {
        private DispatcherTimer timer;
        private DispatcherTimer countdownTimer;
        private DateTime startTime;
        private int durationSeconds;

        public ToastWindow(ToastNotification notification)
        {
            InitializeComponent();

            this.durationSeconds = notification.DurationSeconds;

            // Set content
            TitleText.Text = notification.Title;
            MessageText.Text = notification.Message;

            // Set icon and color based on type
            switch (notification.Type)
            {
                case ToastType.Success:
                    IconText.Text = "✓";
                    IconText.Foreground = System.Windows.Media.Brushes.LimeGreen;
                    break;
                case ToastType.Error:
                    IconText.Text = "✕";
                    IconText.Foreground = System.Windows.Media.Brushes.Red;
                    break;
                case ToastType.Warning:
                    IconText.Text = "⚠";
                    IconText.Foreground = System.Windows.Media.Brushes.Orange;
                    break;
                case ToastType.Info:
                    IconText.Text = "ℹ";
                    IconText.Foreground = System.Windows.Media.Brushes.White;
                    break;
            }

            // Position toast in top-right corner
            PositionToast();

            // Start auto-dismiss timer
            StartTimer();

            // Show/hide progress bar
            if (notification.ShowProgress)
            {
                ProgressBar.Visibility = Visibility.Visible;
                StartProgressBar();
            }

            // Slide-in animation
            SlideIn();
        }

        private void PositionToast()
        {
            var workingArea = SystemParameters.WorkArea;
            this.Left = workingArea.Right - this.Width - 20;
            this.Top = 20;
        }

        private void StartTimer()
        {
            startTime = DateTime.Now;

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(durationSeconds)
            };

            timer.Tick += (s, e) =>
            {
                timer.Stop();
                SlideOut();
            };

            timer.Start();
        }

        private void StartProgressBar()
        {
            ProgressBar.Value = 0;

            // Smooth progress animation
            var progressAnimation = new DoubleAnimation
            {
                From = 0,
                To = 100,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseIn
                },
                FillBehavior = FillBehavior.HoldEnd
            };

            ProgressBar.BeginAnimation(
                 System.Windows.Controls.Primitives.RangeBase.ValueProperty,
                 progressAnimation
                    );


            // Countdown text updater (1s resolution)
            countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            countdownTimer.Tick += (s, e) =>
            {
                var elapsed = (DateTime.Now - startTime).TotalSeconds;
                var remaining = Math.Max(0, durationSeconds - (int)elapsed);

                if (MessageText.Text.Contains("seconds"))
                    MessageText.Text = $"Clearing in {remaining} seconds...";

                if (remaining <= 0)
                    countdownTimer.Stop();
            };

            countdownTimer.Start();
        }

        private void SlideIn()
        {
            // Start off-screen to the right
            this.Left = SystemParameters.WorkArea.Right;

            var animation = new DoubleAnimation
            {
                From = SystemParameters.WorkArea.Right,
                To = SystemParameters.WorkArea.Right - this.Width - 20,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            this.BeginAnimation(Window.LeftProperty, animation);
        }

        private void SlideOut()
        {
            var animation = new DoubleAnimation
            {
                From = this.Left,
                To = SystemParameters.WorkArea.Right,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            animation.Completed += (s, e) => this.Close();

            this.BeginAnimation(Window.LeftProperty, animation);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            timer?.Stop();
            countdownTimer?.Stop();
            SlideOut();
        }
    }
}
