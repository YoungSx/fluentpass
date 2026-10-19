using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace KeePassFluentUI.Common
{
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || value == DependencyProperty.UnsetValue)
            {
                bool inv = Invert;
                string? p = parameter as string;
                if (p != null && p.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                {
                    inv = !inv;
                }
                return inv ? Visibility.Visible : Visibility.Collapsed;
            }

            bool flag = false;
            if (value is bool b)
            {
                flag = b;
            }
            else if (value is int intVal)
            {
                flag = intVal > 0;
            }
            else if (value is string strVal)
            {
                if (bool.TryParse(strVal, out bool parsedBool))
                {
                    flag = parsedBool;
                }
                else
                {
                    flag = !string.IsNullOrEmpty(strVal);
                }
            }

            bool inverse = Invert;
            string? paramStr = parameter as string;
            if (paramStr != null && paramStr.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
            {
                inverse = !inverse;
            }

            if (inverse)
            {
                flag = !flag;
            }

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Visibility vis)
            {
                bool flag = (vis == Visibility.Visible);
                bool inverse = Invert;
                string? paramStr = parameter as string;
                if (paramStr != null && paramStr.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                {
                    inverse = !inverse;
                }

                return inverse ? !flag : flag;
            }

            return false;
        }
    }
}
