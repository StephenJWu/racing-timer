using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Timer.Models;
using Timer.Messages;
using Timer.Services;

namespace Timer.ViewModels
{
    /// <summary>
    /// 多组比赛计时页面的ViewModel，支持多个比赛组同时进行
    /// </summary>
    public partial class MultiRaceTimerViewModel : ObservableObject, IDisposable, IRecipient<ChipGroupUpdatedMessage>, IRecipient<DataReloadRequestedMessage>
    {
        private readonly IRaceGroupRepository _raceGroupRepository;
        private readonly IRaceRecordRepository _raceRecordRepository;
        private readonly IParticipantRepository _participantRepository;
        private readonly IParticipantGroupRepository _participantGroupRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IChipRepository _chipRepository;
        private readonly ITimerService _timerService;
        private readonly ILoggingService? _loggingService;
        private bool _disposed;

        /// <summary>
        /// 可用的项目列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<Project> _availableProjects = new();

        /// <summary>
        /// 当前选中的项目
        /// </summary>
        [ObservableProperty]
        private Project? _selectedProject;

        /// <summary>
        /// 可用的比赛分组列表（用于选择添加）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<RaceGroup> _availableRaceGroups = new();

        /// <summary>
        /// 当前选中的可用分组
        /// </summary>
        [ObservableProperty]
        private RaceGroup? _selectedAvailableGroup;

        /// <summary>
        /// 已添加到比赛的分组列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<RaceGroupTimingInfo> _raceGroups = new();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isSelectAllChecked;

        [ObservableProperty]
        private string _quickLapInput = string.Empty;

        /// <summary>
        /// 选中的比赛组数量
        /// </summary>
        public int SelectedCount => RaceGroups.Count(g => g.IsSelected);

        /// <summary>
        /// 是否有选中的比赛组
        /// </summary>
        public bool HasSelectedGroups => SelectedCount > 0;

        /// <summary>
        /// 是否有正在进行的比赛
        /// </summary>
        public bool HasActiveRaces => RaceGroups.Any(g => g.IsRaceActive);

        /// <summary>
        /// 是否有已添加的比赛组
        /// </summary>
        public bool HasRaceGroups => RaceGroups.Count > 0;

        public string Title => "比赛计时";

        public MultiRaceTimerViewModel(
            IRaceGroupRepository raceGroupRepository,
            IRaceRecordRepository raceRecordRepository,
            IParticipantRepository participantRepository,
            IParticipantGroupRepository participantGroupRepository,
            IProjectRepository projectRepository,
            IChipRepository chipRepository,
            ITimerService timerService,
            ILoggingService? loggingService = null)
        {
            _raceGroupRepository = raceGroupRepository ?? throw new ArgumentNullException(nameof(raceGroupRepository));
            _raceRecordRepository = raceRecordRepository ?? throw new ArgumentNullException(nameof(raceRecordRepository));
            _participantRepository = participantRepository ?? throw new ArgumentNullException(nameof(participantRepository));
            _participantGroupRepository = participantGroupRepository ?? throw new ArgumentNullException(nameof(participantGroupRepository));
            _projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
            _chipRepository = chipRepository ?? throw new ArgumentNullException(nameof(chipRepository));
            _timerService = timerService ?? throw new ArgumentNullException(nameof(timerService));
            _loggingService = loggingService;

            _loggingService?.Debug("MultiRaceTimerViewModel 初始化");

            // 加载可用分组 + 注册跨页面实时刷新（显式注册，避免多 IRecipient<> 时 Register(this) 歧义）
            WeakReferenceMessenger.Default.Register<ChipGroupUpdatedMessage>(this);
            WeakReferenceMessenger.Default.Register<DataReloadRequestedMessage>(this);
            _ = InitializeAsync();
        }

        public void Receive(ChipGroupUpdatedMessage message)
        {
            if (message?.Value == null) return;
            var updated = message.Value;

            // 可用分组列表（RaceGroup）
            foreach (var g in AvailableRaceGroups.Where(r => r.ChipGroupId == updated.Id))
            {
                g.ChipGroupName = updated.ChipGroupName;
                g.ChipGroupColor = updated.Color;
            }

            // 已添加到比赛的分组（RaceGroupTimingInfo）
            foreach (var g in RaceGroups.Where(r => r.RaceGroupId > 0 && r.ChipGroupName != null))
            {
                // RaceGroupTimingInfo 没有 ChipGroupId，按名称/颜色不可靠；改为通过 AvailableRaceGroups 的映射更稳
                // 如果未来需要更强一致性，建议在 TimingInfo 增加 ChipGroupId。
            }

            // 通过 RaceGroupId 反查当前 TimingInfo 对应的 RaceGroup，再更新 TimingInfo 的颜色/名称
            foreach (var timing in RaceGroups)
            {
                var rg = AvailableRaceGroups.FirstOrDefault(x => x.Id == timing.RaceGroupId);
                if (rg != null && rg.ChipGroupId == updated.Id)
                {
                    timing.ChipGroupName = updated.ChipGroupName;
                    timing.ChipGroupColor = updated.Color;
                }
            }
        }

