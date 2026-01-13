# Data Model: 参赛人员管理

**Feature**: 参赛人员管理  
**Date**: 2025-01-27  
**Phase**: Phase 1 - Design & Contracts

## Entities

### Participant

**Purpose**: 表示一个参赛人员，包含人员的基本信息和比赛相关信息

**Properties**:
- `Id` (int, Primary Key, Auto Increment): 数据库主键
- `SequenceNumber` (int, Required, Unique): 序号，必须从1开始连续
- `Name` (string, Required): 姓名
- `Gender` (string, Required): 性别（"男"或"女"）
- `DateOfBirth` (DateTime, Required): 出生日期
- `IdNumber` (string, Required, Unique): 身份证号，唯一标识
- `BibNumber` (string, Required, Unique): 号码布编号，唯一标识
- `Group` (string, Optional): 组别
- `ChipNumber` (string, Optional): 芯片编号（可选，后续分配）
- `CreatedAt` (DateTime): 创建时间
- `UpdatedAt` (DateTime): 更新时间

**Database Schema**:
```sql
CREATE TABLE Participants (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SequenceNumber INTEGER NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    Gender TEXT NOT NULL CHECK(Gender IN ('男', '女')),
    DateOfBirth TEXT NOT NULL,  -- SQLite存储为TEXT，格式YYYY-MM-DD
    IdNumber TEXT NOT NULL UNIQUE,
    BibNumber TEXT NOT NULL UNIQUE,
    Group TEXT,
    ChipNumber TEXT,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_participants_name ON Participants(Name);
CREATE INDEX idx_participants_bib ON Participants(BibNumber);
CREATE INDEX idx_participants_group ON Participants(Group);
```

**Validation Rules**:
- SequenceNumber: 必须从1开始，连续无间隔
- Name: 不能为空，长度1-50字符
- Gender: 必须是"男"或"女"
- DateOfBirth: 必须符合三种格式之一：YYYY-MM-DD、YYYY-MM-DD上午、YYYY-MM-DD下午
- IdNumber: 不能为空，唯一性约束
- BibNumber: 不能为空，唯一性约束
- Group: 可选，如果提供则长度1-50字符

**State Transitions**:
- 初始状态: 创建时设置CreatedAt和UpdatedAt为当前时间
- 更新状态: 更新时修改UpdatedAt为当前时间
- 删除状态: 软删除（可选）或硬删除

**Relationships**:
- 可以关联到Group（人员分组，通过Group字段）
- 可以关联到Chip（芯片设备，通过ChipNumber字段）
- 可以关联到RaceResult（比赛成绩，通过Id外键，后续功能）

### ImportResult

**Purpose**: 表示Excel导入操作的结果，包含成功和失败的详细信息

**Properties**:
- `TotalRecords` (int): 总记录数
- `SuccessCount` (int): 成功导入的记录数
- `FailureCount` (int): 失败的记录数
- `Errors` (List<ImportError>): 详细的错误信息列表

**Methods**:
- `AddError(rowNumber, fieldName, errorMessage)`: 添加错误信息
- `IsSuccess()`: 判断是否全部成功

**Validation Rules**:
- TotalRecords = SuccessCount + FailureCount
- Errors列表长度 = FailureCount

### ImportError

**Purpose**: 表示单条记录的导入错误信息

**Properties**:
- `RowNumber` (int): Excel中的行号（从1开始，包含表头）
- `FieldName` (string): 出错的字段名
- `ErrorMessage` (string): 错误原因描述
- `RecordData` (Dictionary<string, object>): 该行的原始数据（可选，用于调试）

**Display Format**:
"第{RowNumber}行，字段'{FieldName}'：{ErrorMessage}"

### SearchFilter

**Purpose**: 表示搜索和筛选条件

**Properties**:
- `SearchKeyword` (string, Optional): 搜索关键词（匹配姓名、号码布、身份证号）
- `Group` (string, Optional): 组别筛选
- `Gender` (string, Optional): 性别筛选
- `PageNumber` (int, Default: 1): 页码（从1开始）
- `PageSize` (int, Default: 20): 每页记录数

**Validation Rules**:
- PageNumber >= 1
- PageSize >= 1 且 <= 100

## View Models

### ParticipantViewModel

**Purpose**: 参赛人员管理页面的ViewModel

**Properties**:
- `Participants` (ObservableCollection<Participant>): 当前页的人员列表
- `SelectedParticipant` (Participant): 当前选中的人员
- `SearchFilter` (SearchFilter): 搜索筛选条件
- `IsLoading` (bool): 是否正在加载
- `ImportProgress` (double): 导入进度（0-100）
- `ImportResult` (ImportResult): 导入结果
- `TotalCount` (int): 总记录数
- `CurrentPage` (int): 当前页码
- `TotalPages` (int): 总页数

