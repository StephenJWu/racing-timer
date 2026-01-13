# API Contracts: 参赛人员管理

**Feature**: 参赛人员管理  
**Date**: 2025-01-27  
**Phase**: Phase 1 - Design & Contracts

## Service Interfaces

### IParticipantRepository

数据访问接口，定义参赛人员数据的CRUD操作。

**Methods**:

#### GetAllAsync
```csharp
Task<IEnumerable<Participant>> GetAllAsync(SearchFilter filter);
```
获取参赛人员列表，支持搜索和分页。

**Parameters**:
- `filter` (SearchFilter): 搜索筛选条件，包含搜索关键词、筛选条件、分页参数

**Returns**: 
- `Task<IEnumerable<Participant>>`: 人员列表

**Errors**:
- `DatabaseException`: 数据库连接失败或查询错误

---

#### GetByIdAsync
```csharp
Task<Participant?> GetByIdAsync(int id);
```
根据ID获取单个参赛人员。

**Parameters**:
- `id` (int): 人员ID

**Returns**:
- `Task<Participant?>`: 人员对象，如果不存在则返回null

**Errors**:
- `DatabaseException`: 数据库连接失败或查询错误

---

#### GetByBibNumberAsync
```csharp
Task<Participant?> GetByBibNumberAsync(string bibNumber);
```
根据号码布编号获取参赛人员。

**Parameters**:
- `bibNumber` (string): 号码布编号

**Returns**:
- `Task<Participant?>`: 人员对象，如果不存在则返回null

**Errors**:
- `ArgumentNullException`: bibNumber为null或空
- `DatabaseException`: 数据库连接失败或查询错误

---

#### GetTotalCountAsync
```csharp
Task<int> GetTotalCountAsync(SearchFilter filter);
```
获取符合条件的总记录数（用于分页计算）。

**Parameters**:
- `filter` (SearchFilter): 搜索筛选条件（分页参数忽略）

**Returns**:
- `Task<int>`: 总记录数

**Errors**:
- `DatabaseException`: 数据库连接失败或查询错误

---

#### AddAsync
```csharp
Task<int> AddAsync(Participant participant);
```
添加新的参赛人员。

**Parameters**:
- `participant` (Participant): 人员对象

**Returns**:
- `Task<int>`: 新创建的人员ID

**Errors**:
- `ArgumentNullException`: participant为null
- `DuplicateKeyException`: 身份证号或号码布已存在
- `DatabaseException`: 数据库连接失败或插入错误

---

#### UpdateAsync
```csharp
Task UpdateAsync(Participant participant);
```
更新参赛人员信息。

**Parameters**:
- `participant` (Participant): 人员对象（必须包含有效的Id）

**Returns**:
- `Task`: 无返回值

**Errors**:
- `ArgumentNullException`: participant为null
- `ArgumentException`: participant.Id无效
- `DuplicateKeyException`: 更新后的身份证号或号码布与其他人员重复
- `DatabaseException`: 数据库连接失败或更新错误

---

#### DeleteAsync
```csharp
Task DeleteAsync(int id);
```
删除参赛人员。

**Parameters**:
- `id` (int): 人员ID

**Returns**:
- `Task`: 无返回值

**Errors**:
- `ArgumentException`: id无效
- `DatabaseException`: 数据库连接失败或删除错误

---

#### DeleteBatchAsync
```csharp
Task DeleteBatchAsync(IEnumerable<int> ids);
```
批量删除参赛人员。

**Parameters**:
- `ids` (IEnumerable<int>): 人员ID列表

**Returns**:
- `Task`: 无返回值

**Errors**:
- `ArgumentNullException`: ids为null
- `DatabaseException`: 数据库连接失败或删除错误

---

#### ExistsByIdNumberAsync
```csharp
Task<bool> ExistsByIdNumberAsync(string idNumber);
```
检查身份证号是否已存在。

**Parameters**:
- `idNumber` (string): 身份证号

**Returns**:
- `Task<bool>`: 如果存在返回true，否则返回false

**Errors**:
- `ArgumentNullException`: idNumber为null或空
- `DatabaseException`: 数据库连接失败或查询错误

---

#### ExistsByBibNumberAsync
```csharp
Task<bool> ExistsByBibNumberAsync(string bibNumber);
```
检查号码布是否已存在。

**Parameters**:
- `bibNumber` (string): 号码布编号

**Returns**:
- `Task<bool>`: 如果存在返回true，否则返回false

**Errors**:
- `ArgumentNullException`: bibNumber为null或空
- `DatabaseException`: 数据库连接失败或查询错误

---

#### GetMaxSequenceNumberAsync
```csharp
Task<int> GetMaxSequenceNumberAsync();
```
获取当前最大序号。

**Returns**:
- `Task<int>`: 最大序号，如果数据库为空则返回0

**Errors**:
- `DatabaseException`: 数据库连接失败或查询错误

---

#### Transaction Methods

```csharp
Task BeginTransactionAsync();
Task CommitTransactionAsync();
Task RollbackTransactionAsync();
```
事务管理方法，用于批量操作。

**Errors**:
- `DatabaseException`: 数据库连接失败或事务操作错误

---

### IExcelImportService

Excel导入服务接口。

**Methods**:

#### ReadFromFileAsync
```csharp
Task<IEnumerable<Participant>> ReadFromFileAsync(string filePath);
```
从Excel文件读取参赛人员数据。

**Parameters**:
- `filePath` (string): Excel文件路径

**Returns**:
- `Task<IEnumerable<Participant>>`: 解析后的人员列表

**Errors**:
- `ArgumentNullException`: filePath为null或空
- `FileNotFoundException`: 文件不存在
- `InvalidFileFormatException`: 文件格式不支持（不是.xls或.xlsx）
- `ExcelParseException`: Excel解析错误（格式不正确、缺少必需列等）

---

#### ImportAsync
```csharp
Task<ImportResult> ImportAsync(
    IEnumerable<Participant> participants, 
    IProgress<double>? progress = null
);
```
将参赛人员数据导入到数据库。

**Parameters**:
- `participants` (IEnumerable<Participant>): 要导入的人员列表
- `progress` (IProgress<double>): 进度报告（0-100）

**Returns**:
- `Task<ImportResult>`: 导入结果，包含成功数、失败数和详细错误

**Errors**:
- `ArgumentNullException`: participants为null
- `DatabaseException`: 数据库连接失败或插入错误
- `TransactionException`: 事务操作失败

---

## Data Models

### Participant
见 data-model.md

### ImportResult
见 data-model.md

### SearchFilter
见 data-model.md

## Error Handling

所有服务方法都应该：
1. 抛出明确的异常类型（ArgumentNullException, DatabaseException等）
2. 异常消息应该清晰描述错误原因
3. 数据库异常应该包含原始SQL错误信息（用于调试）
4. 业务逻辑异常（如重复数据）应该提供友好的错误消息

## Testing Requirements

所有接口方法都需要单元测试：
- 正常流程测试
- 边界条件测试
- 错误情况测试（null参数、数据库错误等）
- 并发测试（如适用）

测试覆盖率目标：≥80%

