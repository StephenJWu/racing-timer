using System.Collections.Generic;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 圈次记录数据访问接口
    /// </summary>
    public interface ILapRecordRepository
    {
        /// <summary>
        /// 根据ID获取圈次记录
        /// </summary>
        Task<LapRecord?> GetByIdAsync(int id);

        /// <summary>
        /// 根据比赛记录ID获取所有圈次记录
        /// </summary>
        Task<List<LapRecord>> GetByRaceRecordIdAsync(int raceRecordId);

        /// <summary>
        /// 获取指定参赛者在指定比赛中的所有圈次记录
        /// </summary>
        Task<List<LapRecord>> GetParticipantLapsAsync(int raceRecordId, int participantId);

        /// <summary>
        /// 获取指定参赛者在指定比赛中的最新圈次记录
        /// </summary>
        Task<LapRecord?> GetLatestLapAsync(int raceRecordId, int participantId);

        /// <summary>
        /// 获取指定参赛者在指定比赛中的最佳单圈时间
        /// </summary>
        Task<long?> GetParticipantBestTimeAsync(int raceRecordId, int participantId);

        /// <summary>
        /// 创建圈次记录
        /// </summary>
        Task<int> CreateAsync(LapRecord lapRecord);

        /// <summary>
        /// 批量创建圈次记录
        /// </summary>
        Task<int> CreateBatchAsync(List<LapRecord> lapRecords);

        /// <summary>
        /// 更新圈次记录
        /// </summary>
        Task<bool> UpdateAsync(LapRecord lapRecord);

        /// <summary>
        /// 删除圈次记录
        /// </summary>
        Task<bool> DeleteAsync(int id);
    }
}

