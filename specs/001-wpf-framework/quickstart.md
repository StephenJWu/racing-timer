# Quick Start Guide: WPF应用框架和导航菜单

**Feature**: WPF应用框架和导航菜单  
**Date**: 2025-01-27  
**Phase**: Phase 1 - Design & Contracts

## Prerequisites

- .NET 10.0 SDK
- Visual Studio 2022 或 Visual Studio Code
- Windows 10/11

## Setup Steps

### 1. 添加NuGet包依赖

在 `Timer.csproj` 中添加：

```xml
<ItemGroup>
  <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
</ItemGroup>
```

### 2. 创建项目结构

按照 `plan.md` 中的项目结构创建文件夹：
- ViewModels/
- Views/
- Models/
- Services/
- Converters/
- Resources/

### 3. 创建基础文件

按照以下顺序创建文件：

1. **Models/NavigationItem.cs**: 导航菜单项模型
2. **Services/INavigationService.cs**: 导航服务接口
3. **Services/NavigationService.cs**: 导航服务实现
4. **ViewModels/MainViewModel.cs**: 主窗口ViewModel
5. **ViewModels/*ViewModel.cs**: 各个页面ViewModel（占位）
6. **Views/*.xaml**: 各个页面视图
7. **App.xaml** 和 **App.xaml.cs**: 应用程序入口
8. **MainWindow.xaml** 和 **MainWindow.xaml.cs**: 主窗口

## Testing Scenarios

### Scenario 1: 应用启动

**Given** 应用程序已编译  
**When** 用户双击可执行文件  
**Then** 
- 应用程序应该在2秒内启动
- 主窗口应该显示，包含左侧导航菜单和右侧内容区
- 默认应该显示第一个菜单项对应的页面（比赛计时）

**Test Steps**:
1. 编译项目
2. 运行应用程序
3. 验证主窗口显示
4. 验证布局正确（左侧菜单 + 右侧内容）

### Scenario 2: 导航菜单显示

**Given** 主窗口已打开  
**When** 用户查看左侧导航菜单  
**Then**
- 应该看到所有菜单项：比赛计时、成绩管理、人员管理、设备管理
- 人员管理和设备管理应该可以展开/折叠
- 菜单项应该有现代化的UI风格

**Test Steps**:
1. 启动应用程序
2. 查看左侧导航菜单
3. 验证所有菜单项显示
4. 点击"人员管理"，验证子菜单展开
5. 再次点击"人员管理"，验证子菜单折叠

### Scenario 3: 页面切换

**Given** 主窗口已打开  
**When** 用户点击不同的菜单项  
**Then**
- 右侧内容区应该切换到对应的页面
- 页面切换应该在500ms内完成
- 选中的菜单项应该高亮显示

**Test Steps**:
1. 启动应用程序
2. 点击"比赛计时"菜单项
3. 验证右侧显示比赛计时页面
4. 点击"成绩管理"菜单项
5. 验证右侧切换到成绩管理页面
6. 验证菜单项选中状态更新

### Scenario 4: 占位页面显示

**Given** 用户点击任意菜单项  
**When** 页面切换完成  
**Then**
- 应该看到对应页面的占位视图
- 页面应该显示清晰的标题
- 页面应该显示"功能待实现"提示

**Test Steps**:
1. 启动应用程序
2. 依次点击所有菜单项（包括子菜单项）
3. 验证每个页面都正确显示占位内容
4. 验证页面标题和提示信息正确

### Scenario 5: UI交互反馈

**Given** 主窗口已打开  
**When** 用户与菜单项交互  
**Then**
- 鼠标悬停时应该显示hover效果
- 点击菜单项时应该显示选中状态
- 展开/折叠应该有平滑的动画效果

**Test Steps**:
1. 启动应用程序
2. 将鼠标悬停在菜单项上，验证hover效果
3. 点击菜单项，验证选中状态高亮
4. 展开/折叠父菜单项，验证动画效果

## Manual Testing Checklist

- [ ] 应用可以正常启动
- [ ] 主窗口布局正确（左侧菜单 + 右侧内容）
- [ ] 所有菜单项正确显示
- [ ] 嵌套菜单可以展开/折叠
- [ ] 点击菜单项可以切换页面
- [ ] 所有6个占位页面都可以访问
- [ ] 菜单项hover效果正常
- [ ] 菜单项选中状态高亮正常
- [ ] 页面切换流畅，无闪烁
- [ ] 窗口大小调整时布局正确响应

## Known Limitations

- 框架阶段不包含业务功能，所有页面都是占位视图
- 不包含数据持久化功能
- 不包含日志系统（基础结构已建立）
- 不包含错误处理机制（基础结构已建立）

## Next Steps

完成框架实现后，可以开始实现具体功能：
1. 参赛人员管理功能
2. 芯片设备管理功能
3. 扫描设备管理功能
4. 比赛计时功能
5. 成绩管理功能

每个功能都应该遵循相同的MVVM模式和项目结构。

