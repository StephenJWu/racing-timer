# Implementation Plan: WPF应用框架和导航菜单

**Branch**: `001-wpf-framework` | **Date**: 2025-01-27 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-wpf-framework/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

创建一个可运行的WPF应用框架，包含MVVM架构、左侧嵌套导航菜单、右侧内容区，以及所有功能页面的占位视图。框架采用CommunityToolkit.Mvvm实现MVVM模式，使用现代化扁平UI风格，为后续功能实现提供可维护、可扩展的基础架构。

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: 
  - CommunityToolkit.Mvvm (最新稳定版本，用于MVVM框架)
  - Microsoft.Extensions.DependencyInjection (可选，用于依赖注入)
  
**Storage**: N/A (框架阶段不涉及数据存储)  
**Testing**: 
  - xUnit 或 MSTest (单元测试框架)
  - Moq 或 NSubstitute (模拟框架，用于测试)
  
**Target Platform**: Windows 10/11  
**Project Type**: WPF Desktop Application (single project)  
**Performance Goals**: 
  - 应用启动时间 < 2秒
  - 页面切换响应时间 < 500ms
  - UI操作响应时间 < 200ms
  
**Constraints**: 
  - 必须遵循MVVM模式，View和ViewModel分离
  - 代码结构必须清晰，便于后续功能扩展
  - 所有类必须符合单一职责原则（不超过500行）
  
**Scale/Scope**: 
  - 6个功能页面（比赛计时、成绩管理、参赛人员、人员分组、扫描设备、芯片设备）
  - 1个主窗口 + 6个占位页面视图
  - 1个主ViewModel + 6个页面ViewModel

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

验证以下宪法合规性要求：

### 架构与代码质量
- [x] 分层架构：表示层（WPF UI）、业务逻辑层、数据访问层是否清晰分离？是否避免跨层直接调用？
  - **计划**: 框架阶段只涉及表示层和业务逻辑层（ViewModel），数据访问层在后续功能中实现。View和ViewModel严格分离，通过数据绑定和命令模式通信。
- [x] MVVM模式：是否使用CommunityToolkit.Mvvm实现视图与逻辑分离？ViewModel是否通过构造函数注入依赖？
  - **计划**: 使用CommunityToolkit.Mvvm作为MVVM框架，所有ViewModel继承自ObservableObject。ViewModel通过构造函数接收依赖（如导航服务），符合依赖注入原则。
- [x] 单一职责：每个类是否不超过500行，职责是否清晰明确？
  - **计划**: 每个ViewModel专注于单一页面的逻辑，View只负责展示。NavigationItem模型类只包含导航相关属性，职责清晰。
- [x] 显式依赖：业务逻辑依赖是否通过构造函数注入？工具类是否合理使用静态方法？基础设施单例是否通过接口访问？
  - **计划**: ViewModel依赖（如导航服务）通过构造函数注入。工具类（如转换器）使用静态方法。基础设施服务（如日志）通过接口访问。

### 测试与调试
- [x] 单元测试：核心逻辑是否有单元测试计划，覆盖率目标是否≥80%？计时精度相关逻辑是否有专门测试？
  - **计划**: 框架阶段主要测试导航逻辑和ViewModel状态管理。导航服务、ViewModel命令、数据绑定逻辑都需要单元测试。框架阶段不涉及计时逻辑，计时精度测试在后续功能中实现。
- [x] 可测试性：设备扫描服务是否通过接口抽象？计时服务是否可以独立测试？数据库操作是否通过仓储模式抽象？
  - **计划**: 框架阶段不涉及设备扫描、计时和数据库操作。导航服务通过接口抽象，便于测试和模拟。
- [x] 日志系统：是否规划了Trace/Debug/Info/Warn/Error分级日志？是否按天rotate，详细日志保留90天？
  - **计划**: 框架阶段建立日志系统基础结构，定义日志接口。完整的日志配置（按天rotate、90天保留）在后续功能中实现。
- [x] 诊断工具：是否包含设备连接状态检查、数据库健康检查、计时器状态查询功能？
  - **计划**: 框架阶段不涉及设备、数据库和计时器。导航状态可以通过ViewModel属性查询，便于诊断。

### 用户体验
- [x] 渐进式交互：复杂操作（数据导入、设备配置、分组添加）是否分步引导？
  - **计划**: 框架阶段不涉及复杂操作。导航菜单设计清晰，用户可以直观地找到所需功能。
- [x] 状态可视化：是否规划了及时显示比赛状态（待开始、待发令、比赛中、已完成）和设备状态？
  - **计划**: 框架阶段实现导航菜单的选中状态可视化。比赛状态和设备状态在后续功能中实现。
- [x] 操作反馈：所有操作是否有明确的成功/失败提示？长时间操作是否显示进度？
  - **计划**: 框架阶段实现菜单项选中状态的视觉反馈。Toast提示和进度条在后续功能中实现。
- [x] 错误指导：错误信息是否包含解决步骤？数据导入错误是否明确指出行号和列号？
  - **计划**: 框架阶段不涉及数据导入。导航错误（如页面加载失败）应该有友好的错误提示。

### 性能与可扩展性
- [x] 响应式架构：明显影响用户体验的操作（>200ms）是否在后台线程执行？设备扫描事件是否在后台处理？
  - **计划**: 框架阶段页面切换是轻量级操作，在UI线程执行即可。设备扫描事件在后续功能中实现。
