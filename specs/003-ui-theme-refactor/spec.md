# Feature Specification: UI主题重构 - 浅色卡片风格

**Feature ID**: 003  
**Feature Name**: UI主题重构 - 浅色卡片风格  
**Priority**: P1  
**Status**: Completed  
**Date**: 2025-01-27

## Summary

将整个比赛计时系统从深色主题（`#1e1e1e`）重构为浅色主题（白色卡片风格），参考 https://harbor-vhek.upma.site/ 的设计，提供现代化的、专业的用户界面体验。

## User Stories

### User Story 1 - 全局样式系统重构 (Priority: P1)

用户打开应用程序时，应该看到一个统一的浅色主题界面，所有页面使用一致的白色卡片风格、颜色方案和交互效果。

**Why this priority**: 这是整个UI重构的基础，所有其他UI改进都依赖于统一的样式系统。

**Independent Test**: 可以通过启动应用程序并验证所有页面是否使用浅色主题、卡片样式是否正确应用来测试。

**Acceptance Scenarios**:

1. **Given** 用户打开应用程序, **When** 查看主窗口, **Then** 应该看到浅色背景（`#f5f5f5`）和白色卡片风格的导航菜单
2. **Given** 用户查看任何页面, **When** 查看页面元素, **Then** 所有按钮、输入框、下拉框应该使用统一的浅色主题样式
3. **Given** 用户查看数据表格, **When** 查看表格样式, **Then** 应该看到白色背景、清晰的边框和浅色表头

### User Story 2 - 主窗口重构 (Priority: P1)

用户打开应用程序时，主窗口应该使用浅色主题，导航菜单为白色卡片风格，选中状态使用蓝色高亮。

**Why this priority**: 主窗口是用户首先看到的界面，需要立即体现新的设计风格。

**Acceptance Scenarios**:

1. **Given** 用户打开应用程序, **When** 查看主窗口, **Then** 背景应该是浅灰色（`#f5f5f5`）
2. **Given** 用户查看导航菜单, **When** 查看菜单样式, **Then** 菜单应该是白色背景，选中项使用蓝色高亮（`#1890ff`）
3. **Given** 用户悬停在菜单项上, **When** 鼠标悬停, **Then** 应该显示浅灰色背景的hover效果

### User Story 3 - 参赛人员页面重构 (Priority: P1)

用户打开参赛人员管理页面时，应该看到白色卡片风格的筛选区域和数据表格，包含日期范围筛选和级联下拉框。

**Why this priority**: 参赛人员管理是核心功能页面，需要完整的筛选功能和现代化的UI设计。

**Acceptance Scenarios**:

1. **Given** 用户打开参赛人员页面, **When** 查看页面布局, **Then** 应该看到白色卡片容器，包含筛选区域和数据表格区域
2. **Given** 用户查看筛选区域, **When** 查看筛选控件, **Then** 应该看到开始日期、结束日期、学校、年级、班级、组别的级联下拉框
3. **Given** 用户选择学校, **When** 选择学校后, **Then** 年级下拉框应该自动加载该学校的年级列表
4. **Given** 用户选择年级, **When** 选择年级后, **Then** 班级下拉框应该自动加载该年级的班级列表
5. **Given** 用户选择班级, **When** 选择班级后, **Then** 组别下拉框应该自动加载该班级的组别列表
6. **Given** 用户点击查询按钮, **When** 执行查询, **Then** 数据表格应该根据筛选条件更新显示
7. **Given** 用户查看数据表格, **When** 查看表格样式, **Then** 应该看到白色背景、清晰的列边框和浅色表头

### User Story 4 - 其他页面视图重构 (Priority: P2)

用户打开其他功能页面（比赛计时、成绩管理、组别管理、设备管理、芯片管理）时，应该看到统一的浅色主题和白色卡片风格。

**Why this priority**: 保持整个应用UI的一致性，提供统一的用户体验。

**Acceptance Scenarios**:

1. **Given** 用户打开任何功能页面, **When** 查看页面样式, **Then** 应该看到浅色背景和白色卡片容器
2. **Given** 用户查看占位页面, **When** 查看页面内容, **Then** 应该看到统一的浅色主题样式

## Functional Requirements

### FR1: 颜色方案定义

