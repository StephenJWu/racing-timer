using System.Collections.Generic;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// Excel导入服务接口
    /// </summary>
    public interface IExcelImportService
    {
        /// <summary>
        /// 从Excel文件读取参赛人员数据
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <returns>解析后的人员列表</returns>
        Task<IEnumerable<Participant>> ReadFromFileAsync(string filePath);

        /// <summary>
        /// 将参赛人员数据导入到数据库
        /// </summary>
        /// <param name="participants">要导入的人员列表</param>
        /// <param name="progress">进度报告（0-100）</param>
        /// <returns>导入结果，包含成功数、失败数和详细错误</returns>
        Task<ImportResult> ImportAsync(
            IEnumerable<Participant> participants,
            IProgress<double>? progress = null
        );
    }
}