**Commands**:
- `ImportCommand` (AsyncRelayCommand): 导入Excel文件
- `EditCommand` (RelayCommand<Participant>): 编辑人员
- `DeleteCommand` (RelayCommand<Participant>): 删除人员
- `BatchDeleteCommand` (RelayCommand<IEnumerable<Participant>>): 批量删除
- `SearchCommand` (RelayCommand): 执行搜索
- `ClearSearchCommand` (RelayCommand): 清除搜索
- `PreviousPageCommand` (RelayCommand): 上一页
- `NextPageCommand` (RelayCommand): 下一页
- `GoToPageCommand` (RelayCommand<int>): 跳转到指定页

**Methods**:
- `LoadParticipantsAsync()`: 异步加载人员列表
- `ValidateParticipant(Participant)`: 校验人员数据
- `RefreshList()`: 刷新列表

## Services

### IParticipantRepository

**Purpose**: 参赛人员数据访问接口

**Methods**:
- `Task<IEnumerable<Participant>> GetAllAsync(SearchFilter filter)`: 获取所有人员（支持搜索和分页）
- `Task<Participant?> GetByIdAsync(int id)`: 根据ID获取人员
- `Task<Participant?> GetByBibNumberAsync(string bibNumber)`: 根据号码布获取人员
- `Task<int> GetTotalCountAsync(SearchFilter filter)`: 获取总记录数（用于分页）
- `Task<int> AddAsync(Participant participant)`: 添加人员，返回新ID
- `Task UpdateAsync(Participant participant)`: 更新人员
- `Task DeleteAsync(int id)`: 删除人员
- `Task DeleteBatchAsync(IEnumerable<int> ids)`: 批量删除
- `Task<bool> ExistsByIdNumberAsync(string idNumber)`: 检查身份证号是否存在
- `Task<bool> ExistsByBibNumberAsync(string bibNumber)`: 检查号码布是否存在
- `Task<int> GetMaxSequenceNumberAsync()`: 获取最大序号
- `Task BeginTransactionAsync()`: 开始事务
- `Task CommitTransactionAsync()`: 提交事务
- `Task RollbackTransactionAsync()`: 回滚事务

### IExcelImportService

**Purpose**: Excel导入服务接口

**Methods**:
- `Task<IEnumerable<Participant>> ReadFromFileAsync(string filePath)`: 从Excel文件读取数据
- `Task<ImportResult> ImportAsync(IEnumerable<Participant> participants, IProgress<double>? progress = null)`: 导入数据到数据库，返回导入结果

### ParticipantValidator

**Purpose**: 数据校验服务（工具类，使用静态方法）

**Methods**:
- `static ValidationResult Validate(Participant participant)`: 校验单个人员数据
- `static ValidationResult ValidateRequiredFields(Participant participant)`: 校验必填字段
- `static ValidationResult ValidateDateFormat(string dateString)`: 校验日期格式
- `static ValidationResult ValidateSequenceNumber(int sequenceNumber, int maxSequence)`: 校验序号连续性
- `static ValidationResult ValidateUniqueness(Participant participant, IParticipantRepository repository)`: 校验唯一性（异步）

## Data Flow

```
Excel文件导入流程:
1. 用户选择Excel文件
2. ExcelImportService.ReadFromFileAsync() 解析Excel
3. ParticipantValidator.Validate() 校验每条记录
4. 显示导入预览和错误信息
5. 用户确认导入
6. Repository.BeginTransactionAsync() 开始事务
7. Repository.AddAsync() 批量添加（带进度报告）
8. Repository.CommitTransactionAsync() 提交事务
9. 显示导入结果
10. 刷新列表显示

列表显示流程:
1. 用户打开页面或执行搜索
2. ViewModel.LoadParticipantsAsync()
3. Repository.GetAllAsync(SearchFilter) 查询数据
4. Repository.GetTotalCountAsync(SearchFilter) 获取总数
5. 更新ViewModel.Participants和分页信息
6. UI自动更新（数据绑定）

编辑流程:
1. 用户点击编辑按钮
2. 打开编辑对话框，显示当前数据
3. 用户修改数据
4. ParticipantValidator.Validate() 校验
5. Repository.UpdateAsync() 更新数据库
6. 刷新列表显示
```

## Notes

- 数据库使用SQLite，文件位置：`data/timer.db`
- 日期在数据库中存储为TEXT格式（YYYY-MM-DD），便于查询和显示
- 序号连续性校验：导入时检查序号是否从1开始且连续
- 唯一性约束：身份证号和号码布在数据库层面有UNIQUE约束
- 分页查询使用LIMIT和OFFSET，性能优化
- 搜索使用SQL LIKE进行模糊匹配，支持姓名、号码布、身份证号搜索

