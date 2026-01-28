using System.Collections.Generic;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 参赛人员分组配置数据访问接口
    /// </summary>
    public interface IParticipantGroupRepository
    {
        /// <summary>
        /// 根据条件查询参赛人员分组配置
        /// </summary>
        Task<IEnumerable<ParticipantGroup>> QueryAsync(
            int? projectId,
            string? school,
            string? grade = null,
            string? classValue = null,
            string? groupName = null);

        /// <summary>
        /// 根据ID获取单个分组配置
        /// </summary>
        Task<ParticipantGroup?> GetByIdAsync(int id);

        /// <summary>
        /// 根据分组信息获取配置（如果不存在则返回null）
        /// </summary>
        Task<ParticipantGroup?> GetByGroupInfoAsync(
            int? projectId,
            string school,
            string? grade,
            string? classValue,
            string groupName);

        /// <summary>
        /// 创建分组配置
        /// </summary>
        Task<int> CreateAsync(ParticipantGroup participantGroup);

        /// <summary>
        /// 更新分组配置
        /// </summary>
        Task<bool> UpdateAsync(ParticipantGroup participantGroup);

        /// <summary>
        /// 删除分组配置
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 获取或创建分组配置（如果不存在则创建）
        /// </summary>
        Task<ParticipantGroup> GetOrCreateAsync(
            int? projectId,
            string school,
            string? grade,
            string? classValue,
            string groupName);

        /// <summary>
        /// 根据项目ID获取所有不同的学校列表
        /// </summary>
        Task<List<string>> GetDistinctSchoolsByProjectAsync(int? projectId);

        /// <summary>
        /// 获取指定学校下的所有不同年级列表
        /// </summary>
        Task<List<string>> GetDistinctGradesAsync(string? school);

        /// <summary>
        /// 获取指定学校和年级下的所有不同班级列表
        /// </summary>
        Task<List<string>> GetDistinctClassesAsync(string? school, string? grade);

        /// <summary>
        /// 获取指定学校、年级、班级下的所有不同组别列表
        /// </summary>
        Task<List<string>> GetDistinctGroupNamesAsync(string? school, string? grade, string? classValue);
    }
}
