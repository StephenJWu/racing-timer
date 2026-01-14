using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Timer.Models;

namespace Timer.Services
{
    /// <summary>
    /// 芯片Excel导入服务实现
    /// </summary>
    public class ChipImportService : IChipImportService
    {
        private readonly IChipRepository _repository;
        private readonly ILoggingService? _loggingService;
        private const string DefaultChipGroupColor = "#FF1890FF"; // 默认蓝色

        /// <summary>
        /// 初始化芯片导入服务
        /// </summary>
        /// <param name="repository">芯片数据访问接口</param>
        /// <param name="loggingService">日志服务（可选）</param>
        public ChipImportService(IChipRepository repository, ILoggingService? loggingService = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _loggingService = loggingService;
        }

        /// <summary>
        /// 从Excel文件读取芯片数据
        /// </summary>
        public async Task<IEnumerable<ChipImportData>> ReadFromFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("文件路径不能为空", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"文件不存在: {filePath}");
            }

            var chipDataList = new List<ChipImportData>();

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
                        var chipData = ParseRow(row, columnMap, rowNumber);
                        if (chipData != null)
                        {
                            chipDataList.Add(chipData);
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService?.Warn($"解析第{rowNumber}行时出错: {ex.Message}");
                        // 继续处理下一行
                    }
                    rowNumber++;
                }

                _loggingService?.Info($"成功从Excel文件读取{chipDataList.Count}条记录");
            }
            catch (Exception ex)
            {
                _loggingService?.Error($"读取Excel文件失败: {ex.Message}", ex);
                throw;
            }

            return await Task.FromResult(chipDataList);
        }

        /// <summary>
        /// 将芯片数据导入到数据库
        /// </summary>
        public async Task<ImportResult> ImportAsync(
            IEnumerable<ChipImportData> chipData,
            IProgress<double>? progress = null)
        {
            if (chipData == null)
            {
                throw new ArgumentNullException(nameof(chipData));
            }

            var chipDataList = chipData.ToList();
            var result = new ImportResult
            {
                TotalRecords = chipDataList.Count
            };

            if (chipDataList.Count == 0)
            {
                return result;
            }

            try
            {
                // 开始事务
                await _repository.BeginTransactionAsync();

                // 获取所有现有芯片组，建立字典以便快速查找
                var existingGroups = (await _repository.GetAllChipGroupsAsync()).ToList();
                var groupNameToIdMap = existingGroups.ToDictionary(g => g.GroupName, g => g.Id, StringComparer.OrdinalIgnoreCase);
                var createdGroupsMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // 本次导入中创建的组
                var seenLabelNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // 用于检查LabelNumber唯一性

                // 按组名分组处理
                var groupedData = chipDataList.GroupBy(c => c.GroupName, StringComparer.OrdinalIgnoreCase);

                int processedCount = 0;
                foreach (var group in groupedData)
                {
                    var groupName = group.Key;
                    var chipsInGroup = group.ToList();

                    // 确定芯片组ID（如果已存在则使用，否则创建新组）
                    int chipGroupId;
                    if (groupNameToIdMap.TryGetValue(groupName, out var existingId))
                    {
                        // 使用已有芯片组
                        chipGroupId = existingId;
                        _loggingService?.Info($"使用已有芯片组: {groupName} (ID: {chipGroupId})");
                    }
                    else if (createdGroupsMap.TryGetValue(groupName, out var createdId))
                    {
                        // 使用本次导入中已创建的组
                        chipGroupId = createdId;
                    }
                    else
                    {
                        // 创建新芯片组
                        var newGroup = new ChipGroup
                        {
                            GroupName = groupName,
                            Color = DefaultChipGroupColor,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        chipGroupId = await _repository.AddChipGroupAsync(newGroup);
                        createdGroupsMap[groupName] = chipGroupId;
                        groupNameToIdMap[groupName] = chipGroupId;
                        _loggingService?.Info($"创建新芯片组: {groupName} (ID: {chipGroupId})");
                    }

                    // 验证和导入该组的芯片
                    var validChips = new List<Chip>();
                    foreach (var item in chipsInGroup)
                    {
                        processedCount++;
                        var rowNumber = item.SequenceNumber + 1; // Excel行号 = 序号 + 1（因为有表头行）

                        // 验证数据
                        if (string.IsNullOrWhiteSpace(item.LabelNumber))
                        {
                            result.AddError(rowNumber, "芯片标签号码", "芯片标签号码不能为空");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(item.InternalNumber))
                        {
                            result.AddError(rowNumber, "芯片内部编号", "芯片内部编号不能为空");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(item.GroupName))
                        {
                            result.AddError(rowNumber, "组号", "组号不能为空");
                            continue;
                        }

                        // 检查LabelNumber是否在本次导入中重复
                        if (seenLabelNumbers.Contains(item.LabelNumber))
                        {
                            result.AddError(rowNumber, "芯片标签号码", $"芯片标签号码'{item.LabelNumber}'在导入数据中重复");
                            continue;
                        }

                        // 检查LabelNumber是否在数据库中已存在
                        if (await _repository.ExistsByLabelNumberAsync(item.LabelNumber))
                        {
                            result.AddError(rowNumber, "芯片标签号码", $"芯片标签号码'{item.LabelNumber}'已存在于数据库中");
                            continue;
                        }

                        // 数据有效，添加到待插入列表
                        validChips.Add(new Chip
                        {
                            ChipGroupId = chipGroupId,
                            LabelNumber = item.LabelNumber.Trim(),
                            InternalNumber = item.InternalNumber.Trim(),
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                        seenLabelNumbers.Add(item.LabelNumber);
                    }

                    // 批量插入该组的有效芯片
                    if (validChips.Count > 0)
                    {
                        try
                        {
                            await _repository.AddChipsAsync(validChips);
                            result.SuccessCount += validChips.Count;
                            _loggingService?.Info($"成功导入{validChips.Count}个芯片到组'{groupName}'");
                        }
                        catch (Exception ex)
                        {
                            _loggingService?.Error($"导入组'{groupName}'的芯片时失败: {ex.Message}", ex);
                            foreach (var chip in validChips)
                            {
                                result.AddError(chipDataList.FindIndex(c => c.LabelNumber == chip.LabelNumber) + 2, "数据库", $"插入失败: {ex.Message}");
                            }
                        }
                    }

                    // 报告进度
                    var progressValue = (double)processedCount / chipDataList.Count * 100;
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
                    case "芯片标签号码":
                        columnMap["LabelNumber"] = columnIndex;
                        break;
                    case "芯片内部编号":
                        columnMap["InternalNumber"] = columnIndex;
                        break;
                    case "组号":
                        columnMap["GroupName"] = columnIndex;
                        break;
                }
            }

            // 验证必需列是否存在
            var requiredColumns = new[] { "SequenceNumber", "LabelNumber", "InternalNumber", "GroupName" };
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
                "LabelNumber" => "芯片标签号码",
                "InternalNumber" => "芯片内部编号",
                "GroupName" => "组号",
                _ => fieldName
            };
        }

        /// <summary>
        /// 解析数据行
        /// </summary>
        private ChipImportData? ParseRow(IXLRow row, Dictionary<string, int> columnMap, int rowNumber)
        {
            try
            {
                var chipData = new ChipImportData();

                // 解析序号
                if (columnMap.TryGetValue("SequenceNumber", out var seqCol))
                {
                    var seqValue = row.Cell(seqCol).GetValue<int>();
                    chipData.SequenceNumber = seqValue;
                }

                // 解析芯片标签号码
                if (columnMap.TryGetValue("LabelNumber", out var labelCol))
                {
                    chipData.LabelNumber = row.Cell(labelCol).GetString().Trim();
                }

                // 解析芯片内部编号
                if (columnMap.TryGetValue("InternalNumber", out var internalCol))
                {
                    chipData.InternalNumber = row.Cell(internalCol).GetString().Trim();
                }

                // 解析组号
                if (columnMap.TryGetValue("GroupName", out var groupCol))
                {
                    chipData.GroupName = row.Cell(groupCol).GetString().Trim();
                }

                return chipData;
            }
            catch (Exception ex)
            {
                _loggingService?.Warn($"解析第{rowNumber}行时出错: {ex.Message}");
                throw;
            }
        }
    }
}

