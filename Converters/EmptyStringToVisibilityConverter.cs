using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Execor.Converters
{
    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrWhiteSpace(str))
            {
                return Visibility.Collapsed; // Has icon, hide default
            }
            return Visibility.Visible; // No icon, show default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}