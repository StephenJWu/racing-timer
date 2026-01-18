using System;
using System.Linq;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 计时服务实现
    /// </summary>
    public class TimerService : ITimerService
    {
        private readonly IRaceRecordRepository _raceRecordRepository;
        private readonly ILapRecordRepository _lapRecordRepository;
        private RaceRecord? _currentRace;

        public TimerService(
            IRaceRecordRepository raceRecordRepository,
            ILapRecordRepository lapRecordRepository)
        {
            _raceRecordRepository = raceRecordRepository ?? throw new ArgumentNullException(nameof(raceRecordRepository));
            _lapRecordRepository = lapRecordRepository ?? throw new ArgumentNullException(nameof(lapRecordRepository));
        }

        public RaceRecord? CurrentRace => _currentRace;

        public bool IsRunning => _currentRace?.Status == RaceStatus.Running;

        public async Task<RaceRecord> StartRaceAsync(int raceGroupId, int totalLaps)
        {
            // 检查是否有正在进行的比赛
            var activeRace = await _raceRecordRepository.GetActiveRaceAsync();
            if (activeRace != null)
            {
                throw new InvalidOperationException("已有正在进行的比赛，请先停止当前比赛");
            }

            // 创建新的比赛记录
            var raceRecord = new RaceRecord
            {
                RaceGroupId = raceGroupId,
                StartTime = DateTime.Now,
                Status = RaceStatus.Running,
                TotalLaps = totalLaps,
                CreatedAt = DateTime.Now
            };

            var id = await _raceRecordRepository.CreateAsync(raceRecord);
            raceRecord.Id = id;
            _currentRace = raceRecord;

            return raceRecord;
        }

        public async Task PauseRaceAsync()
        {
            if (_currentRace == null)
            {
                throw new InvalidOperationException("没有正在进行的比赛");
            }

            if (_currentRace.Status != RaceStatus.Running)
            {
                throw new InvalidOperationException("比赛未在运行中");
            }

            _currentRace.Status = RaceStatus.Paused;
            await _raceRecordRepository.UpdateAsync(_currentRace);
        }

        public async Task ResumeRaceAsync()
        {
            if (_currentRace == null)
            {
                throw new InvalidOperationException("没有正在进行的比赛");
            }

            if (_currentRace.Status != RaceStatus.Paused)
            {
                throw new InvalidOperationException("比赛未处于暂停状态");
            }

            _currentRace.Status = RaceStatus.Running;
            await _raceRecordRepository.UpdateAsync(_currentRace);
        }

        public async Task StopRaceAsync()
        {
            if (_currentRace == null)
            {
                throw new InvalidOperationException("没有正在进行的比赛");
            }

            _currentRace.Status = RaceStatus.Stopped;
            _currentRace.EndTime = DateTime.Now;
            await _raceRecordRepository.UpdateAsync(_currentRace);
            _currentRace = null;
        }

        public async Task CompleteRaceAsync()
        {
            if (_currentRace == null)
            {
                throw new InvalidOperationException("没有正在进行的比赛");
            }

            _currentRace.Status = RaceStatus.Completed;
            _currentRace.EndTime = DateTime.Now;
            await _raceRecordRepository.UpdateAsync(_currentRace);
            _currentRace = null;
        }

        public async Task<LapRecord> RecordLapAsync(int participantId, DateTime passTime)
        {
            if (_currentRace == null)
            {
                throw new InvalidOperationException("没有正在进行的比赛");
            }

            if (_currentRace.Status != RaceStatus.Running)
            {
                throw new InvalidOperationException("比赛未在运行中，无法记录圈次");
            }

            // 获取该参赛者的上一圈记录
            var previousLap = await _lapRecordRepository.GetLatestLapAsync(_currentRace.Id, participantId);
            
            int lapNumber = (previousLap?.LapNumber ?? 0) + 1;
            long lapTime;
            long totalTime;

            if (previousLap == null)
            {
                // 第一圈：从比赛开始时间计算
                var elapsed = passTime - _currentRace.StartTime;
                lapTime = (long)elapsed.TotalMilliseconds;
                totalTime = lapTime;
            }
            else
            {
                // 后续圈：从上一圈通过时间计算
                var elapsed = passTime - previousLap.PassTime;
                lapTime = (long)elapsed.TotalMilliseconds;
                totalTime = previousLap.TotalTime + lapTime;
            }

            // 创建圈次记录
            var lapRecord = new LapRecord
            {
                RaceRecordId = _currentRace.Id,
                ParticipantId = participantId,
                LapNumber = lapNumber,
                PassTime = passTime,
                LapTime = lapTime,
                TotalTime = totalTime,
                CreatedAt = DateTime.Now
            };

            // 计算排名
            var rank = await CalculateRankAsync(participantId, totalTime, lapNumber);
            lapRecord.Rank = rank;

            // 保存到数据库
            var id = await _lapRecordRepository.CreateAsync(lapRecord);
            lapRecord.Id = id;

            return lapRecord;
        }

        public async Task<int> GetParticipantCurrentLapAsync(int participantId)
        {
            if (_currentRace == null)
            {
                return 0;
            }

            var latestLap = await _lapRecordRepository.GetLatestLapAsync(_currentRace.Id, participantId);
            return latestLap?.LapNumber ?? 0;
        }

        public async Task<long> GetParticipantTotalTimeAsync(int participantId)
        {
            if (_currentRace == null)
            {
                return 0;
            }

            var latestLap = await _lapRecordRepository.GetLatestLapAsync(_currentRace.Id, participantId);
            return latestLap?.TotalTime ?? 0;
        }

        public async Task<int> CalculateRankAsync(int participantId)
        {
            if (_currentRace == null)
            {
                return 0;
            }

            var totalTime = await GetParticipantTotalTimeAsync(participantId);
            var currentLap = await GetParticipantCurrentLapAsync(participantId);
            
            return await CalculateRankAsync(participantId, totalTime, currentLap);
        }

        /// <summary>
        /// 计算排名（内部方法，考虑圈数和用时）
        /// </summary>
        private async Task<int> CalculateRankAsync(int participantId, long totalTime, int currentLap)
        {
            if (_currentRace == null)
            {
                return 0;
            }

            // 获取所有参赛者的最新圈次记录
            var allLaps = await _lapRecordRepository.GetByRaceRecordIdAsync(_currentRace.Id);
            
            // 按参赛者分组，取每个参赛者的最新记录
            var latestLaps = allLaps
                .GroupBy(l => l.ParticipantId)
                .Select(g => g.OrderByDescending(l => l.LapNumber).First())
                .ToList();

            // 排名规则：
            // 1. 圈数多的排前面
            // 2. 圈数相同时，用时少的排前面
            var rank = latestLaps
                .Where(l => l.LapNumber > currentLap || 
                           (l.LapNumber == currentLap && l.TotalTime < totalTime))
                .Count() + 1;

            return rank;
        }

        public async Task<RaceRecord?> LoadActiveRaceAsync()
        {
            _currentRace = await _raceRecordRepository.GetActiveRaceAsync();
            return _currentRace;
        }
    }
}

