using System;

namespace Timer.Models
{
    /// <summary>
    /// 表示一个比赛分组，包含学校、年级、班级、组别和芯片分配信息
    /// </summary>
    public class RaceGroup
    {
        /// <summary>
        /// 数据库主键
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 学校名称
        /// </summary>
        public string School { get; set; } = string.Empty;

        /// <summary>
        /// 年级
        /// </summary>
        public string? Grade { get; set; }

        /// <summary>
        /// 班级
        /// </summary>
        public string? Class { get; set; }

        /// <summary>
        /// 组别名称
        /// </summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// 关联的芯片组ID（外键关联ChipGroups）
        /// </summary>
        public int? ChipGroupId { get; set; }

        /// <summary>
        /// 比赛圈数（1-20）
        /// </summary>
        public int RaceLaps { get; set; } = 1;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 芯片组名称（非持久化属性，用于UI显示）
        /// </summary>
        public string? ChipGroupName { get; set; }

        /// <summary>
        /// 芯片组颜色（非持久化属性，用于UI显示）
        /// </summary>
        public string? ChipGroupColor { get; set; }

        /// <summary>
        /// 组内参赛人员数量（非持久化属性，用于UI显示）
        /// </summary>
        public int ParticipantCount { get; set; }

        /// <summary>
        /// 显示名称：年级-班级-组名（用于UI显示）
        /// </summary>
        public string DisplayName => $"{Grade}-{Class}-{GroupName}";
    }
}

