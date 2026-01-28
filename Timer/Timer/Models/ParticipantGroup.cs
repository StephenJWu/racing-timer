using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Timer.Models
{
    /// <summary>
    /// 参赛人员分组配置模型
    /// </summary>
    public partial class ParticipantGroup : ObservableObject
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [ObservableProperty]
        private int _id;

        /// <summary>
        /// 项目ID（外键关联Projects）
        /// </summary>
        [ObservableProperty]
        private int? _projectId;

        /// <summary>
        /// 学校名称
        /// </summary>
        [ObservableProperty]
        private string _school = string.Empty;

        /// <summary>
        /// 年级
        /// </summary>
        [ObservableProperty]
        private string? _grade;

        /// <summary>
        /// 班级
        /// </summary>
        [ObservableProperty]
        private string? _class;

        /// <summary>
        /// 组别名称
        /// </summary>
        [ObservableProperty]
        private string _groupName = string.Empty;

        /// <summary>
        /// 比赛圈数
        /// </summary>
        [ObservableProperty]
        private int _raceLaps = 1;

        /// <summary>
        /// 关联的芯片组ID（外键关联ChipGroups）
        /// </summary>
        [ObservableProperty]
        private int? _chipGroupId;

        /// <summary>
        /// 芯片组名称（冗余字段，便于查询）
        /// </summary>
        [ObservableProperty]
        private string? _chipGroupName;

        /// <summary>
        /// 创建时间
        /// </summary>
        [ObservableProperty]
        private DateTime _createdAt;

        /// <summary>
        /// 更新时间
        /// </summary>
        [ObservableProperty]
        private DateTime _updatedAt;

        /// <summary>
        /// 显示名称（用于UI显示）
        /// </summary>
        public string DisplayName
        {
            get
            {
                var parts = new List<string> { School };
                if (!string.IsNullOrWhiteSpace(Grade))
                {
                    parts.Add(Grade);
                }
                if (!string.IsNullOrWhiteSpace(Class))
                {
                    parts.Add(Class);
                }
                if (!string.IsNullOrWhiteSpace(GroupName))
                {
                    parts.Add(GroupName);
                }
                return string.Join(" - ", parts);
            }
        }
    }
}
