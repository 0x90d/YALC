using System;
using System.Windows;
using System.Windows.Data;

namespace YetAnotherLosslessCutter
{
    public sealed class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object trueValue, System.Globalization.CultureInfo culture)
        {
            return value?.GetType().IsEnum == true ? Equals(value, trueValue) : DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object trueValue, System.Globalization.CultureInfo culture)
        {
            return value is bool b && b ? trueValue : DependencyProperty.UnsetValue;
        }
    }
}
