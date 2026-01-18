using System;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 计时服务接口，提供核心计时业务逻辑
    /// </summary>
    public interface ITimerService
    {
        /// <summary>
        /// 当前比赛记录
        /// </summary>
        RaceRecord? CurrentRace { get; }

        /// <summary>
        /// 比赛是否正在进行
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// 开始新的比赛
        /// </summary>
        /// <param name="raceGroupId">比赛分组ID</param>
        /// <param name="totalLaps">总圈数</param>
        Task<RaceRecord> StartRaceAsync(int raceGroupId, int totalLaps);

        /// <summary>
        /// 暂停比赛
        /// </summary>
        Task PauseRaceAsync();

        /// <summary>
        /// 继续比赛
        /// </summary>
        Task ResumeRaceAsync();

        /// <summary>
        /// 停止比赛
        /// </summary>
        Task StopRaceAsync();

        /// <summary>
        /// 完成比赛（所有选手完成）
        /// </summary>
        Task CompleteRaceAsync();

        /// <summary>
        /// 记录参赛者完成一圈
        /// </summary>
        /// <param name="participantId">参赛者ID</param>
        /// <param name="passTime">通过时间</param>
        Task<LapRecord> RecordLapAsync(int participantId, DateTime passTime);

        /// <summary>
        /// 获取参赛者当前已完成圈数
        /// </summary>
        Task<int> GetParticipantCurrentLapAsync(int participantId);

        /// <summary>
        /// 获取参赛者累计用时（毫秒）
        /// </summary>
        Task<long> GetParticipantTotalTimeAsync(int participantId);

        /// <summary>
        /// 计算当前排名（基于累计用时）
        /// </summary>
        Task<int> CalculateRankAsync(int participantId);

        /// <summary>
        /// 加载现有的活跃比赛（恢复现场）
        /// </summary>
        Task<RaceRecord?> LoadActiveRaceAsync();
    }
}

