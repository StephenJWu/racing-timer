using System;

namespace Timer.Models
{
    /// <summary>
    /// 表示一个参赛人员，包含人员的基本信息和比赛相关信息
    /// </summary>
    public class Participant
    {
        /// <summary>
        /// 数据库主键
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 序号，必须从1开始连续
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// 日期（比赛日期）
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 学校
        /// </summary>
        public string? School { get; set; }

        /// <summary>
        /// 年级
        /// </summary>
        public string? Grade { get; set; }

        /// <summary>
        /// 班级
        /// </summary>
        public string? Class { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 性别（"男"或"女"）
        /// </summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>
        /// 准考证号，唯一标识（如果提供）
        /// </summary>
        public string? ExamNumber { get; set; }

        /// <summary>
        /// 组别名称
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// 号码布编号（可选，后续分配）
        /// </summary>
        public string? BibNumber { get; set; }

        /// <summary>
        /// 芯片编号（可选，后续分配）
        /// </summary>
        public string? ChipNumber { get; set; }

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

