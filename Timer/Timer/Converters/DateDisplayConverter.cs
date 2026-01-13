using System;
using System.Globalization;
using System.Windows.Data;

namespace Timer.Converters
{
    /// <summary>
    /// 日期显示格式转换器（用于前端显示）：将日期显示为 yyyy-M-d（示例：2026-1-8）
    /// 同时支持将用户输入的 yyyy-M-d / yyyy-MM-dd / 本地短日期格式 解析回 DateTime。
    /// </summary>
    public class DateDisplayConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            if (value is DateTime dateTime)
                return dateTime.ToString("yyyy-M-d", CultureInfo.InvariantCulture);

            if (value is DateTimeOffset dto)
                return dto.Date.ToString("yyyy-M-d", CultureInfo.InvariantCulture);

            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string dateString)
                return null;

            dateString = dateString.Trim();
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

            // 精确格式优先（允许不带前导零）
            var formats = new[] { "yyyy-M-d", "yyyy-MM-dd" };
            if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactDate))
                return exactDate.Date;

            // 兼容用户手动输入的本地短日期（如 7/1/2026）
            if (DateTime.TryParse(dateString, culture, DateTimeStyles.None, out var parsedDate))
                return parsedDate.Date;

            return null;
        }
    }
}

