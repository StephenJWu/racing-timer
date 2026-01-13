# Technical Research: 参赛人员管理

**Feature**: 参赛人员管理  
**Date**: 2025-01-27  
**Phase**: Phase 0 - Research

## Research Tasks

### 1. SQLite数据库访问库选择

**Decision**: 使用 Microsoft.Data.Sqlite

**Rationale**:
- .NET官方推荐的SQLite库，与.NET生态系统集成良好
- 支持异步操作（async/await）
- 轻量级，无需额外依赖
- 支持Entity Framework Core（如需要）
- 跨平台支持

**Alternatives considered**:
- System.Data.SQLite: 功能更丰富，但需要额外安装，且对.NET 10.0支持可能不如Microsoft.Data.Sqlite
- Dapper: 轻量级ORM，但本项目数据模型简单，直接使用ADO.NET即可

**Implementation Notes**:
- 使用连接字符串：`Data Source=data/timer.db`
- 使用using语句确保连接及时释放
- 支持事务操作

### 2. Excel文件读写库选择

**Decision**: 使用 EPPlus (商业许可) 或 ClosedXML (开源)

**Rationale**:
- EPPlus: 功能强大，性能好，支持.xlsx格式，但需要商业许可（.NET 5+）
- ClosedXML: 开源免费，支持.xlsx格式，API友好，但性能略低于EPPlus
- 两者都支持.xlsx格式，对于.xls格式需要额外处理（使用NPOI或转换为.xlsx）

**Alternatives considered**:
- NPOI: 支持.xls和.xlsx，但API较复杂，文档较少
- ExcelDataReader: 主要用于读取，写入功能较弱
- OpenXML SDK: 底层API，使用复杂

**Implementation Notes**:
- 优先使用ClosedXML（开源免费）
- 如果遇到性能问题，可考虑EPPlus
- .xls格式处理：使用NPOI或提示用户转换为.xlsx

### 3. 数据访问模式选择

**Decision**: Repository模式 + 接口抽象

**Rationale**:
- 符合依赖倒置原则，便于单元测试
- 可以轻松替换数据访问实现（如从SQLite迁移到其他数据库）
- 业务逻辑层不直接依赖数据访问实现
- 符合宪法要求的"显式依赖"原则

**Alternatives considered**:
- 直接在ViewModel中使用SQLite连接：违反分层架构，难以测试
- Entity Framework Core: 对于简单CRUD操作可能过于复杂，增加学习成本

**Implementation Notes**:
- 定义IParticipantRepository接口
- 实现ParticipantRepository类
- 通过构造函数注入到ViewModel

### 4. 数据校验策略

**Decision**: 独立的Validator服务类

**Rationale**:
- 单一职责：校验逻辑独立于业务逻辑和数据访问
- 可测试性：可以单独测试校验规则
- 可复用：校验逻辑可以在导入、编辑等多个场景复用
- 符合宪法要求的"数据校验严格"原则

**Implementation Notes**:
- 创建ParticipantValidator类
- 实现必填字段校验、格式校验、重复性校验
- 返回详细的校验错误信息（字段名、错误原因）

### 5. 日期格式处理

**Decision**: 自定义日期解析器，支持三种格式

**Rationale**:
- 符合宪法要求：支持YYYY-MM-DD、YYYY-MM-DD上午、YYYY-MM-DD下午三种格式
- 需要处理中文"上午"、"下午"标识
- 需要将"上午"转换为00:00:00，"下午"转换为12:00:00

**Implementation Notes**:
- 创建DateConverter或DateParser工具类
- 使用DateTime.TryParseExact解析标准格式
- 处理"上午"、"下午"后缀，转换为具体时间

### 6. 分页实现策略

**Decision**: 数据库层面分页（LIMIT/OFFSET）

**Rationale**:
- 性能最优：只查询需要的数据，不加载全部数据到内存
- 支持大数据集：可以处理1000+记录而不影响性能
- SQLite支持LIMIT和OFFSET语法

**Implementation Notes**:
- Repository方法接受pageNumber和pageSize参数
- 使用SQL LIMIT和OFFSET实现分页
- 同时返回总记录数，用于计算总页数

### 7. 搜索和筛选实现

**Decision**: 数据库层面过滤（WHERE子句）

**Rationale**:
- 性能最优：在数据库层面过滤，减少数据传输
- 支持复杂查询：可以组合多个筛选条件
- 实时搜索：使用SQL LIKE进行模糊匹配

**Implementation Notes**:
- SearchFilter模型包含搜索关键词和筛选条件
- Repository方法根据SearchFilter构建动态SQL查询
- 使用参数化查询防止SQL注入

### 8. 事务处理策略

**Decision**: 使用SQLite事务，失败时自动回滚

**Rationale**:
- 符合宪法要求的"事务性操作"
- 确保数据一致性：导入操作要么全部成功，要么全部失败
- SQLite支持事务，使用BEGIN TRANSACTION、COMMIT、ROLLBACK

**Implementation Notes**:
- Excel导入使用事务包装
- 批量删除使用事务
- 使用try-catch确保失败时回滚

### 9. 错误处理和用户反馈

**Decision**: 详细的错误信息 + 进度指示

**Rationale**:
- 符合宪法要求的"错误指导"：错误信息包含解决步骤
- 导入错误明确指出行号和字段名
- 长时间操作显示进度，提升用户体验

**Implementation Notes**:
- ImportResult模型包含详细的错误信息（行号、字段名、错误原因）
- 使用IProgress<T>报告导入进度
- 错误信息格式："第X行，字段'XXX'：错误原因"

### 10. 异步操作处理

**Decision**: 使用async/await模式，后台线程执行耗时操作

**Rationale**:
- 符合宪法要求的"响应式架构"：>200ms的操作在后台线程执行
- 不阻塞UI线程，保持界面响应
- .NET 10.0完全支持async/await

**Implementation Notes**:
- Excel导入、数据库查询使用async方法
- ViewModel使用AsyncRelayCommand处理异步操作
- UI显示加载状态和进度

## Best Practices

### Excel导入最佳实践
1. 先读取并校验所有数据，再执行导入（两阶段导入）
2. 显示导入预览，让用户确认后再导入
3. 使用事务确保原子性
4. 提供详细的错误报告

### 数据访问最佳实践
1. 使用参数化查询防止SQL注入
2. 及时释放数据库连接（using语句）
3. 使用连接池管理连接
4. 错误处理和日志记录

### MVVM最佳实践
1. ViewModel不直接访问数据库，通过Repository
2. 使用ObservableCollection管理列表数据
3. 使用RelayCommand/AsyncRelayCommand处理用户操作
4. 属性变更通知使用SetProperty

## Dependencies Summary

- **Microsoft.Data.Sqlite**: SQLite数据库访问
- **ClosedXML** 或 **EPPlus**: Excel文件读写
- **CommunityToolkit.Mvvm**: MVVM框架（已集成）
- **System.Data.Common**: 数据库通用接口（.NET内置）

## Open Questions / Future Considerations

1. Excel模板的具体字段结构需要确认（需要查看人员导入模板.xls文件）
2. 是否需要支持CSV格式导入？（未来扩展）
3. 是否需要支持数据导出为Excel？（后续功能）
4. 数据库迁移策略（如需要修改表结构）

