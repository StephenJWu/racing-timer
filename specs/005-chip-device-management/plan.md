# Implementation Plan: 芯片设备管理

**Branch**: `005-chip-device-management` | **Date**: 2025-01-14 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-chip-device-management/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

实现芯片设备管理功能，包括从Excel文件批量导入芯片信息、芯片组管理（包括颜色设置）、芯片详情查看、编辑和删除等功能。数据持久化到SQLite数据库，严格遵循数据校验规则（非空、芯片标签号码唯一），使用事务确保数据一致性。采用MVVM架构，通过Repository模式抽象数据访问，支持芯片组列表展示和芯片详情查看。参考原型网站布局，页面采用上下分栏：上半部分为芯片组列表表格，下半部分为选中芯片组的芯片详情表格。

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: 
  - CommunityToolkit.Mvvm (MVVM框架，已集成)
  - Microsoft.Data.Sqlite (SQLite数据库访问，已集成)
  - ClosedXML (Excel文件读写，.xls/.xlsx支持，已集成)
  
**Storage**: SQLite (本地文件数据库)
  - 数据库文件位置：`data/timer.db` (与现有项目共享)
  - 支持Navicat等工具直接查看和SQL查询
  
**Testing**: 
  - xUnit 或 MSTest (单元测试框架)
  - Moq 或 NSubstitute (模拟框架)
  - 测试覆盖率目标：≥80% (核心业务逻辑)
  
**Target Platform**: Windows 10/11  
**Project Type**: WPF Desktop Application (single project，扩展现有Timer项目)  
**Performance Goals**: 
  - Excel导入100+条记录 < 5秒
  - 芯片组列表加载 < 300ms
  - 芯片详情显示 < 200ms
  - UI操作响应时间 < 200ms
  
**Constraints**: 
  - 必须遵循MVVM模式，View和ViewModel分离
  - 数据访问必须通过Repository模式抽象
  - Excel导入必须使用事务，失败时回滚
  - 芯片标签号码必须唯一
  - 必须支持芯片组颜色选择（预定义颜色列表）
  - 删除芯片组时必须级联删除关联的芯片
  
**Scale/Scope**: 
  - 1个芯片设备管理页面
  - 支持多个芯片组管理
  - Excel导入支持100+芯片记录批量导入
  - 芯片组列表支持表格形式展示

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

验证以下宪法合规性要求：

### 架构与代码质量
- [x] 分层架构：表示层（WPF UI）、业务逻辑层、数据访问层是否清晰分离？是否避免跨层直接调用？
  - **计划**: View层（ChipView.xaml）只负责UI展示，ViewModel层（ChipViewModel）处理业务逻辑，Repository层（IChipRepository）处理数据访问。View不直接访问Repository，通过ViewModel间接访问。
- [x] MVVM模式：是否使用CommunityToolkit.Mvvm实现视图与逻辑分离？ViewModel是否通过构造函数注入依赖？
  - **计划**: ChipViewModel继承ObservableObject，通过构造函数注入IChipRepository、IChipImportService等依赖。使用RelayCommand处理用户操作。
- [x] 单一职责：每个类是否不超过500行，职责是否清晰明确？
  - **计划**: ChipViewModel负责UI状态和命令，ChipRepository负责数据访问，ChipImportService负责Excel解析。每个类职责单一，代码量控制在500行以内。
- [x] 显式依赖：业务逻辑依赖是否通过构造函数注入？工具类是否合理使用静态方法？基础设施单例是否通过接口访问？
  - **计划**: ViewModel依赖通过构造函数注入。数据校验等工具类使用静态方法。数据库连接通过Repository接口访问，日志服务通过ILoggingService接口访问。

### 测试与调试
- [x] 单元测试：核心逻辑是否有单元测试计划，覆盖率目标是否≥80%？计时精度相关逻辑是否有专门测试？
  - **计划**: 核心业务逻辑（数据校验、Excel导入、数据访问）需要单元测试，覆盖率目标≥80%。本功能不涉及计时精度。
