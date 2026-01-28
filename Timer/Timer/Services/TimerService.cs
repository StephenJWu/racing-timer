using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 计时服务实现，支持多组同时计时
    /// </summary>
    public class TimerService : ITimerService
    {
        private readonly IRaceRecordRepository _raceRecordRepository;
        private readonly IRaceGroupRepository _raceGroupRepository;
        private readonly Dictionary<int, RaceGroup> _activeRaceGroups = new();
        private readonly Dictionary<int, DateTime> _raceStartTimes = new(); // 存储每个比赛组的开始时间

        public TimerService(
            IRaceRecordRepository raceRecordRepository,
            IRaceGroupRepository raceGroupRepository)
        {
            _raceRecordRepository = raceRecordRepository ?? throw new ArgumentNullException(nameof(raceRecordRepository));
            _raceGroupRepository = raceGroupRepository ?? throw new ArgumentNullException(nameof(raceGroupRepository));
        }

        public IReadOnlyDictionary<int, RaceRecord> ActiveRaces => new Dictionary<int, RaceRecord>(); // 兼容旧接口，返回空字典

        public bool HasActiveRaces => _activeRaceGroups.Count > 0;

        // 兼容旧接口
        public RaceRecord? CurrentRace => null; // 新架构不再使用单个 RaceRecord

        public bool IsRunning => _activeRaceGroups.Values.Any(r => r.Status == RaceStatus.Running);

        public async Task<RaceRecord> StartRaceAsync(int raceGroupId, int totalLaps)
        {
            // 检查该分组是否已有正在进行的比赛
            if (_activeRaceGroups.ContainsKey(raceGroupId))
            {
                throw new InvalidOperationException("该分组已有正在进行的比赛");
            }

            // 获取比赛分组
            var raceGroup = await _raceGroupRepository.GetByIdAsync(raceGroupId);
            if (raceGroup == null)
            {
                throw new InvalidOperationException($"找不到ID为 {raceGroupId} 的比赛分组");
            }

            // 更新比赛分组状态为 Running
            raceGroup.Status = RaceStatus.Running;
            await _raceGroupRepository.UpdateAsync(raceGroup);

            // 更新该分组下所有参赛人员的 RaceRecords 状态为 Running
            var raceRecords = await _raceRecordRepository.GetByRaceGroupIdAsync(raceGroupId);
            foreach (var record in raceRecords)
            {
                record.Status = RaceStatus.Running;
                await _raceRecordRepository.UpdateAsync(record);
            }
            
            // 添加到活跃比赛列表
            _activeRaceGroups[raceGroupId] = raceGroup;
            
            // 记录比赛开始时间
            var startTime = DateTime.Now;
            _raceStartTimes[raceGroupId] = startTime;

            // 返回一个虚拟的 RaceRecord（兼容旧接口）
            return new RaceRecord
            {
                Id = raceGroupId,
                RaceGroupId = raceGroupId,
                Status = RaceStatus.Running
            };
        }

        public async Task PauseRaceAsync(int raceRecordId)
        {
            // raceRecordId 在新架构中实际上是 raceGroupId
            var raceGroupId = raceRecordId;
            
            if (!_activeRaceGroups.TryGetValue(raceGroupId, out var raceGroup))
            {
                throw new InvalidOperationException("找不到指定的比赛");
            }

            if (raceGroup.Status != RaceStatus.Running)
            {
                throw new InvalidOperationException("比赛未在运行中");
            }

            // 更新比赛分组状态为 Paused
            raceGroup.Status = RaceStatus.Paused;
            await _raceGroupRepository.UpdateAsync(raceGroup);

            // 更新该分组下所有参赛人员的 RaceRecords 状态为 Paused
            var raceRecords = await _raceRecordRepository.GetByRaceGroupIdAsync(raceGroupId);
            foreach (var record in raceRecords.Where(r => r.Status == RaceStatus.Running))
            {
                record.Status = RaceStatus.Paused;
                await _raceRecordRepository.UpdateAsync(record);
            }
        }

        public async Task ResumeRaceAsync(int raceRecordId)
        {
            // raceRecordId 在新架构中实际上是 raceGroupId
            var raceGroupId = raceRecordId;
            
            if (!_activeRaceGroups.TryGetValue(raceGroupId, out var raceGroup))
            {
                throw new InvalidOperationException("找不到指定的比赛");
            }

            if (raceGroup.Status != RaceStatus.Paused)
            {
                throw new InvalidOperationException("比赛未处于暂停状态");
            }

            // 更新比赛分组状态为 Running
            raceGroup.Status = RaceStatus.Running;
            await _raceGroupRepository.UpdateAsync(raceGroup);

            // 更新该分组下所有参赛人员的 RaceRecords 状态为 Running
            var raceRecords = await _raceRecordRepository.GetByRaceGroupIdAsync(raceGroupId);
            foreach (var record in raceRecords.Where(r => r.Status == RaceStatus.Paused))
            {
                record.Status = RaceStatus.Running;
                await _raceRecordRepository.UpdateAsync(record);
            }
        }

        public async Task StopRaceAsync(int raceRecordId)
        {
            // raceRecordId 在新架构中实际上是 raceGroupId
            var raceGroupId = raceRecordId;
            
            if (!_activeRaceGroups.TryGetValue(raceGroupId, out var raceGroup))
            {
                throw new InvalidOperationException("找不到指定的比赛");
            }

            // 更新比赛分组状态为 Stopped
            raceGroup.Status = RaceStatus.Stopped;
            await _raceGroupRepository.UpdateAsync(raceGroup);

            // 更新该分组下所有参赛人员的 RaceRecords 状态为 Stopped，并清零计时
            var raceRecords = await _raceRecordRepository.GetByRaceGroupIdAsync(raceGroupId);
            foreach (var record in raceRecords)
            {
                record.Status = RaceStatus.Stopped;
                record.Lap1Time = "00:00:00.000";
                record.Lap2Time = "00:00:00.000";
                record.TotalTime = "00:00:00.000";
                await _raceRecordRepository.UpdateAsync(record);
            }
            
            // 从活跃列表移除
            _activeRaceGroups.Remove(raceGroupId);
            _raceStartTimes.Remove(raceGroupId);
        }

        public async Task CompleteRaceAsync(int raceRecordId)
        {
            // raceRecordId 在新架构中实际上是 raceGroupId
            var raceGroupId = raceRecordId;
            
            if (!_activeRaceGroups.TryGetValue(raceGroupId, out var raceGroup))
            {
                throw new InvalidOperationException("找不到指定的比赛");
            }

            // 更新比赛分组状态为 Completed
            raceGroup.Status = RaceStatus.Completed;
            await _raceGroupRepository.UpdateAsync(raceGroup);

            // 更新该分组下所有参赛人员的 RaceRecords 状态为 Completed
            var raceRecords = await _raceRecordRepository.GetByRaceGroupIdAsync(raceGroupId);
            foreach (var record in raceRecords.Where(r => r.Status != RaceStatus.Completed))
            {
                record.Status = RaceStatus.Completed;
                await _raceRecordRepository.UpdateAsync(record);
            }
            
            // 从活跃列表移除
            _activeRaceGroups.Remove(raceGroupId);
            _raceStartTimes.Remove(raceGroupId);
        }

        public async Task RecordLapAsync(int raceRecordId, int participantId, DateTime passTime)
        {
            // 在新架构中，raceRecordId 是参赛人员的 RaceRecord ID
            // 获取该参赛人员的比赛记录
            var raceRecord = await _raceRecordRepository.GetByIdAsync(raceRecordId);
            if (raceRecord == null)
            {
                throw new InvalidOperationException($"找不到ID为 {raceRecordId} 的比赛记录");
            }

            // 检查该记录所属的比赛组是否正在运行
            var raceGroup = await _raceGroupRepository.GetByIdAsync(raceRecord.RaceGroupId);
            if (raceGroup == null || raceGroup.Status != RaceStatus.Running)
            {
                throw new InvalidOperationException("比赛未在运行中，无法记录圈次");
            }

            // 获取该比赛组下所有参赛人员的记录（用于计算排名）
            var allRecords = await _raceRecordRepository.GetByRaceGroupIdAsync(raceRecord.RaceGroupId);
            
            // 判断当前是第几圈（通过检查 Lap1Time 和 Lap2Time）
            int lapNumber;
            TimeSpan lapTime;
            TimeSpan totalTime;

            if (string.IsNullOrWhiteSpace(raceRecord.Lap1Time) || raceRecord.Lap1Time == "00:00:00.000")
            {
                // 第一圈：需要知道比赛开始时间
                // 由于新架构中 RaceGroup 没有 StartTime，我们使用当前时间减去一个估算值
                // 或者从第一个记录的创建时间计算
                var firstRecord = allRecords.OrderBy(r => r.CreatedAt).FirstOrDefault();
                var startTime = firstRecord?.CreatedAt ?? DateTime.Now.AddMinutes(-1); // 默认假设1分钟前开始
                var elapsed = passTime - startTime;
                lapNumber = 1;
                lapTime = elapsed;
                totalTime = elapsed;
                
                raceRecord.Lap1Time = FormatTimeSpan(elapsed);
                raceRecord.TotalTime = FormatTimeSpan(elapsed);
            }
            else if (string.IsNullOrWhiteSpace(raceRecord.Lap2Time) || raceRecord.Lap2Time == "00:00:00.000")
            {
                // 第二圈：从第一圈时间和当前时间计算
                var lap1Time = ParseTimeString(raceRecord.Lap1Time);
                
                // 估算第一圈的通过时间（使用开始时间 + 第一圈用时）
                if (!_raceStartTimes.TryGetValue(raceRecord.RaceGroupId, out var startTime))
                {
                    var firstRecord = allRecords.OrderBy(r => r.CreatedAt).FirstOrDefault();
                    startTime = firstRecord?.CreatedAt ?? DateTime.Now.AddMinutes(-1);
                }
                var lap1PassTime = startTime + lap1Time;
                
                // 第二圈用时 = 当前时间 - 第一圈通过时间
                var elapsed = passTime - lap1PassTime;
                lapNumber = 2;
                lapTime = elapsed;
                totalTime = lap1Time + lapTime;
                
                raceRecord.Lap2Time = FormatTimeSpan(lapTime);
                raceRecord.TotalTime = FormatTimeSpan(totalTime);
            }
            else
            {
                // 已经完成两圈，不能再记录
                throw new InvalidOperationException("该参赛人员已完成所有圈数");
            }

            // 更新状态
            raceRecord.Status = RaceStatus.Running;
            raceRecord.UpdatedAt = DateTime.Now;
            await _raceRecordRepository.UpdateAsync(raceRecord);

            // 计算排名（如果需要可以更新到 RaceRecord 中）
            // 注意：新架构中不再使用 LapRecord，排名信息可以存储在 RaceRecord 中
        }

        /// <summary>
        /// 格式化 TimeSpan 为时间字符串（HH:mm:ss.fff）
        /// </summary>
        private string FormatTimeSpan(TimeSpan time)
        {
            return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
        }

        /// <summary>
        /// 解析时间字符串为 TimeSpan
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
        /// 计算排名（基于圈数和总用时）
        /// </summary>
        private int CalculateRank(List<RaceRecord> allRecords, RaceRecord currentRecord, int currentLap, TimeSpan totalTime)
        {
            var totalTimeMs = (long)totalTime.TotalMilliseconds;
            
            // 排名规则：圈数多的排前面，圈数相同时用时少的排前面
            var rank = allRecords
                .Where(r => r.Id != currentRecord.Id)
                .Count(r =>
                {
                    var rLap1 = ParseTimeString(r.Lap1Time);
                    var rLap2 = ParseTimeString(r.Lap2Time);
                    var rLap = rLap2 > TimeSpan.Zero ? 2 : (rLap1 > TimeSpan.Zero ? 1 : 0);
                    var rTotal = ParseTimeString(r.TotalTime);
                    var rTotalMs = (long)rTotal.TotalMilliseconds;
                    
                    return rLap > currentLap || (rLap == currentLap && rTotalMs < totalTimeMs);
                }) + 1;

            return rank;
        }

        public async Task<int> GetParticipantCurrentLapAsync(int raceRecordId, int participantId)
        {
            // 在新架构中，raceRecordId 是参赛人员的 RaceRecord ID
            var raceRecord = await _raceRecordRepository.GetByIdAsync(raceRecordId);
            if (raceRecord == null)
            {
                return 0;
            }

            // 根据 Lap1Time 和 Lap2Time 判断当前圈数
            var lap1Time = ParseTimeString(raceRecord.Lap1Time);
            var lap2Time = ParseTimeString(raceRecord.Lap2Time);
            
            if (lap2Time > TimeSpan.Zero)
            {
                return 2;
            }
            else if (lap1Time > TimeSpan.Zero)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public async Task<long> GetParticipantTotalTimeAsync(int raceRecordId, int participantId)
        {
            // 在新架构中，raceRecordId 是参赛人员的 RaceRecord ID
            var raceRecord = await _raceRecordRepository.GetByIdAsync(raceRecordId);
            if (raceRecord == null)
            {
                return 0;
            }

            var totalTime = ParseTimeString(raceRecord.TotalTime);
            return (long)totalTime.TotalMilliseconds;
        }

        public async Task<int> CalculateRankAsync(int raceRecordId, int participantId)
        {
            // 在新架构中，raceRecordId 是参赛人员的 RaceRecord ID
            var raceRecord = await _raceRecordRepository.GetByIdAsync(raceRecordId);
            if (raceRecord == null)
            {
                return 0;
            }

            var allRecords = await _raceRecordRepository.GetByRaceGroupIdAsync(raceRecord.RaceGroupId);
            var totalTime = ParseTimeString(raceRecord.TotalTime);
            var lap1Time = ParseTimeString(raceRecord.Lap1Time);
            var lap2Time = ParseTimeString(raceRecord.Lap2Time);
            var currentLap = lap2Time > TimeSpan.Zero ? 2 : (lap1Time > TimeSpan.Zero ? 1 : 0);
            
            return CalculateRank(allRecords, raceRecord, currentLap, totalTime);
        }

        public async Task<List<RaceRecord>> LoadActiveRacesAsync()
        {
            _activeRaceGroups.Clear();
            
            // 加载所有状态不是 Pending 的比赛组
            var allRaceGroups = await _raceGroupRepository.GetAllAsync();
            var activeRaceGroups = allRaceGroups.Where(rg => rg.Status != RaceStatus.Pending).ToList();
            
            foreach (var raceGroup in activeRaceGroups)
            {
                _activeRaceGroups[raceGroup.Id] = raceGroup;
            }
            
            // 返回虚拟的 RaceRecord 列表（兼容旧接口）
            return activeRaceGroups.Select(rg => new RaceRecord
            {
                Id = rg.Id,
                RaceGroupId = rg.Id,
                Status = rg.Status
            }).ToList();
        }


        #region 兼容旧接口实现

        public async Task PauseRaceAsync()
        {
            var raceGroup = _activeRaceGroups.Values.FirstOrDefault();
            if (raceGroup != null)
            {
                await PauseRaceAsync(raceGroup.Id);
            }
        }

        public async Task ResumeRaceAsync()
        {
            var raceGroup = _activeRaceGroups.Values.FirstOrDefault(r => r.Status == RaceStatus.Paused);
            if (raceGroup != null)
            {
                await ResumeRaceAsync(raceGroup.Id);
            }
        }

        public async Task StopRaceAsync()
        {
            var raceGroup = _activeRaceGroups.Values.FirstOrDefault();
            if (raceGroup != null)
            {
                await StopRaceAsync(raceGroup.Id);
            }
        }

        public async Task CompleteRaceAsync()
        {
            var raceGroup = _activeRaceGroups.Values.FirstOrDefault();
            if (raceGroup != null)
            {
                await CompleteRaceAsync(raceGroup.Id);
            }
        }

        public async Task RecordLapAsync(int participantId, DateTime passTime)
        {
            var raceGroup = _activeRaceGroups.Values.FirstOrDefault(r => r.Status == RaceStatus.Running);
            if (raceGroup == null)
            {
                throw new InvalidOperationException("没有正在进行的比赛");
            }
            
            // 在新架构中，需要通过 participantId 找到对应的 RaceRecord
            // 这里简化处理，假设 participantId 就是 raceRecordId
            await RecordLapAsync(participantId, participantId, passTime);
        }

        public async Task<int> GetParticipantCurrentLapAsync(int participantId)
        {
            // 在新架构中，participantId 实际上是 raceRecordId
            return await GetParticipantCurrentLapAsync(participantId, participantId);
        }

        public async Task<long> GetParticipantTotalTimeAsync(int participantId)
        {
            // 在新架构中，participantId 实际上是 raceRecordId
            return await GetParticipantTotalTimeAsync(participantId, participantId);
        }

        public async Task<int> CalculateRankAsync(int participantId)
        {
            // 在新架构中，participantId 实际上是 raceRecordId
            return await CalculateRankAsync(participantId, participantId);
        }

        public async Task<RaceRecord?> LoadActiveRaceAsync()
        {
            var races = await LoadActiveRacesAsync();
            return races.FirstOrDefault();
        }

        #endregion
    }
}
