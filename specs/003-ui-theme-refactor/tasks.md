# Tasks: UI主题重构 - 浅色卡片风格

**Input**: Design documents from `/specs/003-ui-theme-refactor/`  
**Status**: ✅ All tasks completed

## Phase 1: 全局样式系统重构 ✅

**Purpose**: 创建统一的浅色主题样式系统

- [x] T001 定义浅色主题颜色资源（主背景、卡片背景、主色调等）in `Timer/Timer/Resources/Styles.xaml`
- [x] T002 创建卡片样式（CardStyle）in `Timer/Timer/Resources/Styles.xaml`
- [x] T003 创建主按钮样式（PrimaryButtonStyle - 蓝色）in `Timer/Timer/Resources/Styles.xaml`
- [x] T004 创建次要按钮样式（SecondaryButtonStyle - 绿色）in `Timer/Timer/Resources/Styles.xaml`
- [x] T005 更新输入框样式（TextBox、ComboBox、DatePicker）in `Timer/Timer/Resources/Styles.xaml`
- [x] T006 更新DataGrid样式（白色背景、清晰的边框）in `Timer/Timer/Resources/Styles.xaml`
- [x] T007 更新导航菜单样式（浅色背景）in `Timer/Timer/Resources/Styles.xaml`

## Phase 2: 主窗口重构 ✅

**Purpose**: 将主窗口改为浅色主题

- [x] T008 将主窗口背景从深色改为浅色 in `Timer/Timer/MainWindow.xaml`
- [x] T009 将左侧导航菜单改为浅色背景 in `Timer/Timer/MainWindow.xaml`
- [x] T010 更新导航菜单项样式（浅色背景，hover效果）in `Timer/Timer/MainWindow.xaml`
- [x] T011 更新选中状态为蓝色高亮 in `Timer/Timer/MainWindow.xaml`
- [x] T012 更新右侧内容区为浅灰背景 in `Timer/Timer/MainWindow.xaml`

## Phase 3: 数据层增强（级联查询支持） ✅

**Purpose**: 支持级联下拉框的数据查询

- [x] T013 添加级联查询接口方法 in `Timer/Timer/Services/IParticipantRepository.cs`
- [x] T014 实现GetDistinctSchoolsAsync方法 in `Timer/Timer/Services/ParticipantRepository.cs`
- [x] T015 实现GetDistinctGradesAsync方法 in `Timer/Timer/Services/ParticipantRepository.cs`
- [x] T016 实现GetDistinctClassesAsync方法 in `Timer/Timer/Services/ParticipantRepository.cs`
- [x] T017 实现GetDistinctGroupNamesAsync方法 in `Timer/Timer/Services/ParticipantRepository.cs`
- [x] T018 更新SearchFilter模型，添加StartDate、EndDate字段 in `Timer/Timer/Models/SearchFilter.cs`
- [x] T019 更新SearchFilter模型，添加Grade、Class字段 in `Timer/Timer/Models/SearchFilter.cs`
- [x] T020 更新GetAllAsync方法，支持日期范围筛选 in `Timer/Timer/Services/ParticipantRepository.cs`
- [x] T021 更新GetAllAsync方法，支持Grade、Class筛选 in `Timer/Timer/Services/ParticipantRepository.cs`
- [x] T022 更新GetTotalCountAsync方法，支持日期范围和级联筛选 in `Timer/Timer/Services/ParticipantRepository.cs`

## Phase 4: ViewModel增强（级联下拉框支持） ✅

**Purpose**: 实现级联下拉框的数据绑定和逻辑

- [x] T023 添加日期范围属性（StartDate, EndDate）in `Timer/Timer/ViewModels/ParticipantViewModel.cs`
- [x] T024 添加级联下拉框数据源（Schools, Grades, Classes, GroupNames）in `Timer/Timer/ViewModels/ParticipantViewModel.cs`
- [x] T025 实现LoadSchoolsAsync方法 in `Timer/Timer/ViewModels/ParticipantViewModel.cs`
- [x] T026 实现OnSchoolChangedAsync方法（级联逻辑）in `Timer/Timer/ViewModels/ParticipantViewModel.cs`
- [x] T027 实现OnGradeChangedAsync方法（级联逻辑）in `Timer/Timer/ViewModels/ParticipantViewModel.cs`
- [x] T028 实现OnClassChangedAsync方法（级联逻辑）in `Timer/Timer/ViewModels/ParticipantViewModel.cs`
- [x] T029 添加包装属性（SelectedSchool, SelectedGrade, SelectedClass）in `Timer/Timer/ViewModels/ParticipantViewModel.cs`

## Phase 5: 参赛人员页面重构 ✅

**Purpose**: 重构参赛人员页面为白色卡片风格

- [x] T030 创建筛选区域卡片容器 in `Timer/Timer/Views/ParticipantView.xaml`
- [x] T031 添加页面标题和图标 in `Timer/Timer/Views/ParticipantView.xaml`
- [x] T032 添加开始日期选择器 in `Timer/Timer/Views/ParticipantView.xaml`
- [x] T033 添加结束日期选择器 in `Timer/Timer/Views/ParticipantView.xaml`
- [x] T034 添加学校下拉框 in `Timer/Timer/Views/ParticipantView.xaml`
- [x] T035 添加年级下拉框（级联）in `Timer/Timer/Views/ParticipantView.xaml`
- [x] T036 添加班级下拉框（级联）in `Timer/Timer/Views/ParticipantView.xaml`
- [x] T037 添加组别下拉框（级联）in `Timer/Timer/Views/ParticipantView.xaml`
- [x] T038 添加"查询"按钮（蓝色）in `Timer/Timer/Views/ParticipantView.xaml`
- [x] T039 添加"导入人员信息"按钮（绿色）in `Timer/Timer/Views/ParticipantView.xaml`
- [x] T040 重构数据表格为白色卡片风格 in `Timer/Timer/Views/ParticipantView.xaml`
- [x] T041 添加"共找到 X 条记录"提示 in `Timer/Timer/Views/ParticipantView.xaml`

## Phase 6: 其他页面视图重构 ✅

**Purpose**: 将所有占位页面改为浅色主题

- [x] T042 重构RaceTimerView为浅色主题 in `Timer/Timer/Views/RaceTimerView.xaml`
- [x] T043 重构ScoreView为浅色主题 in `Timer/Timer/Views/ScoreView.xaml`
- [x] T044 重构GroupView为浅色主题 in `Timer/Timer/Views/GroupView.xaml`
- [x] T045 重构DeviceView为浅色主题 in `Timer/Timer/Views/DeviceView.xaml`
- [x] T046 重构ChipView为浅色主题 in `Timer/Timer/Views/ChipView.xaml`

## Summary

✅ **所有任务已完成** (46/46)

UI主题重构已完成，整个应用现在使用统一的浅色主题（白色卡片风格），所有页面视图已更新，级联筛选功能已实现。

