using System;

namespace Timer.Models
{
    /// <summary>
    /// 表示搜索和筛选条件
    /// </summary>
    public class SearchFilter
    {
        /// <summary>
        /// 搜索关键词（匹配姓名、准考证号、号码布）
        /// </summary>
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// 项目ID筛选
        /// </summary>
        public int? ProjectId { get; set; }

        /// <summary>
        /// 组别名称筛选
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// 性别筛选
        /// </summary>
        public string? Gender { get; set; }

        /// <summary>
        /// 学校筛选
        /// </summary>
        public string? School { get; set; }

        /// <summary>
        /// 年级筛选
        /// </summary>
        public string? Grade { get; set; }

        /// <summary>
        /// 班级筛选
        /// </summary>
        public string? Class { get; set; }

        /// <summary>
        /// 页码（从1开始）
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// 每页记录数
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 计算跳过的记录数（用于分页）
        /// </summary>
        public int Skip => (PageNumber - 1) * PageSize;
    }
}

