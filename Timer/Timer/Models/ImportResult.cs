using System.Collections.Generic;
using System.Linq;

namespace Timer.Models
{
    /// <summary>
    /// 表示Excel导入操作的结果，包含成功和失败的详细信息
    /// </summary>
    public class ImportResult
    {
        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// 成功导入的记录数
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败的记录数
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// 详细的错误信息列表
        /// </summary>
        public List<ImportError> Errors { get; set; } = new List<ImportError>();

        /// <summary>
        /// 添加错误信息
        /// </summary>
        /// <param name="rowNumber">Excel中的行号（从1开始，包含表头）</param>
        /// <param name="fieldName">出错的字段名</param>
        /// <param name="errorMessage">错误原因描述</param>
        public void AddError(int rowNumber, string fieldName, string errorMessage)
        {
            Errors.Add(new ImportError
            {
                RowNumber = rowNumber,
                FieldName = fieldName,
                ErrorMessage = errorMessage
            });
            FailureCount++;
        }

        /// <summary>
        /// 判断是否全部成功
        /// </summary>
        /// <returns>如果全部成功返回true，否则返回false</returns>
        public bool IsSuccess()
        {
            return FailureCount == 0 && TotalRecords == SuccessCount;
        }
    }
}

