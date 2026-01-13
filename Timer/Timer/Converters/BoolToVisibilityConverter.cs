using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Timer.Converters
{
    /// <summary>
    /// 布尔值到可见性转换器，用于将bool值转换为Visibility枚举值
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 将bool值转换为Visibility
        /// </summary>
        /// <param name="value">bool值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数（未使用）</param>
        /// <param name="culture">区域信息</param>
        /// <returns>true返回Visible，false返回Collapsed</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        /// <summary>
        /// 将Visibility值转换回bool值
        /// </summary>
        /// <param name="value">Visibility值</param>
        /// <param name="targetType">目标类型</param>
        /// <param name="parameter">转换参数（未使用）</param>
        /// <param name="culture">区域信息</param>
        /// <returns>Visible返回true，其他返回false</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}

