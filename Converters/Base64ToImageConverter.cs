using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.ComponentModel;
using Execor.Services;

namespace Execor.Converters
{
    public class Base64ToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Prevent designer / IntelliSense from executing runtime code
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return null;

            if (value is string base64String && !string.IsNullOrWhiteSpace(base64String))
            {
                try
                {
                    return IconExtractor.Base64ToImage(base64String);
                }
                catch
                {
                    // Never crash the designer or UI thread
                    return null;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
