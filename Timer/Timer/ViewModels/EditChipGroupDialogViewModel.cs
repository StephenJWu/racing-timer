using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Timer.Models;
using Timer.Services;

namespace Timer.ViewModels
{
    /// <summary>
    /// 预定义颜色项
    /// </summary>
    public class ColorItem
    {
        public string Name { get; set; } = string.Empty;
        public string HexColor { get; set; } = string.Empty;
    }

    /// <summary>
    /// 编辑芯片组对话框的ViewModel
    /// </summary>
    public class EditChipGroupDialogViewModel : ObservableObject
    {
        private readonly IChipRepository _repository;
        private readonly ILoggingService? _loggingService;
        private readonly string _originalGroupName;

        private string _groupName = string.Empty;
        private ColorItem? _selectedColor;

        /// <summary>
        /// 初始化编辑芯片组对话框ViewModel
        /// </summary>
        public EditChipGroupDialogViewModel(ChipGroup chipGroup, IChipRepository repository, ILoggingService? loggingService = null)
        {
            ArgumentNullException.ThrowIfNull(chipGroup);
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _loggingService = loggingService;

            Id = chipGroup.Id;
            _originalGroupName = chipGroup.ChipGroupName;
            GroupName = chipGroup.ChipGroupName;

            // 初始化预定义颜色列表
            PredefinedColors = new ObservableCollection<ColorItem>
            {
                new ColorItem { Name = "蓝色", HexColor = "#FF1890FF" },
                new ColorItem { Name = "红色", HexColor = "#FFFF4D4F" },
                new ColorItem { Name = "绿色", HexColor = "#FF52C41A" },
                new ColorItem { Name = "黄色", HexColor = "#FFFAAD14" },
                new ColorItem { Name = "橙色", HexColor = "#FFFF8C00" },
                new ColorItem { Name = "紫色", HexColor = "#FF722ED1" },
                new ColorItem { Name = "青色", HexColor = "#FF13C2C2" },
                new ColorItem { Name = "粉色", HexColor = "#FFEB2F96" },
                new ColorItem { Name = "灰色", HexColor = "#FF8C8C8C" },
                new ColorItem { Name = "深蓝", HexColor = "#FF2F54EB" }
            };

            // 选中当前颜色
            SelectedColor = PredefinedColors.FirstOrDefault(c => 
                c.HexColor.Equals(chipGroup.Color, StringComparison.OrdinalIgnoreCase)) 
                ?? PredefinedColors.First();

            ValidationErrors = new ObservableCollection<string>();
        }

        /// <summary>
        /// 芯片组Id
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// 预定义颜色列表
        /// </summary>
        public ObservableCollection<ColorItem> PredefinedColors { get; }

        /// <summary>
        /// 校验错误列表
        /// </summary>
        public ObservableCollection<string> ValidationErrors { get; }

        /// <summary>
        /// 是否存在校验错误
        /// </summary>
        public bool HasErrors => ValidationErrors.Count > 0;

        /// <summary>
        /// 芯片组名称
        /// </summary>
        public string GroupName
        {
            get => _groupName;
            set => SetProperty(ref _groupName, value);
        }

        /// <summary>
        /// 选中的颜色
        /// </summary>
        public ColorItem? SelectedColor
        {
            get => _selectedColor;
            set => SetProperty(ref _selectedColor, value);
        }

        /// <summary>
        /// 将当前编辑值组装为ChipGroup对象
        /// </summary>
        public ChipGroup ToChipGroup()
        {
            return new ChipGroup
            {
                Id = Id,
                ChipGroupName = GroupName?.Trim() ?? string.Empty,
                Color = SelectedColor?.HexColor ?? "#FF1890FF",
                UpdatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 执行校验（包括组名唯一性校验）
        /// </summary>
        public async Task<bool> ValidateAsync()
        {
            ValidationErrors.Clear();

            if (string.IsNullOrWhiteSpace(GroupName))
            {
                ValidationErrors.Add("芯片组名称不能为空");
            }

            // 如果组名变更，检查唯一性
            var newName = GroupName?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(newName) && 
                !string.Equals(newName, _originalGroupName, StringComparison.Ordinal))
            {
                try
                {
                    var allGroups = await _repository.GetAllChipGroupsAsync();
                    if (allGroups.Any(g => g.ChipGroupName.Equals(newName, StringComparison.OrdinalIgnoreCase) && g.Id != Id))
                    {
                        ValidationErrors.Add($"芯片组名称\"{newName}\"已存在");
                    }
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"校验芯片组名称唯一性失败: {ex.Message}", ex);
                    ValidationErrors.Add("芯片组名称校验失败，请稍后重试");
                }
            }

            if (SelectedColor == null)
            {
                ValidationErrors.Add("请选择一个颜色");
            }

            OnPropertyChanged(nameof(HasErrors));
            return ValidationErrors.Count == 0;
        }
    }
}

