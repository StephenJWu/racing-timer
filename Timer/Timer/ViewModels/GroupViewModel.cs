using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Win32;
using Timer.Data;
using Timer.Messages;
using Timer.Models;
using Timer.Services;

namespace Timer.ViewModels
{
    /// <summary>
    /// 人员分组页面的ViewModel
    /// </summary>
    public class GroupViewModel : ObservableObject, IDisposable, IRecipient<ChipGroupUpdatedMessage>, IRecipient<DataReloadRequestedMessage>
    {
        private const string AllOption = "全部";
        
        private readonly IParticipantRepository _participantRepository;
        private readonly IChipRepository _chipRepository;
        private readonly IRaceGroupRepository _raceGroupRepository;
        private readonly IParticipantGroupRepository _participantGroupRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IRaceGroupExportService _exportService;
        private readonly ILoggingService? _loggingService;
        private bool _disposed;

        // 查询条件
        private ObservableCollection<Project> _projects = new();
        private Project? _selectedProject;
        private string? _selectedSchool;
        private string? _selectedGrade;
        private string? _selectedClass;
        private string? _selectedGroup;
        private bool _isLoading;

        // 查询结果
        private RaceGroup? _selectedRaceGroup;

        /// <summary>
        /// 初始化GroupViewModel实例
        /// </summary>
        public GroupViewModel(
            IParticipantRepository participantRepository,
            IChipRepository chipRepository,
            IRaceGroupRepository raceGroupRepository,
            IParticipantGroupRepository participantGroupRepository,
            IProjectRepository projectRepository,
            IRaceGroupExportService exportService,
            ILoggingService? loggingService = null)
        {
            _participantRepository = participantRepository ?? throw new ArgumentNullException(nameof(participantRepository));
            _chipRepository = chipRepository ?? throw new ArgumentNullException(nameof(chipRepository));
            _raceGroupRepository = raceGroupRepository ?? throw new ArgumentNullException(nameof(raceGroupRepository));
            _participantGroupRepository = participantGroupRepository ?? throw new ArgumentNullException(nameof(participantGroupRepository));
            _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _loggingService = loggingService;

            Title = "人员分组";

            // 初始化集合
            Schools = new ObservableCollection<string> { AllOption };
            Grades = new ObservableCollection<string> { AllOption };
            Classes = new ObservableCollection<string> { AllOption };
            Groups = new ObservableCollection<string> { AllOption };
            RaceGroups = new ObservableCollection<RaceGroup>();
            Participants = new ObservableCollection<Participant>();
            ChipGroups = new ObservableCollection<ChipGroup>();
            LapOptions = new ObservableCollection<int>(Enumerable.Range(1, 20));

            // 初始化命令
            QueryCommand = new AsyncRelayCommand(QueryAsync);
            ExportCommand = new AsyncRelayCommand(ExportAsync);
            SchoolChangedCommand = new RelayCommand(OnSchoolChanged);
            GradeChangedCommand = new RelayCommand(OnGradeChanged);
            ClassChangedCommand = new RelayCommand(OnClassChanged);
            SelectRaceGroupCommand = new AsyncRelayCommand<RaceGroup>(SelectRaceGroupAsync);
            EditRaceGroupCommand = new AsyncRelayCommand<RaceGroup>(EditRaceGroupAsync);
            DeleteRaceGroupCommand = new AsyncRelayCommand<RaceGroup>(DeleteRaceGroupAsync);
            EditParticipantCommand = new AsyncRelayCommand<Participant>(EditParticipantAsync);
            DeleteParticipantCommand = new AsyncRelayCommand<Participant>(DeleteParticipantAsync);

            // 注册跨页面实时刷新（显式注册，避免多 IRecipient<> 时 Register(this) 歧义）
            WeakReferenceMessenger.Default.Register<ChipGroupUpdatedMessage>(this);
            WeakReferenceMessenger.Default.Register<DataReloadRequestedMessage>(this);

            // 加载初始数据
            _ = LoadInitialDataAsync();
        }

