using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Timer.Models;
using Timer.Services;

namespace Timer.ViewModels
{
    /// <summary>
    /// 编辑芯片对话框的ViewModel
    /// </summary>
    public class EditChipDialogViewModel : ObservableObject
    {
        private readonly IChipRepository _repository;
        private readonly ILoggingService? _loggingService;
        private readonly string _originalLabelNumber;

        private string _labelNumber = string.Empty;
        private string _internalNumber = string.Empty;

        /// <summary>
        /// 初始化编辑芯片对话框ViewModel
        /// </summary>
        public EditChipDialogViewModel(Chip chip, IChipRepository repository, ILoggingService? loggingService = null)
        {
            ArgumentNullException.ThrowIfNull(chip);
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _loggingService = loggingService;

            Id = chip.Id;
            ChipGroupId = chip.ChipGroupId;
            _originalLabelNumber = chip.LabelNumber;
            LabelNumber = chip.LabelNumber;
            InternalNumber = chip.InternalNumber;

            ValidationErrors = new ObservableCollection<string>();
        }

        /// <summary>
        /// 芯片Id
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// 所属芯片组Id
        /// </summary>
        public int ChipGroupId { get; }

        /// <summary>
        /// 校验错误列表
        /// </summary>
        public ObservableCollection<string> ValidationErrors { get; }

        /// <summary>
        /// 是否存在校验错误
        /// </summary>
        public bool HasErrors => ValidationErrors.Count > 0;

        /// <summary>
        /// 芯片标签号码
        /// </summary>
        public string LabelNumber
        {
            get => _labelNumber;
            set => SetProperty(ref _labelNumber, value);
        }

        /// <summary>
        /// 芯片内部编号
        /// </summary>
        public string InternalNumber
        {
            get => _internalNumber;
            set => SetProperty(ref _internalNumber, value);
        }

        /// <summary>
        /// 将当前编辑值组装为Chip对象
        /// </summary>
        public Chip ToChip()
        {
            return new Chip
            {
                Id = Id,
                ChipGroupId = ChipGroupId,
                LabelNumber = LabelNumber?.Trim() ?? string.Empty,
                InternalNumber = InternalNumber?.Trim() ?? string.Empty,
                UpdatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 执行校验（包括标签号码唯一性校验）
        /// </summary>
        public async Task<bool> ValidateAsync()
        {
            ValidationErrors.Clear();

            if (string.IsNullOrWhiteSpace(LabelNumber))
            {
                ValidationErrors.Add("芯片标签号码不能为空");
            }

            if (string.IsNullOrWhiteSpace(InternalNumber))
            {
                ValidationErrors.Add("芯片内部编号不能为空");
            }

            // 如果标签号码变更，检查唯一性
            var newLabel = LabelNumber?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(newLabel) && 
                !string.Equals(newLabel, _originalLabelNumber, StringComparison.Ordinal))
            {
                try
                {
                    if (await _repository.ExistsByLabelNumberAsync(newLabel))
                    {
                        ValidationErrors.Add($"芯片标签号码\"{newLabel}\"已存在");
                    }
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"校验芯片标签号码唯一性失败: {ex.Message}", ex);
                    ValidationErrors.Add("芯片标签号码校验失败，请稍后重试");
                }
            }

            OnPropertyChanged(nameof(HasErrors));
            return ValidationErrors.Count == 0;
        }
    }
}