- **主背景**: `#f5f5f5` 或 `#fafafa`（浅灰背景）
- **卡片背景**: `#ffffff`（纯白）
- **主色调**: `#1890ff`（蓝色，用于主要按钮和强调）
- **次要按钮**: `#52c41a`（绿色，用于导入等操作）
- **文字颜色**: `#333333`（深灰，主要文字）
- **次要文字**: `#666666`（中灰）
- **边框颜色**: `#e8e8e8`（浅灰边框）
- **阴影**: 轻微阴影效果（`rgba(0,0,0,0.1)`）

### FR2: 卡片样式

- 白色背景（`#ffffff`）
- 圆角：`4px`
- 边框：`1px solid #e8e8e8`
- 阴影：轻微阴影（`0 2px 4px rgba(0,0,0,0.1)`）
- 内边距：`16px`

### FR3: 按钮样式

- **主按钮**（PrimaryButtonStyle）：蓝色背景（`#1890ff`），白色文字，hover时变为`#40a9ff`
- **次要按钮**（SecondaryButtonStyle）：绿色背景（`#52c41a`），白色文字，hover时变为`#73d13d`
- **默认按钮**：白色背景，深色文字，浅色边框，hover时显示浅灰背景

### FR4: 输入框和下拉框样式

- 白色背景
- 浅色边框（`#e8e8e8`）
- 圆角：`4px`
- 聚焦时边框变为蓝色（`#1890ff`）

### FR5: DataGrid样式

- 白色背景
- 清晰的列边框
- 表头：浅灰背景（`#fafafa`），深色文字
- 行hover效果：浅灰背景（`#f0f0f0`）
- 选中行：浅蓝背景（`#e6f7ff`）

### FR6: 级联下拉框功能

- 学校选择改变时，自动加载年级列表
- 年级选择改变时，自动加载班级列表
- 班级选择改变时，自动加载组别列表
- 上级选择清空时，下级选择自动清空

### FR7: 日期范围筛选

- 支持开始日期和结束日期的选择
- 日期选择器使用浅色主题样式
- 日期筛选与级联筛选可以组合使用

## Key Entities

### ColorResources
- BackgroundBrush: 主背景色
- CardBackgroundBrush: 卡片背景色
- PrimaryColorBrush: 主色调
- SecondaryColorBrush: 次要色调
- TextColorBrush: 主要文字颜色
- SecondaryTextColorBrush: 次要文字颜色
- BorderColorBrush: 边框颜色
- HoverBackgroundBrush: Hover背景色
- SelectedBackgroundBrush: 选中背景色
- TableHeaderBackgroundBrush: 表头背景色

### CardStyle
- 统一的卡片容器样式
- 应用于所有内容区域

### CascadeFilterData
- Schools: 学校列表
- Grades: 年级列表（依赖学校）
- Classes: 班级列表（依赖学校和年级）
- GroupNames: 组别列表（依赖学校、年级和班级）

## Success Criteria

1. ✅ 整个应用使用浅色主题（白色卡片风格）
2. ✅ 主窗口和导航菜单为浅色背景
3. ✅ 参赛人员页面包含完整的筛选功能（日期范围 + 级联下拉框）
4. ✅ 所有页面视图使用统一的卡片样式
5. ✅ 级联下拉框正常工作（学校→年级→班级→组别）
6. ✅ 日期筛选功能正常工作
7. ✅ 查询按钮和导入按钮使用正确的颜色（蓝色和绿色）
8. ✅ 数据表格显示清晰，使用浅色主题
9. ✅ 所有交互元素（按钮、输入框、下拉框）有适当的hover效果

## Out of Scope

- 功能逻辑的修改（只修改UI层）
- 数据模型的修改（SearchFilter扩展除外，用于支持级联筛选）
- 单元测试的编写（UI重构不涉及业务逻辑变更）

## Dependencies

- 依赖现有的MVVM架构
- 依赖现有的数据访问层（ParticipantRepository）
- 需要扩展SearchFilter模型以支持日期范围和级联筛选
- 需要扩展ParticipantRepository以支持级联查询

## Notes

- 保持现有的MVVM架构不变
- 保持现有的数据访问层和业务逻辑层不变
- 只修改UI层（View和样式）
- 确保所有功能在主题变更后仍然正常工作

