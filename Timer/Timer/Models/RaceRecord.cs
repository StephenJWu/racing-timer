using System;

namespace Timer.Models
{
    /// <summary>
    /// 比赛记录状态枚举
    /// </summary>
    public enum RaceStatus
    {
        /// <summary>
        /// 待开始
        /// </summary>
        Pending,
        
        /// <summary>
        /// 比赛进行中
        /// </summary>
        Running,
        
        /// <summary>
        /// 已暂停
        /// </summary>
        Paused,
        
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        
        /// <summary>
        /// 已停止（中途终止）
        /// </summary>
        Stopped
    }

    /// <summary>
    /// 表示一个参赛人员的比赛记录
    /// </summary>
    public class RaceRecord
    {
        /// <summary>
        /// 数据库主键
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 关联的比赛分组ID（外键关联RaceGroups）
        /// </summary>
        public int RaceGroupId { get; set; }

        /// <summary>
        /// 关联的项目ID（外键关联Projects）
        /// </summary>
        public int ProjectId { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        public int SequenceNumber { get; set; }

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
        /// 芯片外部号码（号码布）
        /// </summary>
        public string? LabelNumber { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 性别
        /// </summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>
        /// 1圈计时（格式：HH:mm:ss.fff）
        /// </summary>
        public string Lap1Time { get; set; } = "00:00:00.000";

        /// <summary>
        /// 2圈计时（格式：HH:mm:ss.fff）
        /// </summary>
        public string Lap2Time { get; set; } = "00:00:00.000";

        /// <summary>
        /// 总计时（格式：HH:mm:ss.fff）
        /// </summary>
        public string TotalTime { get; set; } = "00:00:00.000";

        /// <summary>
        /// 比赛状态
        /// </summary>
        public RaceStatus Status { get; set; } = RaceStatus.Pending;

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

