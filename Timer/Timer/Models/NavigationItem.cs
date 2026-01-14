using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Timer.Models
{
    /// <summary>
    /// 导航菜单项模型，表示导航菜单中的一个菜单项，支持嵌套子菜单
    /// </summary>
    public class NavigationItem : ObservableObject
    {
        private string _title = string.Empty;
        private string? _icon;
        private string _iconColor = "#60a5fa";
        private bool _isExpanded;
        private bool _isSelected;
        private object? _viewModel;

        /// <summary>
        /// 菜单项显示文本
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 图标路径或字符（可选）
        /// </summary>
        public string? Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        /// <summary>
        /// 图标颜色（十六进制颜色值）
        /// </summary>
        public string IconColor
        {
            get => _iconColor;
            set => SetProperty(ref _iconColor, value);
        }

        /// <summary>
        /// 子菜单项集合（用于嵌套菜单）
        /// </summary>
        public ObservableCollection<NavigationItem> Children { get; } = new();

        /// <summary>
        /// 是否展开（用于父菜单项）
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// 导航命令
        /// </summary>
        public ICommand? Command { get; set; }

        /// <summary>
        /// 关联的ViewModel实例
        /// </summary>
        public object? ViewModel
        {
            get => _viewModel;
            set => SetProperty(ref _viewModel, value);
        }

        /// <summary>
        /// 是否有子菜单项
        /// </summary>
        public bool HasChildren => Children.Count > 0;
    }
}