        public void Receive(DataReloadRequestedMessage message)
        {
            if (message == null) return;

            if (message.Value == DataDomain.RaceGroups)
            {
                _ = LoadAvailableGroupsAsync();
            }
            else if (message.Value == DataDomain.Participants)
            {
                _ = RefreshParticipantsForActiveGroupsAsync();
            }
        }

        private async Task RefreshParticipantsForActiveGroupsAsync()
        {
            // 逐个分组刷新参赛者“身份信息”（号码布/姓名/芯片号）
            foreach (var group in RaceGroups.ToList())
            {
                // 比赛未开始：可直接全量重载
                if (!group.IsRaceActive && group.Status == RaceStatus.Stopped)
                {
                    await LoadParticipantsForGroupAsync(group);
                    continue;
                }

                // 比赛进行中/已暂停/已完成：仅更新显示字段，避免重置圈次与计时
                await RefreshParticipantIdentityOnlyAsync(group);
            }
        }

        private async Task RefreshParticipantIdentityOnlyAsync(RaceGroupTimingInfo group)
        {
            try
            {
                var searchFilter = new SearchFilter
                {
                    School = group.School,
                    Grade = group.Grade,
                    Class = group.Class,
                    GroupName = group.GroupName,
                    PageNumber = 1,
                    PageSize = 1000
                };

                var participants = (await _participantRepository.GetAllAsync(searchFilter)).ToList();
                var map = participants.ToDictionary(p => p.Id, p => p);

                foreach (var p in group.Participants)
                {
                    if (map.TryGetValue(p.ParticipantId, out var latest))
                    {
                        p.LabelNumber = latest.LabelNumber ?? "-";
                        p.Name = latest.Name;
                        p.InternalNumber = latest.InternalNumber;
                    }
                }

                // 参赛人数变化：比赛进行中不强行增删，避免影响计时；仅更新显示统计
                group.ParticipantCount = group.Participants.Count;
            }
            catch
            {
                // 静默：实时刷新失败不应打扰用户
            }
        }