- [x] 实时性要求：计时器是否使用高精度计时器（Stopwatch），精度到毫秒？设备扫描事件处理延迟是否<100ms？
  - **计划**: 框架阶段不涉及计时和设备扫描。页面切换响应时间目标<500ms，满足用户体验要求。
- [x] 资源管理：数据库连接是否及时释放？设备连接是否实现IDisposable模式？大量数据查询是否使用分页？
  - **计划**: 框架阶段不涉及数据库和设备。ViewModel实现IDisposable模式，确保资源清理。
- [x] 模块化：核心计时功能是否独立于UI？设备服务是否通过接口抽象？数据导入导出是否模块化？
  - **计划**: 框架阶段建立模块化结构。导航服务通过接口抽象，便于后续扩展。核心功能（计时、设备）在后续功能中实现。
- [x] 配置驱动：关键配置参数（数据库路径、日志路径、设备配置）是否外部化？
  - **计划**: 框架阶段建立配置管理基础结构。具体的配置项（数据库路径、日志路径、设备配置）在后续功能中添加。

### 数据管理与一致性
- [x] 数据校验：Excel导入是否校验所有必填字段、格式、重复性？日期格式是否支持三种格式？
  - **计划**: 框架阶段不涉及数据导入和校验。数据校验逻辑在后续功能中实现。
- [x] 事务性操作：数据导入、芯片分配、比赛成绩记录是否支持事务和回滚？
  - **计划**: 框架阶段不涉及数据操作。事务处理在后续功能中实现。
- [x] 数据可追溯：设备扫描记录、比赛成绩是否完整记录到数据库？数据库是否支持Navicat等工具直接查看？
  - **计划**: 框架阶段不涉及数据存储。数据可追溯性在后续功能中实现。
- [x] 数据备份：是否支持成绩数据导出为Excel？是否支持数据库备份功能？
  - **计划**: 框架阶段不涉及数据导出和备份。数据备份功能在后续功能中实现。
- [x] 数据一致性：人员分组、芯片分配、比赛计时之间的数据是否保持一致？
  - **计划**: 框架阶段不涉及业务数据。数据一致性在后续功能中实现。

### 设备集成与实时性
- [x] 设备抽象：扫描设备服务是否通过接口抽象，支持模拟和真实设备？
  - **计划**: 框架阶段不涉及设备集成。设备服务接口在后续功能中定义。
- [x] 实时扫描处理：扫描事件是否在后台线程处理？是否实现扫描去重（500ms窗口）？
  - **计划**: 框架阶段不涉及设备扫描。扫描处理逻辑在后续功能中实现。
- [x] 多组并发控制：每个组的计时是否独立管理？开跑时是否检查分组间隔≥10秒？
  - **计划**: 框架阶段不涉及计时和并发控制。多组并发逻辑在后续功能中实现。
- [x] 设备状态管理：设备激活/暂停状态是否实时更新？设备连通测试是否异步执行？
  - **计划**: 框架阶段不涉及设备状态管理。设备状态管理在后续功能中实现。
- [x] 优雅降级：设备连接失败时是否有替代方案？设备服务不可用时其他功能是否正常工作？
  - **计划**: 框架阶段不涉及设备。优雅降级策略在后续功能中实现。

## Project Structure

### Documentation (this feature)

```text
specs/001-wpf-framework/
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
├── App.xaml                    # 应用程序入口
├── App.xaml.cs                 # 应用程序代码
├── MainWindow.xaml             # 主窗口视图
├── MainWindow.xaml.cs          # 主窗口代码隐藏
├── ViewModels/
│   ├── MainViewModel.cs        # 主窗口ViewModel（导航逻辑）
│   ├── RaceTimerViewModel.cs   # 比赛计时页面ViewModel（占位）
│   ├── ScoreViewModel.cs       # 成绩管理页面ViewModel（占位）
│   ├── ParticipantViewModel.cs # 参赛人员页面ViewModel（占位）
│   ├── GroupViewModel.cs       # 人员分组页面ViewModel（占位）
│   ├── DeviceViewModel.cs      # 扫描设备页面ViewModel（占位）
│   └── ChipViewModel.cs        # 芯片设备页面ViewModel（占位）
├── Views/
│   ├── MainWindow.xaml         # 主窗口视图（已在根目录）
│   ├── RaceTimerView.xaml      # 比赛计时页面（占位）
│   ├── ScoreView.xaml          # 成绩管理页面（占位）
│   ├── ParticipantView.xaml    # 参赛人员页面（占位）
│   ├── GroupView.xaml          # 人员分组页面（占位）
│   ├── DeviceView.xaml         # 扫描设备页面（占位）
│   └── ChipView.xaml           # 芯片设备页面（占位）
├── Models/
│   └── NavigationItem.cs       # 导航菜单项模型
├── Services/
│   └── INavigationService.cs   # 导航服务接口
├── Converters/
│   └── BoolToVisibilityConverter.cs  # 布尔值转可见性转换器
└── Resources/
    └── Styles.xaml             # 全局样式资源
```

**Structure Decision**: 采用单项目结构（Option 1），因为这是WPF桌面应用，所有代码都在同一个项目中。ViewModels、Views、Models、Services分层清晰，符合MVVM模式和宪法要求。Converters和Resources用于支持UI功能。

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

无违反宪法要求的情况。框架设计完全符合宪法原则。