- [x] 可测试性：设备扫描服务是否通过接口抽象？计时服务是否可以独立测试？数据库操作是否通过仓储模式抽象？
  - **计划**: 数据库操作通过IChipRepository接口抽象，便于测试和模拟。Excel导入服务通过IChipImportService接口抽象。不涉及设备扫描和计时服务。
- [x] 日志系统：是否规划了Trace/Debug/Info/Warn/Error分级日志？是否按天rotate，详细日志保留90天？
  - **计划**: 使用已定义的ILoggingService接口，记录导入、编辑、删除等关键操作。日志配置（按天rotate、90天保留）在后续统一配置中实现。
- [x] 诊断工具：是否包含设备连接状态检查、数据库健康检查、计时器状态查询功能？
  - **计划**: 数据库连接状态可以通过Repository的健康检查方法查询。不涉及设备连接和计时器状态。

### 用户体验
- [x] 渐进式交互：复杂操作（数据导入、设备配置、分组添加）是否分步引导？
  - **计划**: Excel导入操作分步进行：选择文件 → 解析验证 → 导入结果。编辑操作使用对话框，分步填写和确认。
- [x] 状态可视化：是否规划了及时显示比赛状态（待开始、待发令、比赛中、已完成）和设备状态？
  - **计划**: 显示列表加载状态、导入进度、操作成功/失败状态。不涉及比赛状态和设备状态。
- [x] 操作反馈：所有操作是否有明确的成功/失败提示？长时间操作是否显示进度？
  - **计划**: 导入操作显示进度指示。所有操作（导入、编辑、删除）都有明确的成功/失败提示。错误信息明确指出行号和字段名。
- [x] 错误指导：错误信息是否包含解决步骤？数据导入错误是否明确指出行号和列号？
  - **计划**: 导入错误信息格式："第X行，字段'XXX'：错误原因"。编辑错误显示具体字段和错误原因。提供解决建议。

### 性能与可扩展性
- [x] 响应式架构：明显影响用户体验的操作（>200ms）是否在后台线程执行？设备扫描事件是否在后台处理？
  - **计划**: Excel导入、数据库查询等耗时操作在后台线程执行，使用async/await模式。UI线程只负责更新界面，不阻塞用户操作。
- [x] 实时性要求：计时器是否使用高精度计时器（Stopwatch），精度到毫秒？设备扫描事件处理延迟是否<100ms？
  - **计划**: 本功能不涉及计时器和设备扫描。列表选择和详情显示使用实时更新，响应时间<200ms。
- [x] 资源管理：数据库连接是否及时释放？设备连接是否实现IDisposable模式？大量数据查询是否使用分页？
  - **计划**: 数据库连接使用using语句确保及时释放。Repository实现IDisposable模式。芯片详情列表如果数据量大，考虑分页或虚拟化。
- [x] 模块化：核心计时功能是否独立于UI？设备服务是否通过接口抽象？数据导入导出是否模块化？
  - **计划**: 数据导入导出功能模块化，ChipImportService独立于UI。Repository通过接口抽象，便于替换实现。不涉及计时功能和设备服务。
- [x] 配置驱动：关键配置参数（数据库路径、日志路径、设备配置）是否外部化？
  - **计划**: 数据库路径使用现有配置。Excel导入配置（必填字段等）可通过配置调整。

### 数据管理与一致性
- [x] 数据校验：Excel导入是否校验所有必填字段、格式、重复性？日期格式是否支持三种格式？
  - **计划**: Excel导入校验必填字段（序号、芯片标签号码、芯片内部编号、组号）、芯片标签号码唯一性。不涉及日期格式。