        /// <summary>
        /// 加载可用的比赛分组（根据当前选中的项目）
        /// </summary>
        [RelayCommand]
        private async Task LoadAvailableGroupsAsync()
        {
            try
            {
                IsLoading = true;
                
                await LoadRaceGroupsByProjectAsync(SelectedProject?.Id ?? 0);

                // 同时恢复活跃的比赛
                await RestoreActiveRacesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载比赛分组失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 恢复活跃的比赛（从 RaceGroups 表加载状态不是 Pending 的比赛组）
        /// </summary>
        private async Task RestoreActiveRacesAsync()
        {
            try
            {
                // 从数据库加载所有状态不是 Pending 的比赛组
                var allRaceGroups = await _raceGroupRepository.GetAllAsync();
                var activeRaceGroups = allRaceGroups.Where(rg => rg.Status != RaceStatus.Pending).ToList();

                foreach (var raceGroup in activeRaceGroups)
                {
                    // 如果已经在列表中，跳过
                    if (RaceGroups.Any(r => r.RaceGroupId == raceGroup.Id))
                        continue;

                    var timingInfo = CreateTimingInfoFromGroup(raceGroup, raceGroup.RaceLaps);
                    timingInfo.RaceGroupId = raceGroup.Id;
                    timingInfo.Status = raceGroup.Status;

                    // 从 RaceRecords 加载参赛者
                    await LoadParticipantsFromRaceRecordsAsync(timingInfo, raceGroup.Id);

                    // 恢复计时状态（根据 RaceRecords 中的状态和时间）
                    var raceRecords = await _raceRecordRepository.GetByRaceGroupIdAsync(raceGroup.Id);
                    if (raceRecords.Any())
                    {
                        // 检查是否有进行中的记录
                        var runningRecords = raceRecords.Where(r => r.Status == RaceStatus.Running || r.Status == RaceStatus.Paused).ToList();
                        if (runningRecords.Any())
                        {
                            // 恢复计时器（使用当前时间减去已用时间）
                            // 这里简化处理，实际应该从数据库恢复开始时间
                            timingInfo.StartTimer(DateTime.Now);
                            if (raceGroup.Status == RaceStatus.Paused)
                            {
                                timingInfo.PauseTimer();
                            }
                        }
                    }

                    RaceGroups.Add(timingInfo);
                    
                    // 从可用列表中移除（如果存在）
                    var availableGroup = AvailableRaceGroups.FirstOrDefault(g => g.Id == raceGroup.Id);
                    if (availableGroup != null)
                    {
                        AvailableRaceGroups.Remove(availableGroup);
                    }
                }

                UpdateSelectionState();
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"恢复活跃比赛失败: {ex.Message}", ex);
                // 不显示错误消息，避免启动时打扰用户
            }
        }

        /// <summary>
        /// 添加选中的分组到比赛列表
        /// </summary>
        [RelayCommand]
        private async Task AddGroupToRaceAsync()
        {
            if (SelectedAvailableGroup == null)
            {
                MessageBox.Show("请选择一个比赛分组", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SelectedProject == null || SelectedProject.Id == 0)
            {
                MessageBox.Show("请先选择一个项目", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                IsLoading = true;
                await Task.Delay(50); // 让UI有机会刷新显示遮罩层

                _loggingService?.Info($"[按钮点击] 比赛计时 - 添加比赛组按钮, 项目: {SelectedProject.Name}, 分组: {SelectedAvailableGroup.DisplayName}");

                var group = SelectedAvailableGroup;
                var laps = group.RaceLaps > 0 ? group.RaceLaps : 1;

                // 1. 获取该分组的参赛人员（使用 RaceGroupRepository 的方法，它正确处理 null 值和 ProjectId）
                var participants = (await _raceGroupRepository.GetParticipantsByGroupAsync(
                    SelectedProject.Id,
                    group.School,
                    group.Grade,
                    group.Class,
                    group.GroupName)).ToList();

                if (participants.Count == 0)
                {
                    MessageBox.Show($"分组 [{group.DisplayName}] 没有参赛者，无法添加", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsLoading = false;
                    return;
                }

                // 2. 从ParticipantGroups表查询该分组的配置信息（圈数、ChipGroupId、ChipGroupName）
                var participantGroup = await _participantGroupRepository.GetByGroupInfoAsync(
                    SelectedProject.Id,
                    group.School,
                    group.Grade,
                    group.Class,
                    group.GroupName);

                // 如果当前项目下没有，尝试查询所有项目下的ParticipantGroup
                if (participantGroup == null)
                {
                    var allParticipantGroups = await _participantGroupRepository.QueryAsync(
                        null, // 不限制项目
                        group.School,
                        group.Grade,
                        group.Class,
                        group.GroupName);
                    
                    participantGroup = allParticipantGroups.FirstOrDefault();
                }

                // 从ParticipantGroups表获取配置信息
                int? chipGroupId = participantGroup?.ChipGroupId;
                string? chipGroupName = participantGroup?.ChipGroupName;
                int raceLaps = participantGroup?.RaceLaps ?? (group.RaceLaps > 0 ? group.RaceLaps : 1);

                // 如果ChipGroupName为空但ChipGroupId不为空，从ChipGroups表获取
                if (string.IsNullOrEmpty(chipGroupName) && chipGroupId.HasValue)
                {
                    var chipGroup = await _chipRepository.GetChipGroupByIdAsync(chipGroupId.Value);
                    if (chipGroup != null)
                    {
                        chipGroupName = chipGroup.ChipGroupName;
                    }
                }

                // 3. 查询是否已存在该分组的 RaceGroup
                var existingRaceGroups = await _raceGroupRepository.QueryRaceGroupsAsync(
                    SelectedProject.Id,
                    group.School,
                    group.Grade,
                    group.Class,
                    group.GroupName);
                
                var existingRaceGroup = existingRaceGroups.FirstOrDefault();

                int raceGroupId;
                RaceGroup raceGroup;
                
                if (existingRaceGroup != null)
                {
                    // 使用现有的 RaceGroup
                    raceGroupId = existingRaceGroup.Id;
                    raceGroup = existingRaceGroup;
                    // 更新信息
                    raceGroup.ParticipantCount = participants.Count;
                    raceGroup.RaceLaps = raceLaps; // 使用ParticipantGroups表中的圈数
                    
                    // 使用ParticipantGroups表中的芯片组信息
                    if (chipGroupId.HasValue)
                    {
                        raceGroup.ChipGroupId = chipGroupId;
                        raceGroup.ChipGroupName = chipGroupName;
                    }
                    // 如果ParticipantGroups表中没有，但RaceGroup中有，保留RaceGroup中的
                    else if (raceGroup.ChipGroupId.HasValue && string.IsNullOrEmpty(raceGroup.ChipGroupName))
                    {
                        var chipGroup = await _chipRepository.GetChipGroupByIdAsync(raceGroup.ChipGroupId.Value);
                        if (chipGroup != null)
                        {
                            raceGroup.ChipGroupName = chipGroup.ChipGroupName;
                        }
                    }
                    
                    await _raceGroupRepository.UpdateAsync(raceGroup);
                    _loggingService?.Info($"更新比赛分组: ID={raceGroupId}, 名称={raceGroup.DisplayName}, RaceLaps={raceGroup.RaceLaps}, ChipGroupId={raceGroup.ChipGroupId}, ChipGroupName={raceGroup.ChipGroupName}");
                }
                else
                {
                    // 创建新的 RaceGroup，使用ParticipantGroups表中的配置信息
                    raceGroup = new RaceGroup
                    {
                        ProjectId = SelectedProject.Id,
                        School = group.School,
                        Grade = group.Grade,
                        Class = group.Class,
                        GroupName = group.GroupName,
                        ParticipantCount = participants.Count,
                        RaceLaps = raceLaps, // 使用ParticipantGroups表中的圈数
                        ChipGroupId = chipGroupId,
                        ChipGroupName = chipGroupName,
                        Status = RaceStatus.Pending,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    raceGroupId = await _raceGroupRepository.CreateAsync(raceGroup);
                    _loggingService?.Info($"创建比赛分组: ID={raceGroupId}, 名称={raceGroup.DisplayName}, RaceLaps={raceGroup.RaceLaps}, ChipGroupId={raceGroup.ChipGroupId}, ChipGroupName={raceGroup.ChipGroupName}");
                }

                // 3. 删除该 RaceGroup 下现有的 RaceRecords（如果存在）
                await _raceRecordRepository.DeleteByRaceGroupIdAsync(raceGroupId);

                // 4. 为每个参赛人员创建 RaceRecord
                var raceRecords = new List<RaceRecord>();
                foreach (var participant in participants)
                {
                    var raceRecord = new RaceRecord
                    {
                        RaceGroupId = raceGroupId,
                        ProjectId = SelectedProject.Id,
                        SequenceNumber = participant.SequenceNumber,
                        School = participant.School ?? string.Empty,
                        Grade = participant.Grade,
                        Class = participant.Class,
                        GroupName = participant.GroupName ?? string.Empty,
                        LabelNumber = participant.LabelNumber,
                        Name = participant.Name,
                        Gender = participant.Gender,
                        Lap1Time = "00:00:00.000",
                        Lap2Time = "00:00:00.000",
                        TotalTime = "00:00:00.000",
                        Status = RaceStatus.Pending,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    raceRecords.Add(raceRecord);
                }

                await _raceRecordRepository.CreateBatchAsync(raceRecords);
                _loggingService?.Info($"创建比赛记录: RaceGroupId={raceGroupId}, 记录数={raceRecords.Count}");

                // 5. 重新从数据库加载 RaceGroup 以确保包含芯片组信息
                var loadedRaceGroup = await _raceGroupRepository.GetByIdAsync(raceGroupId);
                if (loadedRaceGroup != null)
                {
                    raceGroup = loadedRaceGroup;
                }

                // 6. 创建 TimingInfo 并加载参赛者
                var timingInfo = CreateTimingInfoFromGroup(raceGroup, laps);
                timingInfo.RaceGroupId = raceGroupId;
                await LoadParticipantsFromRaceRecordsAsync(timingInfo, raceGroupId);

                // 6. 添加到比赛列表（后加入排在上面）
                RaceGroups.Insert(0, timingInfo);

                // 7. 从可用列表中移除
                AvailableRaceGroups.Remove(group);
                SelectedAvailableGroup = null;

                UpdateSelectionState();
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"添加分组失败: {ex.Message}", ex);
                MessageBox.Show($"添加分组失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 从比赛列表移除分组
        /// </summary>
        [RelayCommand]
        private async Task RemoveGroupFromRaceAsync(RaceGroupTimingInfo group)
        {
            if (group == null) return;

            if (group.IsRaceActive)
            {
                MessageBox.Show("比赛进行中，无法移除。请先停止比赛。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"确定要从比赛列表中移除 [{group.DisplayName}] 吗？",
                "确认移除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            // 移除分组
            RaceGroups.Remove(group);
            group.Dispose();

            // 重新添加到可用列表（仅当该组的项目与当前选中的项目一致时）
            var raceGroup = await _raceGroupRepository.GetByIdAsync(group.RaceGroupId);
            if (raceGroup != null && SelectedProject != null && raceGroup.ProjectId == SelectedProject.Id)
            {
                AvailableRaceGroups.Add(raceGroup);
            }

            UpdateSelectionState();
        }

        /// <summary>
        /// 从 RaceGroup 创建 TimingInfo
        /// </summary>
        private RaceGroupTimingInfo CreateTimingInfoFromGroup(RaceGroup group, int totalLaps)
        {
            return new RaceGroupTimingInfo
            {
                RaceGroupId = group.Id,
                DisplayName = group.DisplayName,
                School = group.School,
                Grade = group.Grade,
                Class = group.Class,
                GroupName = group.GroupName,
                ChipGroupColor = group.ChipGroupColor,
                ChipGroupName = group.ChipGroupName,
                ParticipantCount = group.ParticipantCount,
                TotalLaps = totalLaps
            };
        }

        /// <summary>
        /// 切换分组展开/折叠状态
        /// </summary>
        [RelayCommand]
        private void ToggleExpand(RaceGroupTimingInfo? group)
        {
            if (group == null) return;
            group.IsExpanded = !group.IsExpanded;
        }

        /// <summary>
        /// 单独启动一个比赛组
        /// </summary>
        [RelayCommand]
        private async Task StartSingleRaceAsync(RaceGroupTimingInfo group)
        {
            if (group == null || !group.CanStart) return;

            try
            {
                if (group.ParticipantCount == 0)
                {
                    MessageBox.Show($"分组 [{group.DisplayName}] 没有参赛者", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 启动比赛（更新 RaceGroup 和所有 RaceRecords 的状态）
                var race = await _timerService.StartRaceAsync(group.RaceGroupId, group.TotalLaps);
                group.StartTimer(DateTime.Now);

                // 更新该分组下所有参赛人员的状态
                var raceRecords = await _raceRecordRepository.GetByRaceGroupIdAsync(group.RaceGroupId);
                foreach (var record in raceRecords)
                {
                    record.Status = RaceStatus.Running;
                    await _raceRecordRepository.UpdateAsync(record);
                }

                // 重置参赛者状态
                foreach (var p in group.Participants)
                {
                    p.CurrentLap = 0;
                    p.Lap1Time = TimeSpan.Zero;
                    p.Lap2Time = TimeSpan.Zero;
                    p.TotalTime = TimeSpan.Zero;
                    p.LastLapTime = null;
                    p.IsCompleted = false;
                    p.IsLeading = false;
                    p.Rank = 0;
                    p.IsRacing = true;  // 标记比赛开始
                    p.LiveElapsedTime = TimeSpan.Zero;  // 重置实时用时
                    p.Status = "进行中";
                }

                UpdateSelectionState();
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"启动比赛失败: {ex.Message}", ex);
                MessageBox.Show($"启动比赛失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 暂停单个比赛组
        /// </summary>
        [RelayCommand]
        private async Task PauseSingleRaceAsync(RaceGroupTimingInfo group)
        {
            if (group == null || !group.CanPause) return;

            try
            {
                await _timerService.PauseRaceAsync(group.RaceRecordId);
                group.PauseTimer();
                UpdateSelectionState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"暂停比赛失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 继续单个比赛组
        /// </summary>
        [RelayCommand]
        private async Task ResumeSingleRaceAsync(RaceGroupTimingInfo group)
        {
            if (group == null || !group.CanResume) return;

            try
            {
                await _timerService.ResumeRaceAsync(group.RaceRecordId);
                group.ResumeTimer();
                UpdateSelectionState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"继续比赛失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 重跑（停止并重置比赛，可重新开始）
        /// </summary>
        [RelayCommand]
        private async Task StopSingleRaceAsync(RaceGroupTimingInfo group)
        {
            if (group == null || !group.CanStop) return;

            var result = MessageBox.Show(
                $"确定要对 [{group.DisplayName}] 执行重跑吗？\n\n计时器和所有参赛者的成绩将被清零，可重新开始比赛。",
                "确认重跑",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                await _timerService.StopRaceAsync(group.RaceRecordId);
                
                // 重置计时器（清零）
                group.ResetTimer();
                group.RaceRecordId = 0;  // 清除比赛记录ID，以便重新开始
                
                // 重置所有参赛者的状态和成绩
                foreach (var p in group.Participants)
                {
                    p.IsRacing = false;
                    p.CurrentLap = 0;
                    p.TotalTime = TimeSpan.Zero;
                    p.LastLapTime = null;
                    p.LapTimes.Clear();
                    p.IsCompleted = false;
                    p.IsLeading = false;
                    p.Rank = 0;
                    p.LiveElapsedTime = TimeSpan.Zero;
                    p.NotifyAllLapsChanged();
                }
                
                // 重置完成人数
                group.CompletedCount = 0;
                
                UpdateSelectionState();
                
                MessageBox.Show($"[{group.DisplayName}] 已重置，可重新开始比赛。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重跑失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 批量启动选中的比赛组
        /// </summary>
        [RelayCommand]
        private async Task StartSelectedRacesAsync()
        {
            var selectedGroups = RaceGroups.Where(g => g.IsSelected && g.CanStart).ToList();
            if (selectedGroups.Count == 0)
            {
                MessageBox.Show("请选择可以启动的比赛组", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确定要启动选中的 {selectedGroups.Count} 个比赛组吗？",
                "批量启动",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            foreach (var group in selectedGroups)
            {
                await StartSingleRaceAsync(group);
            }
        }

        /// <summary>
        /// 批量停止选中的比赛组
        /// </summary>
        [RelayCommand]
        private async Task StopSelectedRacesAsync()
        {
            var selectedGroups = RaceGroups.Where(g => g.IsSelected && g.CanStop).ToList();
            if (selectedGroups.Count == 0)
            {
                MessageBox.Show("请选择正在进行的比赛组", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确定要停止选中的 {selectedGroups.Count} 个比赛组吗？",
                "批量停止",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            foreach (var group in selectedGroups)
            {
                try
                {
                    await _timerService.StopRaceAsync(group.RaceRecordId);
                    group.StopTimer(RaceStatus.Stopped);
                    
                    // 重置参赛者的比赛状态
                    foreach (var p in group.Participants)
                    {
                        p.IsRacing = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"停止 [{group.DisplayName}] 失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            UpdateSelectionState();
        }

        /// <summary>
        /// 全选/取消全选
        /// </summary>
        [RelayCommand]
        private void ToggleSelectAll()
        {
            var newState = !IsSelectAllChecked;
            foreach (var group in RaceGroups)
            {
                group.IsSelected = newState;
            }
            IsSelectAllChecked = newState;
            UpdateSelectionState();
        }

        /// <summary>
        /// 记录圈次（单个参赛者）
        /// </summary>
        [RelayCommand]
        private async Task RecordLapAsync(ParticipantTimingInfo participant)
        {
            if (participant == null) return;

            // 找到该参赛者所属的比赛组
            var group = RaceGroups.FirstOrDefault(g => 
                g.Participants.Contains(participant) && g.Status == RaceStatus.Running);

            if (group == null)
            {
                MessageBox.Show("该参赛者所属的比赛未在进行中", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (participant.CurrentLap >= group.TotalLaps)
            {
                MessageBox.Show("该选手已完成所有圈数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // 在新架构中，使用 participant.RaceRecordId 来记录圈次
                await _timerService.RecordLapAsync(participant.RaceRecordId, participant.ParticipantId, DateTime.Now);
                
                // 重新加载该参赛人员的记录以获取最新数据
                var updatedRecord = await _raceRecordRepository.GetByIdAsync(participant.RaceRecordId);
                if (updatedRecord != null)
                {
                    // 计算当前圈数
                    int currentLap = 0;
                    if (!string.IsNullOrWhiteSpace(updatedRecord.Lap2Time) && updatedRecord.Lap2Time != "00:00:00.000")
                        currentLap = 2;
                    else if (!string.IsNullOrWhiteSpace(updatedRecord.Lap1Time) && updatedRecord.Lap1Time != "00:00:00.000")
                        currentLap = 1;
                    
                    // 更新UI显示
                    participant.CurrentLap = currentLap;
                    participant.Lap1Time = ParseTimeString(updatedRecord.Lap1Time);
                    participant.Lap2Time = ParseTimeString(updatedRecord.Lap2Time);
                    participant.TotalTime = ParseTimeString(updatedRecord.TotalTime);
                    
                    // 更新最后一圈时间
                    if (currentLap == 1)
                    {
                        participant.LastLapTime = participant.Lap1Time;
                    }
                    else if (currentLap == 2)
                    {
                        participant.LastLapTime = participant.Lap2Time;
                    }
                    
                    // 计算排名（从所有记录中计算）
                    var allRecords = await _raceRecordRepository.GetByRaceGroupIdAsync(updatedRecord.RaceGroupId);
                    var sortedRecords = allRecords
                        .Where(r => !string.IsNullOrWhiteSpace(r.TotalTime) && r.TotalTime != "00:00:00.000")
                        .OrderBy(r => ParseTimeString(r.TotalTime))
                        .ToList();
                    participant.Rank = sortedRecords.FindIndex(r => r.Id == updatedRecord.Id) + 1;
                    
                    participant.Status = GetStatusText(updatedRecord.Status);
                    participant.IsCompleted = updatedRecord.Status == RaceStatus.Completed;
                }

                // 检查是否完成比赛
                if (participant.CurrentLap >= group.TotalLaps)
                {
                    participant.IsCompleted = true;
                }

                // 更新排名
                group.UpdateRankings();

                // 检查是否所有选手都已完成
                if (group.Participants.All(p => p.IsCompleted))
                {
                    await CompleteRaceAsync(group);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"记录圈次失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 快速记圈（通过号码布/芯片号）
        /// </summary>
        [RelayCommand]
        private async Task QuickRecordLapAsync()
        {
            if (string.IsNullOrWhiteSpace(QuickLapInput)) return;

            var input = QuickLapInput.Trim();

            // 在所有正在进行的比赛中查找参赛者
            ParticipantTimingInfo? foundParticipant = null;

            foreach (var group in RaceGroups.Where(g => g.Status == RaceStatus.Running))
            {
                var participant = group.Participants.FirstOrDefault(p =>
                    p.LabelNumber == input || p.InternalNumber == input);

                if (participant != null)
                {
                    foundParticipant = participant;
                    break;
                }
            }

            if (foundParticipant == null)
            {
                MessageBox.Show($"在进行中的比赛中未找到号码布为 '{input}' 的参赛者", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await RecordLapAsync(foundParticipant);
            QuickLapInput = string.Empty;
        }

        /// <summary>
        /// 完成比赛
        /// </summary>
        private async Task CompleteRaceAsync(RaceGroupTimingInfo group)
        {
            try
            {
                await _timerService.CompleteRaceAsync(group.RaceGroupId);
                group.StopTimer(RaceStatus.Completed);
                
                // 重置参赛者的比赛状态（虽然已完成，但语义上比赛已结束）
                foreach (var p in group.Participants)
                {
                    p.IsRacing = false;
                    p.Status = "已完成";
                }
                
                UpdateSelectionState();

                MessageBox.Show($"[{group.DisplayName}] 比赛已完成！所有选手都已完成比赛。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"完成比赛失败: {ex.Message}", ex);
                MessageBox.Show($"完成比赛失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 从 RaceRecords 加载分组的参赛者
        /// </summary>
        private async Task LoadParticipantsFromRaceRecordsAsync(RaceGroupTimingInfo group, int raceGroupId)
        {
            try
            {
                var raceRecords = await _raceRecordRepository.GetByRaceGroupIdAsync(raceGroupId);
                
                group.Participants.Clear();
                foreach (var raceRecord in raceRecords)
                {
                    // 解析时间字符串为 TimeSpan
                    TimeSpan lap1Time = ParseTimeString(raceRecord.Lap1Time);
                    TimeSpan lap2Time = ParseTimeString(raceRecord.Lap2Time);
                    TimeSpan totalTime = ParseTimeString(raceRecord.TotalTime);

                    var participantInfo = new ParticipantTimingInfo
                    {
                        RaceRecordId = raceRecord.Id,
                        Rank = 0,
                        LabelNumber = raceRecord.LabelNumber ?? "-",
                        Name = raceRecord.Name,
                        Gender = raceRecord.Gender,
                        Lap1Time = lap1Time,
                        Lap2Time = lap2Time,
                        TotalTime = totalTime,
                        Status = GetStatusText(raceRecord.Status),
                        IsLeading = false,
                        IsCompleted = raceRecord.Status == RaceStatus.Completed
                    };

                    // 根据圈数设置当前圈数
                    if (lap2Time > TimeSpan.Zero)
                    {
                        participantInfo.CurrentLap = 2;
                    }
                    else if (lap1Time > TimeSpan.Zero)
                    {
                        participantInfo.CurrentLap = 1;
                    }
                    else
                    {
                        participantInfo.CurrentLap = 0;
                    }

                    group.Participants.Add(participantInfo);
                }

                group.ParticipantCount = group.Participants.Count;
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"从 RaceRecords 加载参赛者失败: {ex.Message}", ex);
                MessageBox.Show($"加载参赛者失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 解析时间字符串（格式：HH:mm:ss.fff）为 TimeSpan
        /// </summary>
        private TimeSpan ParseTimeString(string timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString) || timeString == "00:00:00.000")
            {
                return TimeSpan.Zero;
            }

            try
            {
                var parts = timeString.Split(':');
                if (parts.Length == 3)
                {
                    var hours = int.Parse(parts[0]);
                    var minutes = int.Parse(parts[1]);
                    var secondsAndMs = parts[2].Split('.');
                    var seconds = int.Parse(secondsAndMs[0]);
                    var milliseconds = secondsAndMs.Length > 1 ? int.Parse(secondsAndMs[1]) : 0;
                    return new TimeSpan(0, hours, minutes, seconds, milliseconds);
                }
            }
            catch
            {
                // 解析失败，返回零
            }

            return TimeSpan.Zero;
        }

        /// <summary>
        /// 获取状态文本
        /// </summary>
        private string GetStatusText(RaceStatus status)
        {
            return status switch
            {
                RaceStatus.Pending => "待开始",
                RaceStatus.Running => "进行中",
                RaceStatus.Paused => "已暂停",
                RaceStatus.Completed => "已完成",
                RaceStatus.Stopped => "已停止",
                _ => "待开始"
            };
        }

        /// <summary>
        /// 加载分组的参赛者（兼容旧方法，现在从 RaceRecords 加载）
        /// </summary>
        private async Task LoadParticipantsForGroupAsync(RaceGroupTimingInfo group)
        {
            if (group.RaceGroupId > 0)
            {
                await LoadParticipantsFromRaceRecordsAsync(group, group.RaceGroupId);
            }
        }


        /// <summary>
        /// 更新选择状态
        /// </summary>
        private void UpdateSelectionState()
        {
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasSelectedGroups));
            OnPropertyChanged(nameof(HasActiveRaces));
            OnPropertyChanged(nameof(HasRaceGroups));
        }

        private async Task InitializeAsync()
        {
            await LoadProjectsAsync();
            await RestoreActiveRacesAsync();
        }

        /// <summary>
        /// 加载可用的项目列表（状态为正常的项目）
        /// </summary>
        [RelayCommand]
        private async Task LoadProjectsAsync()
        {
            try
            {
                var projects = await _projectRepository.GetActiveProjectsAsync();
                AvailableProjects.Clear();
                foreach (var project in projects)
                {
                    AvailableProjects.Add(project);
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载项目列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 项目选择改变时的处理
        /// </summary>
        partial void OnSelectedProjectChanged(Project? value)
        {
            // 清空比赛组选择
            SelectedAvailableGroup = null;
            AvailableRaceGroups.Clear();

            if (value != null)
            {
                _ = LoadRaceGroupsByProjectAsync(value.Id);
            }
        }

        /// <summary>
        /// 根据项目ID加载“可选比赛组”（来源 ParticipantGroups，展示 School-Grade-Class-GroupName）
        /// </summary>
        private async Task LoadRaceGroupsByProjectAsync(int projectId)
        {
            try
            {
                IsLoading = true;

                AvailableRaceGroups.Clear();

                // 未选择项目：不展示任何可选比赛组
                if (projectId <= 0)
                {
                    return;
                }

                // 1) 从 ParticipantGroups 读取该项目下所有分组配置
                var participantGroups = (await _participantGroupRepository.QueryAsync(
                    projectId,
                    school: null,
                    grade: null,
                    classValue: null,
                    groupName: null)).ToList();

                // 2) 用 ChipGroups 补齐颜色/名称（如果配置里缺失）
                var chipGroups = (await _chipRepository.GetAllChipGroupsAsync()).ToList();
                var chipGroupById = chipGroups.ToDictionary(c => c.Id, c => c);

                // 3) 过滤掉当前 UI 已经添加的分组（通过 RaceGroups 表的 Id 来过滤更准确）
                var existingRaceGroups = (await _raceGroupRepository.GetByProjectIdAsync(projectId)).ToList();
                var existingRaceGroupIdSet = existingRaceGroups
                    .Where(g => RaceGroups.Any(r => r.RaceGroupId == g.Id))
                    .Select(g => g.Id)
                    .ToHashSet();

                foreach (var pg in participantGroups)
                {
                    // 如果该 ParticipantGroup 已经对应一个 RaceGroup 且已在 UI 中展示，则跳过
                    var matchedRaceGroup = existingRaceGroups.FirstOrDefault(rg =>
                        rg.School == pg.School &&
                        rg.Grade == pg.Grade &&
                        rg.Class == pg.Class &&
                        rg.GroupName == pg.GroupName);
                    if (matchedRaceGroup != null && existingRaceGroupIdSet.Contains(matchedRaceGroup.Id))
                    {
                        continue;
                    }

                    var option = new RaceGroup
                    {
                        // 注意：这里的 Id 不是 RaceGroups 表的 Id，而是“可选项”的临时对象
                        // 添加到比赛时，会按 School/Grade/Class/GroupName 去查找/创建 RaceGroups 记录。
                        Id = 0,
                        ProjectId = pg.ProjectId,
                        School = pg.School,
                        Grade = pg.Grade,
                        Class = pg.Class,
                        GroupName = pg.GroupName,
                        RaceLaps = pg.RaceLaps > 0 ? pg.RaceLaps : 1,
                        ChipGroupId = pg.ChipGroupId,
                        ChipGroupName = pg.ChipGroupName
                    };

                    if (option.ChipGroupId.HasValue && chipGroupById.TryGetValue(option.ChipGroupId.Value, out var cg))
                    {
                        option.ChipGroupName ??= cg.ChipGroupName;
                        option.ChipGroupColor = cg.Color;
                    }

                    AvailableRaceGroups.Add(option);
                }
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"加载比赛分组失败: {ex.Message}", ex);
                MessageBox.Show($"加载比赛分组失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 删除参赛人员记录
        /// </summary>
        [RelayCommand]
        private async Task DeleteParticipantAsync(ParticipantTimingInfo? participant)
        {
            if (participant == null || participant.RaceRecordId == 0)
            {
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除参赛人员 \"{participant.Name}\" 的记录吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                await _raceRecordRepository.DeleteAsync(participant.RaceRecordId);
                _loggingService?.Info($"删除比赛记录: ID={participant.RaceRecordId}, 姓名={participant.Name}");

                // 从对应的比赛组中移除
                var group = RaceGroups.FirstOrDefault(g => g.Participants.Contains(participant));
                if (group != null)
                {
                    group.Participants.Remove(participant);
                    group.ParticipantCount = group.Participants.Count;
                    
                    // 更新 RaceGroup 的 ParticipantCount
                    var raceGroup = await _raceGroupRepository.GetByIdAsync(group.RaceGroupId);
                    if (raceGroup != null)
                    {
                        raceGroup.ParticipantCount = group.ParticipantCount;
                        await _raceGroupRepository.UpdateAsync(raceGroup);
                    }
                }

                MessageBox.Show("删除成功。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"删除比赛记录失败: {ex.Message}", ex);
                MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                WeakReferenceMessenger.Default.UnregisterAll(this);
                foreach (var group in RaceGroups)
                {
                    group.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
