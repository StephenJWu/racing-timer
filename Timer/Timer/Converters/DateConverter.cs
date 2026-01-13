using System;
using System.Globalization;

namespace Timer.Converters
{
    /// <summary>
    /// 日期格式转换器，支持多种日期格式变体
    /// </summary>
    public static class DateConverter
    {
        /// <summary>
        /// 支持的日期格式列表
        /// </summary>
        private static readonly string[] DateFormats = new[]
        {
            "yyyy-M-d",
            "yyyy-MM-dd",
            "yyyy-M-d上午",
            "yyyy-MM-dd上午",
            "yyyy-M-d下午",
            "yyyy-MM-dd下午"
        };

        /// <summary>
        /// 解析日期字符串，支持多种格式
        /// </summary>
        /// <param name="dateString">日期字符串</param>
        /// <returns>解析后的DateTime，如果解析失败返回null</returns>
        public static DateTime? ParseDate(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
            {
                return null;
            }

            // 尝试解析各种格式
            foreach (var format in DateFormats)
            {
                if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    // 如果包含"下午"，将时间设置为12:00:00
                    if (dateString.Contains("下午"))
                    {
                        date = date.Date.AddHours(12);
                    }
                    // 如果包含"上午"或只有日期，将时间设置为00:00:00
                    else
                    {
                        date = date.Date;
                    }
                    return date;
                }
            }

            // 如果所有格式都失败，尝试使用标准DateTime.Parse
            if (DateTime.TryParse(dateString, out var parsedDate))
            {
                // 检查是否包含"下午"
                if (dateString.Contains("下午"))
                {
                    parsedDate = parsedDate.Date.AddHours(12);
                }
                else
                {
                    parsedDate = parsedDate.Date;
                }
                return parsedDate;
            }

            return null;
        }

        /// <summary>
        /// 将DateTime格式化为标准字符串（YYYY-MM-DD）
        /// </summary>
        /// <param name="date">日期对象</param>
        /// <returns>格式化的日期字符串</returns>
        public static string FormatDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 将DateTime格式化为带时间段的字符串（YYYY-MM-DD上午/下午）
        /// </summary>
        /// <param name="date">日期对象</param>
        /// <returns>格式化的日期字符串</returns>
        public static string FormatDateWithPeriod(DateTime date)
        {
            var period = date.Hour >= 12 ? "下午" : "上午";
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + period;
        }
    }
}

