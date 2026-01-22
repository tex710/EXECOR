using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Execor.Widgets;

namespace Execor.Widgets
{
    /// <summary>
    /// Interaction logic for CustomMessageWidget.xaml
    /// </summary>
    public partial class CustomMessageWidget : UserControl
    {
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(CustomMessageWidget), new PropertyMetadata("Custom Message"));

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public CustomMessageWidget()
        {
            InitializeComponent();
            // Initial state: show TextBlock, hide TextBox
            MessageTextBlock.Visibility = Visibility.Visible;
            MessageTextBox.Visibility = Visibility.Collapsed;
        }

        // Method to switch to edit mode (show TextBox)
        public void EnterEditMode()
        {
            MessageTextBlock.Visibility = Visibility.Collapsed;
            MessageTextBox.Visibility = Visibility.Visible;
            MessageTextBox.Focus();
            MessageTextBox.SelectAll();
        }

        // Method to exit edit mode (show TextBlock)
        public void ExitEditMode()
        {
            MessageTextBlock.Visibility = Visibility.Visible;
            MessageTextBox.Visibility = Visibility.Collapsed;
        }

        private void MessageTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ExitEditMode();
            // Potentially save the message via a callback/event if the WidgetConfig isn't directly bound
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Optionally unfocus or save on Enter
                var scope = FocusManager.GetFocusScope(MessageTextBox);
                FocusManager.SetFocusedElement(scope, null);
            }
        }
    }
}
