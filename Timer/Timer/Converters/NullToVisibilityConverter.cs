using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Timer.Converters
{
    /// <summary>
    /// 将null值转换为Visibility的转换器
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 将值转换为Visibility
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isVisible = value != null;
            // 如果 parameter 是 "Invert"，则反转可见性
            if (parameter is string param && param == "Invert")
            {
                isVisible = !isVisible;
            }
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 将Visibility转换回值（不支持）
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

