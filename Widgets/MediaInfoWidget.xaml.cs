using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using Windows.Media.Control; // Requires specific Windows SDK references

namespace Execor.Widgets
{
    /// <summary>
    /// Interaction logic for MediaInfoWidget.xaml
    /// </summary>
    public partial class MediaInfoWidget : UserControl
    {
        private DispatcherTimer _timer;
        private GlobalSystemMediaTransportControlsSessionManager _mediaManager;
        private GlobalSystemMediaTransportControlsSession _currentSession;

        public MediaInfoWidget()
        {
            InitializeComponent();
            InitializeMediaControls();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2) // Check for media updates every 2 seconds
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Initial update
            UpdateMediaInfo();
        }

        private async void InitializeMediaControls()
        {
            try
            {
                _mediaManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                _mediaManager.CurrentSessionChanged += MediaManager_CurrentSessionChanged;
                MediaManager_CurrentSessionChanged(_mediaManager, null); // Initial check
            }
            catch (Exception ex)
            {
                // Handle exception if Media Controls are not available (e.g., older Windows version)
                TitleTextBlock.Text = "Media API Error";
                ArtistTextBlock.Text = ex.Message;
            }
        }

        private async void MediaManager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            _currentSession = sender.GetCurrentSession();
            await UpdateMediaInfo();
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            await UpdateMediaInfo();
        }

        private async System.Threading.Tasks.Task UpdateMediaInfo()
        {
            if (_currentSession == null)
            {
                TitleTextBlock.Text = "No Media Playing";
                ArtistTextBlock.Text = "";
                AlbumArtImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/steam_default.png"));
                return;
            }

            try
            {
                var mediaProperties = await _currentSession.TryGetMediaPropertiesAsync();
                var playbackInfo = _currentSession.GetPlaybackInfo();

                if (mediaProperties != null)
                {
                    TitleTextBlock.Text = string.IsNullOrEmpty(mediaProperties.Title) ? "Unknown Title" : mediaProperties.Title;
                    ArtistTextBlock.Text = string.IsNullOrEmpty(mediaProperties.Artist) ? "Unknown Artist" : mediaProperties.Artist;

                    if (mediaProperties.Thumbnail != null)
                    {
                        var stream = await mediaProperties.Thumbnail.OpenReadAsync();
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream.AsStream();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze(); // Freeze for cross-thread access
                        AlbumArtImage.Source = bitmap;
                    }
                    else
                    {
                        AlbumArtImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/steam_default.png"));
                    }
                }
                else
                {
                    TitleTextBlock.Text = "No Media Playing";
                    ArtistTextBlock.Text = "";
                    AlbumArtImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/steam_default.png"));
                }
            }
            catch (Exception ex)
            {
                TitleTextBlock.Text = "Media Error";
                ArtistTextBlock.Text = ex.Message;
                AlbumArtImage.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/steam_default.png"));
                _currentSession = null; // Reset session on error
            }
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _timer.Stop();
            if (_mediaManager != null)
            {
                _mediaManager.CurrentSessionChanged -= MediaManager_CurrentSessionChanged;
            }
        }
    }
}
