using System;
using System.Windows; // For Point, Size

namespace Execor.Models
{
    public class WidgetConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Unique ID for each widget instance
        public string WidgetType { get; set; } // e.g., "TimeDate", "SystemMetrics", "MediaInfo", "CustomMessage"
        public bool IsEnabled { get; set; } = true;
        public double Opacity { get; set; } = 1.0;
        public Point Position { get; set; } = new Point(0, 0); // X, Y coordinates on the overlay
        public Size Size { get; set; } = new Size(200, 100); // Width, Height
        public string CustomMessage { get; set; } = "Hello, Overlay!"; // For CustomMessageWidget
        // Add other widget-specific properties here as needed
        // e.g., for MediaInfoWidget: bool ShowAlbumArt, bool ShowProgressBar
        // e.g., for SystemMetricsWidget: bool ShowCPU, bool ShowGPU, bool ShowRAM, bool ShowNetwork

        public WidgetConfig() { } // Default constructor for deserialization
    }
}
