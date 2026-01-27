using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Timer.Models;
using Timer.Services;

namespace Timer.ViewModels
{
    /// <summary>
    /// 编辑参赛人员对话框的ViewModel（仅用于编辑窗口）
    /// </summary>
    public class EditParticipantDialogViewModel : ObservableObject
    {
        private readonly IParticipantRepository _repository;
        private readonly ILoggingService? _loggingService;
        private readonly string? _originalExamNumber;
        private readonly string? _originalBibNumber;
        private readonly int? _projectId;

        private DateTime _date;
        private string? _school;
        private string? _grade;
        private string? _class;
        private string _name = string.Empty;
        private string _gender = string.Empty;
        private string? _examNumber;
        private string? _groupName;
        private string? _bibNumber;
        private string? _chipNumber;

        /// <summary>
        /// 初始化编辑对话框ViewModel
        /// </summary>
        public EditParticipantDialogViewModel(Participant participant, IParticipantRepository repository, ILoggingService? loggingService = null)
        {
            ArgumentNullException.ThrowIfNull(participant);
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _loggingService = loggingService;

            Id = participant.Id;
            SequenceNumber = participant.SequenceNumber;
            _projectId = participant.ProjectId;

            Date = participant.Date;
            School = participant.School;
            Grade = participant.Grade;
            Class = participant.Class;
            Name = participant.Name;
            Gender = participant.Gender;
            ExamNumber = participant.ExamNumber;
            GroupName = participant.GroupName;
            BibNumber = participant.BibNumber;
            ChipNumber = participant.ChipNumber;

            _originalExamNumber = participant.ExamNumber;
            _originalBibNumber = participant.BibNumber;

            GenderOptions = new ObservableCollection<string> { "男", "女" };
            ValidationErrors = new ObservableCollection<string>();
        }

        /// <summary>
        /// 人员Id
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// 序号（只读）
        /// </summary>
        public int SequenceNumber { get; }

        /// <summary>
        /// 性别选项
        /// </summary>
        public ObservableCollection<string> GenderOptions { get; }

        /// <summary>
        /// 校验错误列表
        /// </summary>
        public ObservableCollection<string> ValidationErrors { get; }

        /// <summary>
        /// 是否存在校验错误
        /// </summary>
        public bool HasErrors => ValidationErrors.Count > 0;

        /// <summary>
        /// 比赛日期
        /// </summary>
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        /// <summary>
        /// 学校
        /// </summary>
        public string? School
        {
            get => _school;
            set => SetProperty(ref _school, value);
        }

        /// <summary>
        /// 年级
        /// </summary>
        public string? Grade
        {
            get => _grade;
            set => SetProperty(ref _grade, value);
        }

        /// <summary>
        /// 班级
        /// </summary>
        public string? Class
        {
            get => _class;
            set => SetProperty(ref _class, value);
        }

        /// <summary>
        /// 姓名（必填）
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 性别（必填）
        /// </summary>
        public string Gender
        {
            get => _gender;
            set => SetProperty(ref _gender, value);
        }

        /// <summary>
        /// 准考证号（可选；若用户修改，则需要保持唯一）
        /// </summary>
        public string? ExamNumber
        {
            get => _examNumber;
            set => SetProperty(ref _examNumber, value);
        }

        /// <summary>
        /// 组别名称
        /// </summary>
        public string? GroupName
        {
            get => _groupName;
            set => SetProperty(ref _groupName, value);
        }

        /// <summary>
        /// 号码布（可选；若用户修改，则需要保持唯一）
        /// </summary>
        public string? BibNumber
        {
            get => _bibNumber;
            set => SetProperty(ref _bibNumber, value);
        }

        /// <summary>
        /// 芯片编号
        /// </summary>
        public string? ChipNumber
        {
            get => _chipNumber;
            set => SetProperty(ref _chipNumber, value);
        }

        /// <summary>
        /// 将当前编辑值组装为Participant对象（用于UpdateAsync）
        /// </summary>
        public Participant ToParticipant()
        {
            return new Participant
            {
                Id = Id,
                SequenceNumber = SequenceNumber,
                Date = Date,
                School = School,
                Grade = Grade,
                Class = Class,
                Name = Name?.Trim() ?? string.Empty,
                Gender = Gender?.Trim() ?? string.Empty,
                ExamNumber = string.IsNullOrWhiteSpace(ExamNumber) ? null : ExamNumber.Trim(),
                GroupName = string.IsNullOrWhiteSpace(GroupName) ? null : GroupName.Trim(),
                BibNumber = string.IsNullOrWhiteSpace(BibNumber) ? null : BibNumber.Trim(),
                ChipNumber = string.IsNullOrWhiteSpace(ChipNumber) ? null : ChipNumber.Trim()
            };
        }

        /// <summary>
        /// 执行校验（包括唯一性校验）
        /// </summary>
        public async Task<bool> ValidateAsync()
        {
            ValidationErrors.Clear();

            if (Date == default)
            {
                ValidationErrors.Add("比赛日期不能为空");
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                ValidationErrors.Add("姓名不能为空");
            }

            if (string.IsNullOrWhiteSpace(Gender))
            {
                ValidationErrors.Add("性别不能为空");
            }
            else if (Gender != "男" && Gender != "女")
            {
                ValidationErrors.Add("性别必须是\"男\"或\"女\"");
            }

            var exam = string.IsNullOrWhiteSpace(ExamNumber) ? null : ExamNumber.Trim();
            if (!string.IsNullOrWhiteSpace(exam) && !string.Equals(exam, _originalExamNumber, StringComparison.Ordinal))
            {
                try
                {
                    if (await _repository.ExistsByExamNumberAsync(exam, _projectId))
                    {
                        ValidationErrors.Add($"准考证号\"{exam}\"已存在");
                    }
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"校验准考证号唯一性失败: {ex.Message}", ex);
                    ValidationErrors.Add("准考证号校验失败，请稍后重试");
                }
            }

            var bib = string.IsNullOrWhiteSpace(BibNumber) ? null : BibNumber.Trim();
            if (!string.IsNullOrWhiteSpace(bib) && !string.Equals(bib, _originalBibNumber, StringComparison.Ordinal))
            {
                try
                {
                    if (await _repository.ExistsByBibNumberAsync(bib))
                    {
                        ValidationErrors.Add($"号码布\"{bib}\"已存在");
                    }
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"校验号码布唯一性失败: {ex.Message}", ex);
                    ValidationErrors.Add("号码布校验失败，请稍后重试");
                }
            }

            OnPropertyChanged(nameof(HasErrors));
            return ValidationErrors.Count == 0;
        }
    }
}