        public void Receive(ChipGroupUpdatedMessage message)
        {
            if (message?.Value == null) return;

            var updated = message.Value;

            // 1) 更新本页 ChipGroups 集合（用于下拉框等）
            var existingGroup = ChipGroups.FirstOrDefault(g => g.Id == updated.Id);
            if (existingGroup != null)
            {
                existingGroup.ChipGroupName = updated.ChipGroupName;
                existingGroup.Color = updated.Color;
                existingGroup.UpdatedAt = updated.UpdatedAt;
            }
            else
            {
                ChipGroups.Add(updated);
            }

            // 2) 刷新当前查询结果中的 RaceGroups 显示字段（颜色/名称）
            foreach (var rg in RaceGroups.Where(r => r.ChipGroupId == updated.Id))
            {
                rg.ChipGroupName = updated.ChipGroupName;
                rg.ChipGroupColor = updated.Color;
            }

            // 3) 如果当前选中分组也引用该芯片组，确保详情区也刷新（RaceGroup 已可通知）
            if (SelectedRaceGroup?.ChipGroupId == updated.Id)
            {
                SelectedRaceGroup.ChipGroupName = updated.ChipGroupName;
                SelectedRaceGroup.ChipGroupColor = updated.Color;
            }
        }

        public void Receive(DataReloadRequestedMessage message)
        {
            if (message == null) return;

            switch (message.Value)
            {
                case DataDomain.ChipGroups:
                    _ = ReloadChipGroupsAsync();
                    break;
                case DataDomain.RaceGroups:
                    // 仅在已选择学校时自动重查，避免弹“请选择学校”
                    if (!string.IsNullOrWhiteSpace(SelectedSchool) && SelectedSchool != AllOption)
                    {
                        _ = QueryAsync();
                    }
                    break;
                case DataDomain.Participants:
                    if (SelectedRaceGroup != null)
                    {
                        _ = SelectRaceGroupAsync(SelectedRaceGroup);
                    }
                    break;
            }
        }

        /// <summary>
        /// 页面标题
        /// </summary>
        public string Title { get; }

