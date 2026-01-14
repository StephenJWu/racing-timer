using System;

namespace Timer.Models
{
    /// <summary>
    /// 表示一个芯片组，包含芯片组的基本信息和颜色
    /// </summary>
    public class ChipGroup
    {
        /// <summary>
        /// 数据库主键
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 芯片组名称，唯一
        /// </summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// 芯片组颜色（存储为ARGB十六进制字符串，如"#FF1890FF"）
        /// </summary>
        public string Color { get; set; } = "#FF1890FF"; // Default to blue

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 芯片数量（非持久化属性，用于UI显示）
        /// </summary>
        public int ChipCount { get; set; }
    }
}

