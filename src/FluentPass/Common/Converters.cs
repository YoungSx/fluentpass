using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace FluentPass.Common
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
                flag = !string.IsNullOrEmpty(strVal);
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

    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool hasText = !string.IsNullOrEmpty(value as string);
            bool inverse = Invert;
            if (parameter is string paramStr && paramStr.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
            {
                inverse = !inverse;
            }

            if (inverse)
            {
                hasText = !hasText;
            }

            return hasText ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>布尔取反（bool → bool），用于 IsEnabled/IsHitTestVisible 等直接取反绑定。</summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !(value is bool b && b);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !(value is bool b && b);
        }
    }

    /// <summary>
    /// bool → InfoBarSeverity。用于过期警示条：
    /// “已过期”属事实性失效，需比“即将过期”高一级，不能两者共用 Warning。
    /// 转换放在视图层，VM 因此不依赖 Wpf.Ui 枚举。
    /// </summary>
    [ValueConversion(typeof(bool), typeof(InfoBarSeverity))]
    public class ExpiredToInfoBarSeverityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool expired = value is bool b && b;
            return expired ? InfoBarSeverity.Error : InfoBarSeverity.Warning;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// <see cref="UpdateBarSeverity"/> → <see cref="InfoBarSeverity"/>。
    /// 更新提示条三态（成功/需人工/失败）在视觉上必须可区分，不能一律 Warning。
    /// </summary>
    [ValueConversion(typeof(UpdateBarSeverity), typeof(InfoBarSeverity))]
    public class UpdateBarSeverityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is UpdateBarSeverity severity)
            {
                switch (severity)
                {
                    case UpdateBarSeverity.Success: return InfoBarSeverity.Success;
                    case UpdateBarSeverity.Warning: return InfoBarSeverity.Warning;
                    case UpdateBarSeverity.Error: return InfoBarSeverity.Error;
                    default: return InfoBarSeverity.Informational;
                }
            }

            return InfoBarSeverity.Informational;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
