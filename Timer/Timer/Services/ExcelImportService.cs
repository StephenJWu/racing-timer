using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Timer.Converters;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// Excel导入服务实现
    /// </summary>
    public class ExcelImportService : IExcelImportService
    {
        private readonly IParticipantRepository _repository;
        private readonly ILoggingService? _loggingService;

        /// <summary>
        /// 初始化Excel导入服务
        /// </summary>
        /// <param name="repository">数据访问接口</param>
        /// <param name="loggingService">日志服务（可选）</param>
        public ExcelImportService(IParticipantRepository repository, ILoggingService? loggingService = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _loggingService = loggingService;
        }

        /// <summary>
        /// 从Excel文件读取参赛人员数据
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <returns>解析后的人员列表</returns>
        public async Task<IEnumerable<Participant>> ReadFromFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"文件不存在: {filePath}");
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension != ".xls" && extension != ".xlsx")
            {
                throw new InvalidOperationException($"不支持的文件格式: {extension}。仅支持.xls和.xlsx格式");
            }

            var participants = new List<Participant>();

            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    throw new InvalidOperationException("Excel文件中没有工作表");
                }

                // 查找表头行（第一行）
                var headerRow = worksheet.FirstRow();
                if (headerRow == null)
                {
                    throw new InvalidOperationException("Excel文件中没有表头行");
                }

                // 查找列索引
                var columnMap = FindColumnIndices(headerRow);

                // 从第二行开始读取数据
                var dataRows = worksheet.RowsUsed().Skip(1);
                int rowNumber = 2; // 从第二行开始（第一行是表头）

                foreach (var row in dataRows)
                {
                    try
                    {
                        var participant = ParseRow(row, columnMap, rowNumber);
                        if (participant != null)
                        {
                            participants.Add(participant);
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.Warn($"解析第{rowNumber}行时出错: {ex.Message}");
                        // 继续处理下一行
                    }
                    rowNumber++;
                }

                _loggingService?.Info($"成功从Excel文件读取{participants.Count}条记录");
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"读取Excel文件失败: {ex.Message}", ex);
                throw;
            }

            return await Task.FromResult(participants);
        }

        /// <summary>
        /// 将参赛人员数据导入到数据库
        /// </summary>
        /// <param name="participants">要导入的人员列表</param>
        /// <param name="progress">进度报告（0-100）</param>
        /// <returns>导入结果，包含成功数、失败数和详细错误</returns>
        public async Task<ImportResult> ImportAsync(
            IEnumerable<Participant> participants,
            IProgress<double>? progress = null)
        {
            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            var participantList = participants.ToList();
            var result = new ImportResult
            {
                TotalRecords = participantList.Count
            };

            if (participantList.Count == 0)
            {
                return result;
            }

            try
            {
                // 开始事务
                await _repository.BeginTransactionAsync();

                // 获取项目ID（从第一个participant获取，因为导入时所有participant应该属于同一个项目）
                var projectId = participantList.FirstOrDefault()?.ProjectId;

                // 验证序号连续性
                var sequenceValidation = new ParticipantValidator.ValidationResult { IsValid = true };
                ParticipantValidator.ValidateSequenceNumberContinuity(participantList, sequenceValidation);
                if (!sequenceValidation.IsValid)
                {
                    // 序号不连续，添加所有错误
                    foreach (var error in sequenceValidation.Errors)
                    {
                        result.AddError(0, "序号", error);
                    }
                    await _repository.RollbackTransactionAsync();
                    return result;
                }

                // 获取该项目下的最大序号
                var maxSequence = await _repository.GetMaxSequenceNumberAsync(projectId);
                var expectedSequence = maxSequence + 1;

                // 验证序号是否从正确的位置开始
                if (participantList.Count > 0 && participantList[0].SequenceNumber != expectedSequence)
                {
                    result.AddError(0, "序号", $"序号必须从{expectedSequence}开始，但第一个序号是{participantList[0].SequenceNumber}");
                    await _repository.RollbackTransactionAsync();
                    return result;
                }

                // 逐条验证和导入
                for (int i = 0; i < participantList.Count; i++)
                {
                    var participant = participantList[i];
                    var validationResult = await ParticipantValidator.ValidateAsync(participant, _repository);

                    if (validationResult.IsValid)
                    {
                        try
                        {
                            participant.CreatedAt = DateTime.Now;
                            participant.UpdatedAt = DateTime.Now;
                            await _repository.AddAsync(participant);
                            result.SuccessCount++;
                        }
                        catch (Exception ex)
                        {
                            _loggingService?.Error($"导入第{i + 2}行数据失败: {ex.Message}", ex);
                            result.AddError(i + 2, "数据库", $"插入失败: {ex.Message}");
                        }
                    }
                    else
                    {
                        // 添加所有验证错误
                        foreach (var error in validationResult.Errors)
                        {
                            result.AddError(i + 2, "验证", error);
                        }
                    }

                    // 报告进度
                    var progressValue = (double)(i + 1) / participantList.Count * 100;
                    progress?.Report(progressValue);
                }

                // 如果全部成功，提交事务；否则回滚
                if (result.IsSuccess())
                {
                    await _repository.CommitTransactionAsync();
                    _loggingService?.Info($"成功导入{result.SuccessCount}条记录");
                }
                else
                {
                    await _repository.RollbackTransactionAsync();
                    _loggingService?.Warn($"导入完成，成功{result.SuccessCount}条，失败{result.FailureCount}条");
                }
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                _loggingService?.Error($"导入过程中发生错误: {ex.Message}", ex);
                throw;
            }

            return result;
        }

        /// <summary>
        /// 查找列索引
        /// </summary>
        private Dictionary<string, int> FindColumnIndices(IXLRow headerRow)
        {
            var columnMap = new Dictionary<string, int>();

            foreach (var cell in headerRow.CellsUsed())
            {
                var headerText = cell.GetString().Trim();
                var columnIndex = cell.Address.ColumnNumber;

                // 映射Excel列名到字段名
                switch (headerText)
                {
                    case "序号":
                        columnMap["SequenceNumber"] = columnIndex;
                        break;
                    case "日期":
                        columnMap["Date"] = columnIndex;
                        break;
                    case "学校":
                        columnMap["School"] = columnIndex;
                        break;
                    case "年级":
                        columnMap["Grade"] = columnIndex;
                        break;
                    case "班级":
                        columnMap["Class"] = columnIndex;
                        break;
                    case "姓名":
                        columnMap["Name"] = columnIndex;
                        break;
                    case "性别":
                        columnMap["Gender"] = columnIndex;
                        break;
                    case "准考证号":
                        columnMap["ExamNumber"] = columnIndex;
                        break;
                    case "组别名称":
                        columnMap["GroupName"] = columnIndex;
                        break;
                }
            }

            // 验证必需列是否存在
            var requiredColumns = new[] { "SequenceNumber", "Date", "Name", "Gender" };
            foreach (var requiredColumn in requiredColumns)
            {
                if (!columnMap.ContainsKey(requiredColumn))
                {
                    throw new InvalidOperationException($"Excel文件中缺少必需的列: {GetColumnName(requiredColumn)}");
                }
            }

            return columnMap;
        }

        /// <summary>
        /// 获取列的中文名称
        /// </summary>
        private string GetColumnName(string fieldName)
        {
            return fieldName switch
            {
                "SequenceNumber" => "序号",
                "Date" => "日期",
                "School" => "学校",
                "Grade" => "年级",
                "Class" => "班级",
                "Name" => "姓名",
                "Gender" => "性别",
                "ExamNumber" => "准考证号",
                "GroupName" => "组别名称",
                _ => fieldName
            };
        }

        /// <summary>
        /// 解析数据行
        /// </summary>
        private Participant? ParseRow(IXLRow row, Dictionary<string, int> columnMap, int rowNumber)
        {
            try
            {
                var participant = new Participant();

                // 解析序号
                if (columnMap.TryGetValue("SequenceNumber", out var seqCol))
                {
                    var seqValue = row.Cell(seqCol).GetValue<int>();
                    participant.SequenceNumber = seqValue;
                }

                // 解析日期
                if (columnMap.TryGetValue("Date", out var dateCol))
                {
                    var dateValue = row.Cell(dateCol).GetString().Trim();
                    var parsedDate = DateConverter.ParseDate(dateValue);
                    if (parsedDate.HasValue)
                    {
                        participant.Date = parsedDate.Value;
                    }
                    else
                    {
                        throw new InvalidOperationException($"日期格式无效: {dateValue}");
                    }
                }

                // 解析可选字段
                if (columnMap.TryGetValue("School", out var schoolCol))
                {
                    participant.School = row.Cell(schoolCol).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(participant.School))
                    {
                        participant.School = null;
                    }
                }

                if (columnMap.TryGetValue("Grade", out var gradeCol))
                {
                    participant.Grade = row.Cell(gradeCol).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(participant.Grade))
                    {
                        participant.Grade = null;
                    }
                }

                if (columnMap.TryGetValue("Class", out var classCol))
                {
                    participant.Class = row.Cell(classCol).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(participant.Class))
                    {
                        participant.Class = null;
                    }
                }

                // 解析姓名
                if (columnMap.TryGetValue("Name", out var nameCol))
                {
                    participant.Name = row.Cell(nameCol).GetString().Trim();
                }

                // 解析性别
                if (columnMap.TryGetValue("Gender", out var genderCol))
                {
                    participant.Gender = row.Cell(genderCol).GetString().Trim();
                }

                // 解析准考证号
                if (columnMap.TryGetValue("ExamNumber", out var examCol))
                {
                    participant.ExamNumber = row.Cell(examCol).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(participant.ExamNumber))
                    {
                        participant.ExamNumber = null;
                    }
                }

                // 解析组别名称
                if (columnMap.TryGetValue("GroupName", out var groupCol))
                {
                    participant.GroupName = row.Cell(groupCol).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(participant.GroupName))
                    {
                        participant.GroupName = null;
                    }
                }

                return participant;
            }
            catch (Exception ex)
            {
                _loggingService?.Warn($"解析第{rowNumber}行时出错: {ex.Message}");
                throw;
            }
        }
    }
}

