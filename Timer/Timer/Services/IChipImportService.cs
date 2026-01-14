using System.Collections.Generic;
using System.Threading.Tasks;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 芯片Excel导入服务接口
    /// </summary>
    public interface IChipImportService
    {
        /// <summary>
        /// 从Excel文件读取芯片数据
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <returns>解析后的芯片数据列表（包含组名信息）</returns>
        Task<IEnumerable<ChipImportData>> ReadFromFileAsync(string filePath);

        /// <summary>
        /// 将芯片数据导入到数据库
        /// </summary>
        /// <param name="chipData">要导入的芯片数据列表</param>
        /// <param name="progress">进度报告（0-100）</param>
        /// <returns>导入结果，包含成功数、失败数和详细错误</returns>
        Task<ImportResult> ImportAsync(
            IEnumerable<ChipImportData> chipData,
            IProgress<double>? progress = null
        );
    }
}

