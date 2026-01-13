# Quick Start Guide: 参赛人员管理

**Feature**: 参赛人员管理  
**Date**: 2025-01-27  
**Last Updated**: 2025-01-27  
**Phase**: Phase 1-4, 6 Completed | Phase 5, 7, 8 Partial

## Prerequisites

- .NET 10.0 SDK
- Visual Studio 2022 或 Visual Studio Code
- Windows 10/11
- SQLite数据库（通过Microsoft.Data.Sqlite）
- Excel文件读写库（ClosedXML或EPPlus）

## Setup Steps

### 1. 添加NuGet包依赖

在 `Timer.csproj` 中添加：

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.0" />
  <PackageReference Include="ClosedXML" Version="0.102.0" />
</ItemGroup>
```

### 2. 创建数据库

数据库文件位置：`data/timer.db`（可配置）

首次运行时自动创建数据库和表结构。

### 3. 创建项目结构

按照 `plan.md` 中的项目结构创建文件夹和文件：
- Models/ (Participant, ImportResult, SearchFilter)
- Services/ (IParticipantRepository, ParticipantRepository, IExcelImportService, ExcelImportService, ParticipantValidator)
- ViewModels/ (ParticipantViewModel)
- Views/ (ParticipantView.xaml)
- Data/ (DatabaseContext, 数据库初始化)

### 4. 实现核心组件

按照以下顺序实现：

1. **Models**: Participant, ImportResult, SearchFilter
2. **Services**: ParticipantValidator（数据校验）
3. **Services**: IParticipantRepository接口和ParticipantRepository实现
4. **Services**: IExcelImportService接口和ExcelImportService实现
5. **ViewModels**: ParticipantViewModel
6. **Views**: ParticipantView.xaml和ParticipantView.xaml.cs
7. **Data**: DatabaseContext（数据库初始化和迁移）

## Testing Scenarios

### Scenario 1: Excel导入参赛人员

**Given** 用户已打开参赛人员管理页面  
**When** 用户点击"导入"按钮并选择符合模板格式的Excel文件  
**Then** 
- 系统应该解析Excel文件
- 显示导入预览（总记录数、有效记录数、错误记录数）
- 显示详细的错误信息（如有）

**Test Steps**:
1. 准备测试Excel文件（包含有效和无效数据）
2. 点击导入按钮
3. 选择Excel文件
4. 验证导入预览显示
5. 确认导入
6. 验证导入结果
7. 验证数据已保存到数据库

### Scenario 2: 查看参赛人员列表

**Given** 数据库中已有参赛人员数据  
**When** 用户打开参赛人员管理页面  
**Then**
- 应该显示所有人员的列表
- 包含姓名、性别、号码布、组别等基本信息
- 支持分页显示（如果数据超过一页）

**Test Steps**:
1. 导入一些测试数据
2. 打开参赛人员页面
3. 验证列表正确显示
4. 验证分页功能（如果数据超过一页）

### Scenario 3: 搜索和筛选

**Given** 人员列表中有多个人员  
**When** 用户使用筛选条件（日期范围、级联下拉）或搜索关键词  
**Then**
- 列表应该实时过滤，只显示匹配的人员
- 支持日期范围筛选（开始日期、结束日期）
- 支持级联下拉筛选（学校 -> 年级 -> 班级 -> 组别）
- 支持按姓名、号码布搜索（SearchKeyword）
- 支持按组别、性别筛选

**Test Steps**:
1. 导入包含不同学校、年级、班级、组别和性别的人员数据
2. 选择开始日期和结束日期进行日期范围筛选
3. 验证列表只显示该日期范围内的人员
4. 选择学校，验证年级下拉自动更新
5. 选择年级，验证班级下拉自动更新
6. 选择班级，验证组别下拉自动更新
7. 选择组别，验证列表只显示该组别的人员
8. 点击"查询"按钮应用筛选条件
9. 清除筛选条件，验证列表显示所有人员

### Scenario 4: 编辑参赛人员

**Given** 用户查看人员列表  
**When** 用户点击某个人员的"编辑"按钮，修改信息并保存  
**Then**
- 应该打开编辑对话框
- 显示该人员的当前信息
- 保存后数据应该更新到数据库
- 列表应该刷新显示最新数据

**Test Steps**:
1. 选择一个人员
2. 点击编辑按钮
3. 修改人员信息
4. 点击保存
5. 验证数据已更新
6. 验证列表显示最新数据

### Scenario 5: 删除参赛人员

**Given** 用户查看人员列表  
**When** 用户选择一个人员并点击"删除"按钮，确认删除  
**Then**
- 应该显示确认对话框
- 确认后人员应该从数据库删除
- 列表应该刷新，不再显示该人员

**Test Steps**:
1. 选择一个人员
2. 点击删除按钮
3. 验证确认对话框显示
4. 点击确认
5. 验证人员已从数据库删除
6. 验证列表已更新

### Scenario 6: 数据校验

**Given** 用户尝试导入包含无效数据的Excel文件  
**When** 系统处理导入  
**Then**
- 应该拒绝无效数据
- 显示详细的错误信息（行号、字段名、错误原因）
- 有效数据应该成功导入

**Test Steps**:
1. 准备包含以下错误的Excel文件：
   - 必填字段为空
   - 日期格式错误
   - 序号不连续
   - 重复的身份证号或号码布
2. 执行导入
3. 验证错误信息详细且准确
4. 验证有效数据已导入

## Manual Testing Checklist

- [x] Excel导入功能正常（.xls和.xlsx格式）
- [x] 数据校验正确（必填字段、格式、重复性）
- [x] 导入进度显示正常
- [x] 导入结果报告准确
- [x] 人员列表正确显示
- [x] 分页功能正常
- [x] 日期范围筛选功能正常（开始日期、结束日期）
- [x] 级联下拉筛选功能正常（学校 -> 年级 -> 班级 -> 组别）
- [x] 搜索功能正常（姓名、号码布）
- [x] 筛选功能正常（组别、性别）
- [x] 数据持久化正常（重启应用后数据仍在）
- [x] 错误处理友好（数据库错误、文件错误等）
- [x] 事务处理正常（导入失败时回滚）
- [x] UI样式优化（DataGrid单横线、ComboBox下拉正常显示、日期格式yyyy-M-d）
- [x] 编辑功能正常（Phase 5 - Completed，支持编辑对话框、数据验证、保存后刷新）
- [x] 删除功能正常（单个和批量）（Phase 7 - Completed，支持单条删除与勾选批量删除）

## Known Limitations

- 当前版本不支持CSV格式导入（未来可扩展）
- 当前版本不支持数据导出为Excel（后续功能）
- Excel模板的具体字段结构需要根据实际模板文件确认
- 日期格式支持多种格式（YYYY-M-D, YYYY-MM-DD及其"上午"/"下午"变体），其他格式会被拒绝
- 编辑功能（Phase 5）和删除功能（Phase 7）已完成
- 单元测试覆盖率尚未达到80%目标（Phase 8 - Pending）

## Next Steps

完成参赛人员管理功能后，可以开始实现：
1. 人员分组功能（关联参赛人员）
2. 芯片设备管理功能（分配芯片给人员）
3. 比赛计时功能（使用参赛人员数据）
4. 成绩管理功能（关联参赛人员）

每个功能都应该遵循相同的MVVM模式和项目结构。

