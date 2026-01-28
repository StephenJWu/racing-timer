using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Timer.Data;
using Timer.Messages;
using Timer.Models;
using Timer.Services;

namespace Timer.ViewModels
{
    /// <summary>
    /// 比赛计时页面的ViewModel
    /// </summary>
    public partial class RaceTimerViewModel : ObservableObject, IDisposable, IRecipient<ChipGroupUpdatedMessage>, IRecipient<DataReloadRequestedMessage>
    {
        private readonly IRaceGroupRepository _raceGroupRepository;
        private readonly IRaceRecordRepository _raceRecordRepository;
        private readonly IParticipantRepository _participantRepository;
        private readonly ITimerService _timerService;
        private readonly DatabaseContext _dbContext;
        private readonly DispatcherTimer _timer;
        private DateTime _raceStartTime;
        private bool _disposed;

        [ObservableProperty]
        private ObservableCollection<RaceGroup> _raceGroups = new();

        [ObservableProperty]
        private RaceGroup? _selectedRaceGroup;

        [ObservableProperty]
        private ObservableCollection<ParticipantTimingInfo> _participantTimings = new();

        [ObservableProperty]
        private string _elapsedTimeDisplay = "00:00:00.000";

        [ObservableProperty]
        private RaceStatus _currentStatus = RaceStatus.Stopped;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanEditSettings))]
        private bool _isRaceActive;

        /// <summary>
        /// 是否可以编辑设置（比赛未开始时）
        /// </summary>
        public bool CanEditSettings => !IsRaceActive;

        [ObservableProperty]
        private bool _canStart;

        [ObservableProperty]
        private bool _canPause;

        [ObservableProperty]
        private bool _canResume;

        [ObservableProperty]
        private bool _canStop;

        [ObservableProperty]
        private string _statusText = "待开始";

        [ObservableProperty]
        private string _statusColor = "#94a3b8";

        [ObservableProperty]
        private int _totalLaps = 1;

        [ObservableProperty]
        private ObservableCollection<int> _lapOptions = new();

        [ObservableProperty]
        private int _participantCount;

        [ObservableProperty]
        private bool _isLoading;

        private string _quickLapInput = string.Empty;
        
        /// <summary>
        /// 快速记圈输入（号码布或芯片号）
        /// </summary>
        public string QuickLapInput
        {
            get => _quickLapInput;
            set
            {
                if (SetProperty(ref _quickLapInput, value))
                {
                    QuickRecordLapCommand?.NotifyCanExecuteChanged();
                }
            }
        }

        public string Title => "比赛计时";

        public RaceTimerViewModel(
            IRaceGroupRepository raceGroupRepository,
            IRaceRecordRepository raceRecordRepository,
            IParticipantRepository participantRepository,
            ITimerService timerService,
            DatabaseContext dbContext)
        {
            _raceGroupRepository = raceGroupRepository ?? throw new ArgumentNullException(nameof(raceGroupRepository));
            _raceRecordRepository = raceRecordRepository ?? throw new ArgumentNullException(nameof(raceRecordRepository));
            _participantRepository = participantRepository ?? throw new ArgumentNullException(nameof(participantRepository));
            _timerService = timerService ?? throw new ArgumentNullException(nameof(timerService));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            // 初始化计时器（每100ms更新一次）
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += Timer_Tick;

            // 注册跨页面实时刷新（显式注册，避免多 IRecipient<> 时 Register(this) 歧义）
            WeakReferenceMessenger.Default.Register<ChipGroupUpdatedMessage>(this);
            WeakReferenceMessenger.Default.Register<DataReloadRequestedMessage>(this);

            // 初始化圈数选项（1-20圈）
            for (int i = 1; i <= 20; i++)
            {
                LapOptions.Add(i);
            }

            // 初始化命令
            LoadRaceGroupsCommand = new AsyncRelayCommand(LoadRaceGroupsAsync);
            SelectRaceGroupCommand = new AsyncRelayCommand(OnRaceGroupSelectedAsync);
            StartRaceCommand = new AsyncRelayCommand(StartRaceAsync, () => CanStart);
            PauseRaceCommand = new AsyncRelayCommand(PauseRaceAsync, () => CanPause);
            ResumeRaceCommand = new AsyncRelayCommand(ResumeRaceAsync, () => CanResume);
            StopRaceCommand = new AsyncRelayCommand(StopRaceAsync, () => CanStop);
            RecordLapCommand = new AsyncRelayCommand<ParticipantTimingInfo>(RecordLapAsync, p => p != null && IsRaceActive);
            QuickRecordLapCommand = new AsyncRelayCommand(QuickRecordLapAsync, () => IsRaceActive && !string.IsNullOrWhiteSpace(QuickLapInput));

            // 加载数据
            _ = InitializeAsync();
        }

        public void Receive(ChipGroupUpdatedMessage message)
        {
            if (message?.Value == null) return;
            var updated = message.Value;

            foreach (var rg in RaceGroups.Where(r => r.ChipGroupId == updated.Id))
            {
                rg.ChipGroupName = updated.ChipGroupName;
                rg.ChipGroupColor = updated.Color;
            }

            if (SelectedRaceGroup?.ChipGroupId == updated.Id)
            {
                SelectedRaceGroup.ChipGroupName = updated.ChipGroupName;
                SelectedRaceGroup.ChipGroupColor = updated.Color;
            }
        }

        public void Receive(DataReloadRequestedMessage message)
        {
            if (message == null) return;

            if (message.Value == DataDomain.RaceGroups)
            {
                var selectedId = SelectedRaceGroup?.Id;
                _ = ReloadRaceGroupsAndRestoreSelectionAsync(selectedId);
            }
            else if (message.Value == DataDomain.Participants)
            {
                // 当前页面选中分组时，人员数据变化会影响计时列表
                if (SelectedRaceGroup != null)
                {
                    _ = OnRaceGroupSelectedAsync();
                }
            }
        }

        private async Task ReloadRaceGroupsAndRestoreSelectionAsync(int? selectedRaceGroupId)
        {
            await LoadRaceGroupsAsync();

            if (selectedRaceGroupId.HasValue)
            {
                SelectedRaceGroup = RaceGroups.FirstOrDefault(g => g.Id == selectedRaceGroupId.Value);
            }
        }

        public IAsyncRelayCommand LoadRaceGroupsCommand { get; }
        public IAsyncRelayCommand SelectRaceGroupCommand { get; }
        public IAsyncRelayCommand StartRaceCommand { get; }
        public IAsyncRelayCommand PauseRaceCommand { get; }
        public IAsyncRelayCommand ResumeRaceCommand { get; }
        public IAsyncRelayCommand StopRaceCommand { get; }
        public IAsyncRelayCommand<ParticipantTimingInfo> RecordLapCommand { get; }
        public IAsyncRelayCommand QuickRecordLapCommand { get; }

        private async Task InitializeAsync()
        {
            await LoadRaceGroupsAsync();
            
            // 检查是否有活跃的比赛
            var activeRace = await _timerService.LoadActiveRaceAsync();
            if (activeRace != null)
            {
                // 恢复比赛状态
                await RestoreActiveRaceAsync(activeRace);
            }
            else
            {
                UpdateButtonStates();
            }
        }

        private async Task LoadRaceGroupsAsync()
        {
            try
            {
                IsLoading = true;
                var groups = await _raceGroupRepository.GetAllAsync();
                
                RaceGroups.Clear();
                foreach (var group in groups)
                {
                    RaceGroups.Add(group);
                }
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

        private async Task OnRaceGroupSelectedAsync()
        {
            if (SelectedRaceGroup == null)
                return;

            try
            {
                IsLoading = true;
                
                // 加载该分组的参赛人员
                var searchFilter = new SearchFilter
                {
                    School = SelectedRaceGroup.School,
                    Grade = SelectedRaceGroup.Grade,
                    Class = SelectedRaceGroup.Class,
                    GroupName = SelectedRaceGroup.GroupName,
                    PageNumber = 1,
                    PageSize = 1000
                };

                var participants = await _participantRepository.GetAllAsync(searchFilter);
                
                ParticipantTimings.Clear();
                foreach (var participant in participants)
                {
                    ParticipantTimings.Add(new ParticipantTimingInfo
                    {
                        ParticipantId = participant.Id,
                        Rank = 0,
                        LabelNumber = participant.LabelNumber ?? "-",
                        Name = participant.Name,
                        InternalNumber = participant.InternalNumber,
                        CurrentLap = 0,
                        TotalTime = TimeSpan.Zero,
                        LastLapTime = null,
                        Status = "未开始",
                        IsLeading = false,
                        IsCompleted = false
                    });
                }

                // 默认圈数设为1，用户可以手动选择
                if (TotalLaps == 0)
                {
                    TotalLaps = 1;
                }
                ParticipantCount = ParticipantTimings.Count;
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载参赛人员失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task StartRaceAsync()
        {
            if (SelectedRaceGroup == null)
            {
                MessageBox.Show("请先选择比赛分组", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 启动比赛
                var race = await _timerService.StartRaceAsync(SelectedRaceGroup.Id, TotalLaps);
                _raceStartTime = DateTime.Now; // 新架构中不再从 race.StartTime 获取，使用当前时间
                
                CurrentStatus = RaceStatus.Running;
                IsRaceActive = true;
                UpdateStatusDisplay();
                UpdateButtonStates();

                // 启动计时器
                _timer.Start();

                MessageBox.Show("比赛已开始！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"开始比赛失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task PauseRaceAsync()
        {
            try
            {
                await _timerService.PauseRaceAsync();
                _timer.Stop();
                
                CurrentStatus = RaceStatus.Paused;
                UpdateStatusDisplay();
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"暂停比赛失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ResumeRaceAsync()
        {
            try
            {
                await _timerService.ResumeRaceAsync();
                _timer.Start();
                
                CurrentStatus = RaceStatus.Running;
                UpdateStatusDisplay();
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"继续比赛失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task StopRaceAsync()
        {
            var result = MessageBox.Show(
                "确定要停止比赛吗？比赛记录将被保存。",
                "确认停止",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await _timerService.StopRaceAsync();
                _timer.Stop();
                
                CurrentStatus = RaceStatus.Stopped;
                IsRaceActive = false;
                UpdateStatusDisplay();
                UpdateButtonStates();

                MessageBox.Show("比赛已停止", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止比赛失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RecordLapAsync(ParticipantTimingInfo? participant)
        {
            if (participant == null)
                return;

            try
            {
                // 检查是否已完成所有圈数
                if (participant.CurrentLap >= TotalLaps)
                {
                    MessageBox.Show("该选手已完成所有圈数", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 记录圈次
                await _timerService.RecordLapAsync(participant.ParticipantId, DateTime.Now);
                
                // 重新加载记录以获取最新数据
                // 注意：在新架构中，需要通过其他方式获取更新后的数据
                // 这里简化处理，从 RaceRecord 中获取信息
                var raceRecord = await _raceRecordRepository.GetByIdAsync(participant.RaceRecordId);
                if (raceRecord != null)
                {
                    // 计算当前圈数
                    int currentLap = 0;
                    if (!string.IsNullOrWhiteSpace(raceRecord.Lap2Time) && raceRecord.Lap2Time != "00:00:00.000")
                        currentLap = 2;
                    else if (!string.IsNullOrWhiteSpace(raceRecord.Lap1Time) && raceRecord.Lap1Time != "00:00:00.000")
                        currentLap = 1;
                    
                    participant.CurrentLap = currentLap;
                    participant.TotalTime = ParseTimeString(raceRecord.TotalTime);
                    
                    // 计算最后一圈时间
                    if (currentLap == 1)
                        participant.LastLapTime = ParseTimeString(raceRecord.Lap1Time);
                    else if (currentLap == 2)
                        participant.LastLapTime = ParseTimeString(raceRecord.Lap2Time);
                    
                    // 添加本圈用时到列表
                    if (participant.LastLapTime.HasValue)
                        participant.LapTimes.Add(participant.LastLapTime.Value);
                    participant.NotifyAllLapsChanged();
                    
                    // 计算排名
                    var allRecords = await _raceRecordRepository.GetByRaceGroupIdAsync(raceRecord.RaceGroupId);
                    var sortedRecords = allRecords
                        .Where(r => !string.IsNullOrWhiteSpace(r.TotalTime) && r.TotalTime != "00:00:00.000")
                        .OrderBy(r => ParseTimeString(r.TotalTime))
                        .ToList();
                    participant.Rank = sortedRecords.FindIndex(r => r.Id == raceRecord.Id) + 1;
                }

                // 检查是否完成比赛
                if (participant.CurrentLap >= TotalLaps)
                {
                    participant.IsCompleted = true;
                }

                // 更新所有参赛者的排名
                await UpdateAllRankingsAsync();

                // 检查是否所有选手都已完成
                if (ParticipantTimings.All(p => p.IsCompleted))
                {
                    await CompleteRaceAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"记录圈次失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task QuickRecordLapAsync()
        {
            if (string.IsNullOrWhiteSpace(QuickLapInput))
                return;

            try
            {
                var input = QuickLapInput.Trim();
                
                // 根据芯片外部号码/内部号码查找参赛者
                var participant = ParticipantTimings.FirstOrDefault(p => 
                    p.LabelNumber == input || 
                    p.InternalNumber == input);

                if (participant == null)
                {
                    MessageBox.Show($"未找到芯片外部号码/内部号码为 '{input}' 的参赛者", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 调用记圈方法
                await RecordLapAsync(participant);
                
                // 清空输入框以便下次输入
                QuickLapInput = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"快速记圈失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CompleteRaceAsync()
        {
            try
            {
                await _timerService.CompleteRaceAsync();
                _timer.Stop();
                
                CurrentStatus = RaceStatus.Completed;
                IsRaceActive = false;
                UpdateStatusDisplay();
                UpdateButtonStates();

                MessageBox.Show("比赛已完成！所有选手都已完成比赛。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"完成比赛失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateAllRankingsAsync()
        {
            // 按照圈数和用时重新排序
            var sortedParticipants = ParticipantTimings
                .OrderByDescending(p => p.CurrentLap)
                .ThenBy(p => p.TotalTime)
                .ToList();

            for (int i = 0; i < sortedParticipants.Count; i++)
            {
                sortedParticipants[i].Rank = i + 1;
                sortedParticipants[i].IsLeading = (i == 0 && sortedParticipants[i].CurrentLap > 0);
            }

            await Task.CompletedTask;
        }

        private async Task RestoreActiveRaceAsync(RaceRecord activeRace)
        {
            try
            {
                // 加载比赛分组
                var raceGroup = await _raceGroupRepository.GetByIdAsync(activeRace.RaceGroupId);
                if (raceGroup != null)
                {
                    SelectedRaceGroup = raceGroup;
                    await OnRaceGroupSelectedAsync();
                    
                    // 从 RaceGroup 获取 TotalLaps（新架构中 RaceRecord 不再有 TotalLaps 字段）
                    TotalLaps = raceGroup.RaceLaps;
                }
                else
                {
                    TotalLaps = 1; // 默认值
                }

                _raceStartTime = DateTime.Now; // 新架构中不再从 activeRace.StartTime 获取，使用当前时间
                CurrentStatus = activeRace.Status;
                IsRaceActive = (activeRace.Status == RaceStatus.Running || activeRace.Status == RaceStatus.Paused);

                // TODO: 恢复参赛者的圈次记录
                // 这里可以从数据库加载已有的圈次记录并更新UI

                UpdateStatusDisplay();
                UpdateButtonStates();

                if (activeRace.Status == RaceStatus.Running)
                {
                    _timer.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"恢复比赛状态失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (CurrentStatus == RaceStatus.Running)
            {
                var elapsed = DateTime.Now - _raceStartTime;
                ElapsedTimeDisplay = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{elapsed.Milliseconds:D3}";
            }
        }

        private void UpdateButtonStates()
        {
            CanStart = !IsRaceActive && SelectedRaceGroup != null && ParticipantTimings.Count > 0;
            CanPause = IsRaceActive && CurrentStatus == RaceStatus.Running;
            CanResume = IsRaceActive && CurrentStatus == RaceStatus.Paused;
            CanStop = IsRaceActive;

            StartRaceCommand.NotifyCanExecuteChanged();
            PauseRaceCommand.NotifyCanExecuteChanged();
            ResumeRaceCommand.NotifyCanExecuteChanged();
            StopRaceCommand.NotifyCanExecuteChanged();
        }

        private void UpdateStatusDisplay()
        {
            switch (CurrentStatus)
            {
                case RaceStatus.Running:
                    StatusText = "进行中";
                    StatusColor = "#52c41a";
                    break;
                case RaceStatus.Paused:
                    StatusText = "已暂停";
                    StatusColor = "#faad14";
                    break;
                case RaceStatus.Completed:
                    StatusText = "已完成";
                    StatusColor = "#1890ff";
                    break;
                case RaceStatus.Stopped:
                    StatusText = "已停止";
                    StatusColor = "#f5222d";
                    break;
                default:
                    StatusText = "待开始";
                    StatusColor = "#94a3b8";
                    break;
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                WeakReferenceMessenger.Default.UnregisterAll(this);
                _timer?.Stop();
                _disposed = true;
            }
        }
    }
}
