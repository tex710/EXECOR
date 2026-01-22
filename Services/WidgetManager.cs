using Execor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Execor.Widgets;

namespace Execor.Services
{
    public class WidgetManager
    {
        private Canvas _overlayCanvas;
        private List<WidgetConfig> _widgetConfigs;
        private bool _isEditMode;

        // Keep track of the currently dragged widget
        private FrameworkElement _draggingWidget = null;
        private Point _dragStartPoint;

        public event Action<List<WidgetConfig>> WidgetsUpdated; // Event to notify parent window of changes

        public WidgetManager(Canvas overlayCanvas, List<WidgetConfig> widgetConfigs)
        {
            _overlayCanvas = overlayCanvas;
            _widgetConfigs = widgetConfigs;
            _overlayCanvas.PreviewMouseLeftButtonDown += OverlayCanvas_PreviewMouseLeftButtonDown;
            _overlayCanvas.PreviewMouseMove += OverlayCanvas_PreviewMouseMove;
            _overlayCanvas.PreviewMouseLeftButtonUp += OverlayCanvas_PreviewMouseLeftButtonUp;
        }

        public void SetEditMode(bool isEditMode)
        {
            _isEditMode = isEditMode;
            foreach (UIElement child in _overlayCanvas.Children)
            {
                child.IsHitTestVisible = isEditMode;
                // If it's a CustomMessageWidget, toggle its internal edit state
                if (child is CustomMessageWidget messageWidget)
                {
                    if (isEditMode)
                    {
                        messageWidget.EnterEditMode();
                    }
                    else
                    {
                        messageWidget.ExitEditMode();
                        // Save the updated message back to WidgetConfig
                        if (messageWidget.Tag is WidgetConfig config)
                        {
                            config.CustomMessage = messageWidget.Message;
                            WidgetsUpdated?.Invoke(_widgetConfigs); // Notify for saving
                        }
                    }
                }
            }
        }

        public void LoadWidgets()
        {
            _overlayCanvas.Children.Clear(); // Clear existing widgets

            foreach (var config in _widgetConfigs.Where(w => w.IsEnabled))
            {
                FrameworkElement widget = CreateWidgetInstance(config);
                if (widget != null)
                {
                    // Set common properties
                    widget.Opacity = config.Opacity;
                    Canvas.SetLeft(widget, config.Position.X);
                    Canvas.SetTop(widget, config.Position.Y);
                    widget.Width = config.Size.Width;
                    widget.Height = config.Size.Height;

                    // Add to canvas
                    _overlayCanvas.Children.Add(widget);

                    // Attach event handlers for dragging if in edit mode initially
                    // Actual dragging will be controlled by _isEditMode set later
                    AttachDragHandlers(widget, config);
                }
            }
        }

        private FrameworkElement CreateWidgetInstance(WidgetConfig config)
        {
            // This is a factory method that will create different UserControls
            // based on config.WidgetType. For now, a placeholder.
            switch (config.WidgetType)
            {
                case "TimeDate":
                    return new TimeDateWidget(); // Now creating an actual instance
                case "SystemMetrics":
                    return new SystemMetricsWidget(); // Now creating an actual instance
                case "MediaInfo":
                    return new MediaInfoWidget(); // Now creating an actual instance
                case "CustomMessage":
                    var customMessageWidget = new CustomMessageWidget { Message = config.CustomMessage }; // Pass message
                    return customMessageWidget;
                case "SessionDuration":
                    return new SessionDurationWidget();
                default:
                    return new TextBlock { Text = $"Unknown Widget: {config.WidgetType}" };
            }
        }

        private void AttachDragHandlers(FrameworkElement widget, WidgetConfig config)
        {
            // Store config with the UIElement to retrieve it during drag operations
            widget.Tag = config;
            widget.MouseLeftButtonDown += Widget_MouseLeftButtonDown;
            // widget.MouseRightButtonDown += Widget_MouseRightButtonDown; // For context menu/config
        }

        private void OverlayCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isEditMode) return;

            // Find the widget that was clicked
            _draggingWidget = null;
            var hitTestResult = VisualTreeHelper.HitTest(_overlayCanvas, e.GetPosition(_overlayCanvas));
            if (hitTestResult != null && hitTestResult.VisualHit is FrameworkElement element && element.Tag is WidgetConfig)
            {
                // Find the actual top-level widget element (UserControl or whatever wraps the content)
                // This might need refinement based on how actual widgets are structured
                _draggingWidget = FindParentWidget(element);
                if (_draggingWidget != null)
                {
                    _dragStartPoint = e.GetPosition(_overlayCanvas);
                    _draggingWidget.CaptureMouse();
                }
            }
        }

        // Helper to find the actual widget control that has the WidgetConfig tag
        private FrameworkElement FindParentWidget(FrameworkElement element)
        {
            FrameworkElement current = element;
            while (current != null && !(current.Tag is WidgetConfig))
            {
                current = VisualTreeHelper.GetParent(current) as FrameworkElement;
            }
            return current;
        }


        private void OverlayCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isEditMode || _draggingWidget == null || !e.LeftButton.HasFlag(MouseButtonState.Pressed)) return;

            Point currentPoint = e.GetPosition(_overlayCanvas);
            double offsetX = currentPoint.X - _dragStartPoint.X;
            double offsetY = currentPoint.Y - _dragStartPoint.Y;

            double newLeft = Canvas.GetLeft(_draggingWidget) + offsetX;
            double newTop = Canvas.GetTop(_draggingWidget) + offsetY;

            // Constrain movement within canvas bounds (optional but good practice)
            newLeft = Math.Max(0, Math.Min(newLeft, _overlayCanvas.ActualWidth - _draggingWidget.ActualWidth));
            newTop = Math.Max(0, Math.Min(newTop, _overlayCanvas.ActualHeight - _draggingWidget.ActualHeight));


            Canvas.SetLeft(_draggingWidget, newLeft);
            Canvas.SetTop(_draggingWidget, newTop);

            _dragStartPoint = currentPoint;
        }

        private void OverlayCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggingWidget != null)
            {
                _draggingWidget.ReleaseMouseCapture();

                // Update the position in the WidgetConfig and save
                if (_draggingWidget.Tag is WidgetConfig config)
                {
                    config.Position = new Point(Canvas.GetLeft(_draggingWidget), Canvas.GetTop(_draggingWidget));
                    // The WidgetManager itself doesn't save directly, it notifies the parent OverlayWindow
                    // which then uses DataService to save the entire profile.
                    WidgetsUpdated?.Invoke(_widgetConfigs);
                }
                _draggingWidget = null;
            }
        }

        // Widget-specific event handlers (can be expanded)
        private void Widget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // If in edit mode, this will trigger the dragging logic in OverlayCanvas_PreviewMouseLeftButtonDown
            // If not in edit mode, this event should not fire if IsHitTestVisible is false on the widget
            if (_isEditMode)
            {
                e.Handled = true; // Mark as handled to prevent further processing on the canvas if starting drag
            }
        }
    }
}