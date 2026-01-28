using System.Collections.Generic;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 比赛记录数据访问接口
    /// </summary>
    public interface IRaceRecordRepository
    {
        /// <summary>
        /// 根据ID获取比赛记录
        /// </summary>
        Task<RaceRecord?> GetByIdAsync(int id);

        /// <summary>
        /// 获取所有比赛记录
        /// </summary>
        Task<List<RaceRecord>> GetAllAsync();

        /// <summary>
        /// 根据比赛分组ID获取比赛记录列表（该分组下所有参赛人员的记录）
        /// </summary>
        Task<List<RaceRecord>> GetByRaceGroupIdAsync(int raceGroupId);

        /// <summary>
        /// 批量创建比赛记录
        /// </summary>
        Task<int> CreateBatchAsync(IEnumerable<RaceRecord> raceRecords);

        /// <summary>
        /// 创建比赛记录
        /// </summary>
        Task<int> CreateAsync(RaceRecord raceRecord);

        /// <summary>
        /// 更新比赛记录
        /// </summary>
        Task<bool> UpdateAsync(RaceRecord raceRecord);

        /// <summary>
        /// 删除比赛记录
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 根据比赛分组ID删除所有比赛记录
        /// </summary>
        Task<bool> DeleteByRaceGroupIdAsync(int raceGroupId);

        /// <summary>
        /// 查询成绩结果（从 RaceRecords 表查询）
        /// </summary>
        Task<List<ScoreResult>> SearchScoresAsync(ScoreSearchFilter filter);

        /// <summary>
        /// 获取成绩查询的总记录数
        /// </summary>
        Task<int> GetScoresTotalCountAsync(ScoreSearchFilter filter);

        /// <summary>
        /// 获取成绩查询中所有学校列表
        /// </summary>
        Task<List<string>> GetScoreSchoolsAsync();

        /// <summary>
        /// 根据项目ID获取成绩查询中的学校列表
        /// </summary>
        Task<List<string>> GetScoreSchoolsByProjectAsync(int projectId);

        /// <summary>
        /// 获取指定学校下的年级列表
        /// </summary>
        Task<List<string>> GetScoreGradesAsync(string school);

        /// <summary>
        /// 获取指定学校和年级下的班级列表
        /// </summary>
        Task<List<string>> GetScoreClassesAsync(string school, string grade);

        /// <summary>
        /// 获取指定学校、年级、班级下的组别列表
        /// </summary>
        Task<List<string>> GetScoreGroupNamesAsync(string school, string grade, string className);
    }
}

