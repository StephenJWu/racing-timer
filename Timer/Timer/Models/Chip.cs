using System;

namespace Timer.Models
{
    /// <summary>
    /// 表示一个芯片，包含芯片的标签号码和内部编号
    /// </summary>
    public class Chip
    {
        /// <summary>
        /// 数据库主键
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 所属芯片组ID（外键关联ChipGroups）
        /// </summary>
        public int ChipGroupId { get; set; }

        /// <summary>
        /// 芯片标签号码，唯一
        /// </summary>
        public string LabelNumber { get; set; } = string.Empty;

        /// <summary>
        /// 芯片内部编号
        /// </summary>
        public string InternalNumber { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}

