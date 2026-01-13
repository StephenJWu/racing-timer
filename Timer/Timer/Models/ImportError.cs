using System.Collections.Generic;

namespace Timer.Models
{
    /// <summary>
    /// 表示单条记录的导入错误信息
    /// </summary>
    public class ImportError
    {
        /// <summary>
        /// Excel中的行号（从1开始，包含表头）
        /// </summary>
        public int RowNumber { get; set; }

        /// <summary>
        /// 出错的字段名
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// 错误原因描述
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// 该行的原始数据（可选，用于调试）
        /// </summary>
        public Dictionary<string, object>? RecordData { get; set; }

        /// <summary>
        /// 获取格式化的错误显示信息
        /// </summary>
        /// <returns>格式化的错误信息字符串</returns>
        public override string ToString()
        {
            return $"第{RowNumber}行，字段'{FieldName}'：{ErrorMessage}";
        }
    }
}