        // 项目列表属性
        public ObservableCollection<Project> Projects
        {
            get => _projects;
            set => SetProperty(ref _projects, value);
        }

        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (SetProperty(ref _selectedProject, value))
                {
                    // 重置下级下拉框
                    _selectedSchool = AllOption;
                    _selectedGrade = AllOption;
                    _selectedClass = AllOption;
                    _selectedGroup = AllOption;
                    
                    Schools.Clear();
                    Schools.Add(AllOption);
                    Grades.Clear();
                    Grades.Add(AllOption);
                    Classes.Clear();
                    Classes.Add(AllOption);
                    Groups.Clear();
                    Groups.Add(AllOption);
                    
                    OnPropertyChanged(nameof(SelectedSchool));
                    OnPropertyChanged(nameof(SelectedGrade));
                    OnPropertyChanged(nameof(SelectedClass));
                    OnPropertyChanged(nameof(SelectedGroup));
                    
                    // 加载该项目下的学校列表（如果选择的是"全部"，则加载所有学校的列表）
                    _ = LoadSchoolsByProjectAsync(value?.Id ?? 0);
                }
            }
        }

        public ObservableCollection<string> Schools { get; }

        public string? SelectedSchool
        {
            get => _selectedSchool;
            set
            {
                if (SetProperty(ref _selectedSchool, value))
                {
                    SchoolChangedCommand.Execute(null);
                }
            }
        }

        public ObservableCollection<string> Grades { get; }

        public string? SelectedGrade
        {
            get => _selectedGrade;
            set
            {
                if (SetProperty(ref _selectedGrade, value))
                {
                    GradeChangedCommand.Execute(null);
                }
            }
        }

        public ObservableCollection<string> Classes { get; }

        public string? SelectedClass
        {
            get => _selectedClass;
            set
            {
                if (SetProperty(ref _selectedClass, value))
                {
                    ClassChangedCommand.Execute(null);
                }
            }
        }

        public ObservableCollection<string> Groups { get; }

        public string? SelectedGroup
        {
            get => _selectedGroup;
            set => SetProperty(ref _selectedGroup, value);
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // 查询结果
        public ObservableCollection<RaceGroup> RaceGroups { get; }

        public RaceGroup? SelectedRaceGroup
        {
            get => _selectedRaceGroup;
            set
            {
                if (SetProperty(ref _selectedRaceGroup, value))
                {
                    // 当选中项变化时，自动加载分组详情
                    if (value != null)
                    {
                        _ = SelectRaceGroupAsync(value);
                    }
                    else
                    {
                        Participants.Clear();
                    }
                }
            }
        }

        public ObservableCollection<Participant> Participants { get; }

        // 芯片组数据
        public ObservableCollection<ChipGroup> ChipGroups { get; }
        public ObservableCollection<int> LapOptions { get; }

        // 命令
        public IAsyncRelayCommand QueryCommand { get; }
        public IAsyncRelayCommand ExportCommand { get; }
        public IRelayCommand SchoolChangedCommand { get; }
        public IRelayCommand GradeChangedCommand { get; }
        public IRelayCommand ClassChangedCommand { get; }
        public IAsyncRelayCommand<RaceGroup> SelectRaceGroupCommand { get; }
        public IAsyncRelayCommand<RaceGroup> EditRaceGroupCommand { get; }
        public IAsyncRelayCommand<RaceGroup> DeleteRaceGroupCommand { get; }
        public IAsyncRelayCommand<Participant> EditParticipantCommand { get; }
        public IAsyncRelayCommand<Participant> DeleteParticipantCommand { get; }

        /// <summary>
        /// 加载初始数据
        /// </summary>
        private async Task LoadInitialDataAsync()
        {
            try
            {
                // 加载项目列表
                await LoadProjectsAsync();

                // 加载芯片组列表
                var chipGroups = await _chipRepository.GetAllChipGroupsAsync();
                ChipGroups.Clear();
                foreach (var chipGroup in chipGroups)
                {
                    ChipGroups.Add(chipGroup);
                }

                // 初始化学校下拉框，默认选择"全部"
                await LoadSchoolsByProjectAsync(0); // 0 表示全部项目

                // 初始化年级、班级、组别下拉框，默认选择"全部"
                await LoadGradesAsync(null);
                await LoadClassesAsync(null, null);
                await LoadGroupsAsync(null, null, null);

                // 确保所有下拉框默认选择"全部"
                _selectedSchool = AllOption;
                _selectedGrade = AllOption;
                _selectedClass = AllOption;
                _selectedGroup = AllOption;
                OnPropertyChanged(nameof(SelectedSchool));
                OnPropertyChanged(nameof(SelectedGrade));
                OnPropertyChanged(nameof(SelectedClass));
                OnPropertyChanged(nameof(SelectedGroup));

                // 确保项目选择为"全部"（如果还没有设置）
                if (_selectedProject == null)
                {
                    _selectedProject = Projects.FirstOrDefault(p => p.Id == 0);
                    OnPropertyChanged(nameof(SelectedProject));
                }

                // 默认查询所有分组信息（项目=全部，即 projectId = null）
                await QueryAsync();
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载初始数据失败: {ex.Message}", ex);
                MessageBox.Show($"加载初始数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 加载项目列表
        /// </summary>
        private async Task LoadProjectsAsync()
        {
            try
            {
                var projects = await _projectRepository.GetActiveProjectsAsync();
                var projectList = new ObservableCollection<Project>();
                
                // 添加"全部"选项作为第一项（Id=0 表示全部）
                projectList.Add(new Project { Id = 0, Name = AllOption });
                
                foreach (var project in projects)
                {
                    projectList.Add(project);
                }
                
                Projects = projectList;
                
                // 默认选择"全部"
                _selectedProject = projectList.FirstOrDefault(p => p.Id == 0);
                OnPropertyChanged(nameof(SelectedProject));
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载项目列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据项目ID加载学校列表
        /// </summary>
        private async Task LoadSchoolsByProjectAsync(int projectId)
        {
            try
            {
                int? projectIdNullable = projectId > 0 ? projectId : null;
                var schools = await _participantGroupRepository.GetDistinctSchoolsByProjectAsync(projectIdNullable);
                Schools.Clear();
                Schools.Add(AllOption);
                foreach (var school in schools)
                {
                    Schools.Add(school);
                }
                _selectedSchool = AllOption;
                OnPropertyChanged(nameof(SelectedSchool));
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载学校列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载年级列表
        /// </summary>
        private async Task LoadGradesAsync(string? school)
        {
            try
            {
                var grades = await _participantGroupRepository.GetDistinctGradesAsync(school);
                Grades.Clear();
                Grades.Add(AllOption);
                foreach (var grade in grades)
                {
                    Grades.Add(grade);
                }
                // 默认选择"全部"
                _selectedGrade = AllOption;
                OnPropertyChanged(nameof(SelectedGrade));

                // 加载班级（因为年级默认是"全部"）
                await LoadClassesAsync(school, null);
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载年级失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载班级列表
        /// </summary>
        private async Task LoadClassesAsync(string? school, string? grade)
        {
            try
            {
                var classes = await _participantRepository.GetDistinctClassesAsync(school, grade);
                Classes.Clear();
                Classes.Add(AllOption);
                foreach (var cls in classes)
                {
                    Classes.Add(cls);
                }
                // 默认选择"全部"
                _selectedClass = AllOption;
                OnPropertyChanged(nameof(SelectedClass));

                // 加载组别（因为班级默认是"全部"）
                await LoadGroupsAsync(school, grade, null);
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载班级失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载组别列表
        /// </summary>
        private async Task LoadGroupsAsync(string? school, string? grade, string? classValue)
        {
            try
            {
                var groups = await _participantGroupRepository.GetDistinctGroupNamesAsync(school, grade, classValue);
                Groups.Clear();
                Groups.Add(AllOption);
                foreach (var group in groups)
                {
                    Groups.Add(group);
                }
                // 默认选择"全部"
                _selectedGroup = AllOption;
                OnPropertyChanged(nameof(SelectedGroup));
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载组别失败: {ex.Message}", ex);
            }
        }

        private async Task ReloadChipGroupsAsync()
        {
            try
            {
                var chipGroups = await _chipRepository.GetAllChipGroupsAsync();
                ChipGroups.Clear();
                foreach (var chipGroup in chipGroups)
                {
                    ChipGroups.Add(chipGroup);
                }
            }
            catch
            {
                // 静默：实时刷新不应打扰用户
            }
        }

        /// <summary>
        /// 学校改变时加载年级
        /// </summary>
        private async void OnSchoolChanged()
        {
            Grades.Clear();
            Classes.Clear();
            Groups.Clear();

            var school = (SelectedSchool == AllOption) ? null : SelectedSchool;
            await LoadGradesAsync(school);
        }

        /// <summary>
        /// 年级改变时加载班级
        /// </summary>
        private async void OnGradeChanged()
        {
            Classes.Clear();
            Groups.Clear();

            var school = (SelectedSchool == AllOption) ? null : SelectedSchool;
            var grade = (SelectedGrade == AllOption) ? null : SelectedGrade;
            await LoadClassesAsync(school, grade);
        }

        /// <summary>
        /// 班级改变时加载组别
        /// </summary>
        private async void OnClassChanged()
        {
            Groups.Clear();

            var school = (SelectedSchool == AllOption) ? null : SelectedSchool;
            var grade = (SelectedGrade == AllOption) ? null : SelectedGrade;
            var classValue = (SelectedClass == AllOption) ? null : SelectedClass;
            await LoadGroupsAsync(school, grade, classValue);
        }

        /// <summary>
        /// 查询分组
        /// </summary>
        private async Task QueryAsync()
        {
            _loggingService?.Info("[按钮点击] 人员分组 - 查询按钮");
            var projectId = (SelectedProject == null || SelectedProject.Id == 0) ? (int?)null : SelectedProject.Id;
            var projectDisplay = (SelectedProject == null || SelectedProject.Id == 0) ? "全部" : SelectedProject.Name;
            _loggingService?.Debug($"[查询条件] 项目={projectDisplay}, 学校={SelectedSchool ?? AllOption}, 年级={SelectedGrade ?? AllOption}, 班级={SelectedClass ?? AllOption}, 组别={SelectedGroup ?? AllOption}");
            try
            {
                IsLoading = true;
                // 让UI有机会刷新显示遮罩层
                await Task.Delay(50);
                
                var school = (SelectedSchool == AllOption) ? null : SelectedSchool;
                var grade = (SelectedGrade == AllOption) ? null : SelectedGrade;
                var classValue = (SelectedClass == AllOption) ? null : SelectedClass;
                var groupName = (SelectedGroup == AllOption) ? null : SelectedGroup;

                // 直接查询ParticipantGroups表
                var participantGroups = await _participantGroupRepository.QueryAsync(
                    projectId,
                    school,
                    grade,
                    classValue,
                    groupName);

                RaceGroups.Clear();
                foreach (var participantGroup in participantGroups)
                {
                    // 将ParticipantGroup转换为RaceGroup用于显示
                    var raceGroup = new RaceGroup
                    {
                        Id = participantGroup.Id, // 使用ParticipantGroup的ID
                        ProjectId = participantGroup.ProjectId,
                        School = participantGroup.School,
                        Grade = participantGroup.Grade,
                        Class = participantGroup.Class,
                        GroupName = participantGroup.GroupName,
                        RaceLaps = participantGroup.RaceLaps,
                        ChipGroupId = participantGroup.ChipGroupId,
                        ChipGroupName = participantGroup.ChipGroupName,
                        Status = RaceStatus.Pending, // 默认状态
                        CreatedAt = participantGroup.CreatedAt,
                        UpdatedAt = participantGroup.UpdatedAt
                    };

                    // 获取该分组的人员数量
                    try
                    {
                        var participants = await _raceGroupRepository.GetParticipantsByGroupAsync(
                            participantGroup.ProjectId,
                            participantGroup.School,
                            participantGroup.Grade,
                            participantGroup.Class,
                            participantGroup.GroupName);
                        raceGroup.ParticipantCount = participants.Count();
                    }
                    catch
                    {
                        raceGroup.ParticipantCount = 0;
                    }

                    // 如果芯片组ID不为空，获取芯片组颜色
                    if (participantGroup.ChipGroupId.HasValue)
                    {
                        try
                        {
                            var chipGroup = await _chipRepository.GetChipGroupByIdAsync(participantGroup.ChipGroupId.Value);
                            if (chipGroup != null)
                            {
                                raceGroup.ChipGroupColor = chipGroup.Color;
                            }
                        }
                        catch
                        {
                            // 忽略错误
                        }
                    }

                    RaceGroups.Add(raceGroup);
                }

                _loggingService?.Info($"[查询结果] 查询到 {RaceGroups.Count} 个分组");
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"查询分组失败: {ex.Message}", ex);
                MessageBox.Show($"查询失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 选择分组卡片
        /// </summary>
        private async Task SelectRaceGroupAsync(RaceGroup? raceGroup)
        {
            if (raceGroup == null)
            {
                return;
            }

            try
            {
                // 加载分组内的参赛人员
                var participants = await _raceGroupRepository.GetParticipantsByGroupAsync(
                    raceGroup.ProjectId,
                    raceGroup.School,
                    raceGroup.Grade,
                    raceGroup.Class,
                    raceGroup.GroupName);

                Participants.Clear();
                foreach (var participant in participants)
                {
                    Participants.Add(participant);
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载参赛人员失败: {ex.Message}", ex);
                MessageBox.Show($"加载参赛人员失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 编辑分组（打开对话框）
        /// </summary>
        private async Task EditRaceGroupAsync(RaceGroup? raceGroup)
        {
            if (raceGroup == null)
            {
                return;
            }

            try
            {
                var dialogViewModel = new EditRaceGroupDialogViewModel(raceGroup, ChipGroups);
                var dialog = new Timer.Views.EditRaceGroupDialog
                {
                    DataContext = dialogViewModel,
                    Owner = Application.Current.MainWindow
                };

                if (dialog.ShowDialog() == true)
                {
                    // 获取或创建ParticipantGroup配置
                    var participantGroup = await _participantGroupRepository.GetOrCreateAsync(
                        raceGroup.ProjectId,
                        raceGroup.School,
                        raceGroup.Grade,
                        raceGroup.Class,
                        raceGroup.GroupName);

                    // 更新ParticipantGroups表中的配置（芯片组 + 圈数）
                    participantGroup.RaceLaps = raceGroup.RaceLaps;
                    participantGroup.ChipGroupId = raceGroup.ChipGroupId;
                    
                    // 获取芯片组名称
                    if (raceGroup.ChipGroupId.HasValue)
                    {
                        var chipGroup = await _chipRepository.GetChipGroupByIdAsync(raceGroup.ChipGroupId.Value);
                        if (chipGroup != null)
                        {
                            participantGroup.ChipGroupName = chipGroup.ChipGroupName;
                        }
                    }
                    else
                    {
                        participantGroup.ChipGroupName = null;
                    }

                    participantGroup.UpdatedAt = DateTime.Now;
                    await _participantGroupRepository.UpdateAsync(participantGroup);

                    // 为分组内人员分配芯片
                    // 先查找或创建对应的RaceGroup（用于芯片分配）
                    var existingRaceGroups = await _raceGroupRepository.QueryRaceGroupsAsync(
                        raceGroup.ProjectId,
                        raceGroup.School,
                        raceGroup.Grade,
                        raceGroup.Class,
                        raceGroup.GroupName);
                    var existingRaceGroup = existingRaceGroups.FirstOrDefault();
                    
                    int assignedCount = 0;
                    if (raceGroup.ChipGroupId.HasValue)
                    {
                        if (existingRaceGroup != null)
                        {
                            // 使用现有的RaceGroup进行芯片分配
                            assignedCount = await _raceGroupRepository.AssignChipsToParticipantsAsync(
                                existingRaceGroup.Id,
                                raceGroup.ChipGroupId.Value);
                        }
                        else
                        {
                            // 如果没有RaceGroup，直接通过分组信息分配芯片
                            // 获取分组内的所有参赛人员
                            var participants = await _raceGroupRepository.GetParticipantsByGroupAsync(
                                raceGroup.ProjectId,
                                raceGroup.School,
                                raceGroup.Grade,
                                raceGroup.Class,
                                raceGroup.GroupName);
                            
                            if (participants.Any())
                            {
                                // 获取芯片组的所有芯片
                                var chips = await _chipRepository.GetChipsByGroupIdAsync(raceGroup.ChipGroupId.Value);
                                var chipList = chips.OrderBy(c => int.TryParse(c.LabelNumber, out var num) ? num : int.MaxValue).ToList();
                                
                                if (chipList.Count >= participants.Count())
                                {
                                    // 为每个参赛人员分配芯片
                                    var participantList = participants.OrderBy(p => p.SequenceNumber).ToList();
                                    for (int i = 0; i < participantList.Count && i < chipList.Count; i++)
                                    {
                                        var participant = participantList[i];
                                        var chip = chipList[i];
                                        participant.LabelNumber = chip.LabelNumber;
                                        participant.InternalNumber = chip.InternalNumber;
                                        participant.UpdatedAt = DateTime.Now;
                                        await _participantRepository.UpdateAsync(participant);
                                    }
                                    assignedCount = Math.Min(participantList.Count, chipList.Count);
                                }
                                else
                                {
                                    throw new InvalidOperationException(
                                        $"芯片数量不足：需要 {participants.Count()} 个芯片，但芯片组中只有 {chipList.Count} 个芯片");
                                }
                            }
                        }
                    }

                    _loggingService?.Info($"成功为 {assignedCount} 名参赛人员分配芯片");

                    // 更新分组的芯片组信息显示
                    var chipGroupForDisplay = ChipGroups.FirstOrDefault(cg => cg.Id == raceGroup.ChipGroupId.Value);
                    if (chipGroupForDisplay != null)
                    {
                        raceGroup.ChipGroupName = chipGroupForDisplay.ChipGroupName;
                        raceGroup.ChipGroupColor = chipGroupForDisplay.Color;
                    }

                    // 刷新人员详情（如果当前选中的是该分组）
                    if (SelectedRaceGroup?.Id == raceGroup.Id)
                    {
                        await SelectRaceGroupAsync(raceGroup);
                    }

                    // 通知其它页面：分组/人员（芯片分配）已批量变更
                    WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.RaceGroups));
                    WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.Participants));

                    MessageBox.Show($"编辑成功！已为 {assignedCount} 名参赛人员分配芯片。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (InvalidOperationException ex)
            {
                _loggingService?.Warn($"编辑分组失败: {ex.Message}");
                MessageBox.Show(ex.Message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"编辑分组失败: {ex.Message}", ex);
                MessageBox.Show($"编辑分组失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除分组
        /// </summary>
        private async Task DeleteRaceGroupAsync(RaceGroup? raceGroup)
        {
            if (raceGroup == null)
            {
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除分组 {raceGroup.DisplayName} 吗？\n注意：这不会删除参赛人员，只会删除分组配置。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // 删除ParticipantGroup配置
                    await _participantGroupRepository.DeleteAsync(raceGroup.Id);
                    
                    RaceGroups.Remove(raceGroup);

                    if (SelectedRaceGroup?.Id == raceGroup.Id)
                    {
                        SelectedRaceGroup = null;
                        Participants.Clear();
                    }

                    _loggingService?.Info($"删除分组 {raceGroup.DisplayName}");
                    MessageBox.Show("删除成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"删除分组失败: {ex.Message}", ex);
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 编辑参赛人员（编辑芯片分配）
        /// </summary>
        private async Task EditParticipantAsync(Participant? participant)
        {
            if (participant == null || SelectedRaceGroup == null)
            {
                return;
            }

            try
            {
                // 获取当前分组的芯片组芯片列表
                var availableChips = new ObservableCollection<Chip>();
                if (SelectedRaceGroup.ChipGroupId.HasValue)
                {
                    var chips = await _chipRepository.GetChipsByGroupIdAsync(SelectedRaceGroup.ChipGroupId.Value);
                    foreach (var chip in chips)
                    {
                        availableChips.Add(chip);
                    }
                }

                var dialogViewModel = new EditGroupMemberDialogViewModel(participant, availableChips);
                var dialog = new Timer.Views.EditGroupMemberDialog
                {
                    DataContext = dialogViewModel,
                    Owner = Application.Current.MainWindow
                };

                if (dialog.ShowDialog() == true)
                {
                    // 更新数据库
                    var updatedParticipant = dialogViewModel.GetUpdatedParticipant();
                    await _participantRepository.UpdateAsync(updatedParticipant);

                    // 刷新当前分组的人员列表
                    await SelectRaceGroupAsync(SelectedRaceGroup);
                    
                    // 通知其它页面：人员与分组统计可能变化
                    WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.Participants));
                    WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.RaceGroups));
                    
                    MessageBox.Show("编辑成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"编辑参赛人员失败: {ex.Message}", ex);
                MessageBox.Show($"编辑失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除参赛人员
        /// </summary>
        private async Task DeleteParticipantAsync(Participant? participant)
        {
            if (participant == null)
            {
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除参赛人员 {participant.Name} 吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _participantRepository.DeleteAsync(participant.Id);
                    Participants.Remove(participant);
                    
                    WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.Participants));
                    WeakReferenceMessenger.Default.Send(new DataReloadRequestedMessage(DataDomain.RaceGroups));
                    
                    _loggingService?.Info($"删除参赛人员 {participant.Name}");
                    MessageBox.Show("删除成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _loggingService?.Error($"删除参赛人员失败: {ex.Message}", ex);
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 导出Excel
        /// </summary>
        private async Task ExportAsync()
        {
            _loggingService?.Info("[按钮点击] 人员分组 - 导出按钮");
            
            if (RaceGroups.Count == 0)
            {
                _loggingService?.Warn("[导出] 没有可导出的数据");
                MessageBox.Show("没有可导出的数据，请先查询分组", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel文件 (*.xlsx)|*.xlsx",
                    FileName = $"分组结果_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    _loggingService?.Info($"[导出] 开始导出分组到: {saveFileDialog.FileName}");
                    IsLoading = true;
                    
                    try
                    {
                        // 加载所有分组的参赛人员
                        var participantsDict = new Dictionary<int, IEnumerable<Participant>>();
                        foreach (var raceGroup in RaceGroups)
                        {
                            var participants = await _raceGroupRepository.GetParticipantsByGroupAsync(
                                raceGroup.ProjectId,
                                raceGroup.School,
                                raceGroup.Grade,
                                raceGroup.Class,
                                raceGroup.GroupName);
                            participantsDict[raceGroup.Id] = participants;
                        }

                        await _exportService.ExportToExcelAsync(RaceGroups, participantsDict, saveFileDialog.FileName);

                        _loggingService?.Info($"[导出] 成功导出 {RaceGroups.Count} 个分组到 {saveFileDialog.FileName}");
                        MessageBox.Show($"导出成功！\n文件位置：{saveFileDialog.FileName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.Error($"[导出异常] 导出Excel失败: {ex.Message}", ex);
                        MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"[导出异常] 打开保存对话框失败: {ex.Message}", ex);
                MessageBox.Show($"发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的实现
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                WeakReferenceMessenger.Default.UnregisterAll(this);
                // 清理托管资源
                _disposed = true;
            }
        }
    }
}