- [x] 事务性操作：数据导入、芯片分配、比赛成绩记录是否支持事务和回滚？
  - **计划**: Excel导入使用数据库事务，失败时自动回滚。批量删除操作也使用事务。不涉及芯片分配和比赛成绩记录。
- [x] 数据可追溯：设备扫描记录、比赛成绩是否完整记录到数据库？数据库是否支持Navicat等工具直接查看？
  - **计划**: 导入、编辑、删除操作记录日志。数据库使用标准SQLite格式，支持Navicat等工具直接查看和SQL查询。不涉及设备扫描记录和比赛成绩。
- [x] 数据备份：是否支持成绩数据导出为Excel？是否支持数据库备份功能？
  - **计划**: 芯片数据导出功能（后续功能）。数据库备份功能在系统配置中实现（后续功能）。
- [x] 数据一致性：人员分组、芯片分配、比赛计时之间的数据是否保持一致？
  - **计划**: 通过数据库外键约束确保芯片与芯片组的关系一致性。删除芯片组时级联删除关联的芯片。

### 设备集成与实时性
- [x] 设备抽象：扫描设备服务是否通过接口抽象，支持模拟和真实设备？
  - **计划**: 本功能不涉及设备集成。
- [x] 实时扫描处理：扫描事件是否在后台线程处理？是否实现扫描去重（500ms窗口）？
  - **计划**: 本功能不涉及扫描处理。
- [x] 多组并发控制：每个组的计时是否独立管理？开跑时是否检查分组间隔≥10秒？
  - **计划**: 本功能不涉及计时控制。
- [x] 设备状态管理：设备激活/暂停状态是否实时更新？设备连通测试是否异步执行？
  - **计划**: 本功能不涉及设备状态管理。
- [x] 优雅降级：设备连接失败时是否有替代方案？设备服务不可用时其他功能是否正常工作？
  - **计划**: 数据库连接失败时显示友好错误提示，不影响其他功能模块。Excel导入失败时不影响已有数据。

## Project Structure

### Documentation (this feature)

```text
specs/005-chip-device-management/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
Timer/Timer/
├── ViewModels/
│   └── ChipViewModel.cs                  # 芯片设备管理页面ViewModel
│   └── ChipGroupViewModel.cs             # 芯片组显示模型（可选）
├── Views/
│   └── ChipView.xaml                     # 芯片设备管理页面视图
│   └── ChipView.xaml.cs
│   └── EditChipGroupDialog.xaml          # 编辑芯片组对话框
│   └── EditChipGroupDialog.xaml.cs
│   └── EditChipDialog.xaml               # 编辑芯片对话框
│   └── EditChipDialog.xaml.cs
├── Models/
│   └── ChipGroup.cs                      # 芯片组实体模型
│   └── Chip.cs                           # 芯片实体模型
│   └── ImportResult.cs                   # 导入结果模型（复用现有）
├── Services/
│   └── IChipRepository.cs                # 芯片数据访问接口
│   └── ChipRepository.cs                 # 芯片数据访问实现
│   └── IChipImportService.cs             # 芯片Excel导入服务接口
│   └── ChipImportService.cs              # 芯片Excel导入服务实现
├── Data/
│   └── DatabaseContext.cs                # 数据库上下文（扩展表创建SQL）
```

**Structure Decision**: 采用单项目结构，在现有Timer项目中扩展。数据访问层通过Repository模式抽象，便于测试和替换。Excel导入服务独立模块化，参考ExcelImportService的实现模式。数据模型遵循现有项目的命名和结构约定。

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Repository模式 | 需要支持单元测试和未来可能的数据库迁移 | 直接使用SQLite连接无法有效测试和模拟 |
| Excel导入服务独立模块 | 需要支持多种Excel格式和未来可能的CSV导入 | 在ViewModel中直接处理Excel会违反单一职责原则 |
| 级联删除 | 确保数据一致性，删除芯片组时自动删除关联芯片 | 手动删除会增加用户操作负担，且容易出现数据不一致 |
