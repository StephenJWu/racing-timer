# Research: WPF应用框架和导航菜单

**Feature**: WPF应用框架和导航菜单  
**Date**: 2025-01-27  
**Phase**: Phase 0 - Outline & Research

## Technology Decisions

### MVVM Framework: CommunityToolkit.Mvvm

**Decision**: 使用 CommunityToolkit.Mvvm 作为MVVM框架

**Rationale**: 
- CommunityToolkit.Mvvm 是Microsoft官方维护的MVVM工具包，提供ObservableObject、RelayCommand等核心功能
- 轻量级，不引入过多依赖
- 与.NET生态系统集成良好
- 符合宪法要求的MVVM模式实现

**Alternatives considered**:
- Prism: 功能强大但过于复杂，对于框架阶段来说太重
- Caliburn.Micro: 已不再积极维护
- MVVM Light: 已停止维护，迁移到CommunityToolkit.Mvvm

**Version**: 最新稳定版本（8.x）

### Dependency Injection: 可选，初期使用简单方式

**Decision**: 框架阶段使用简单的服务定位器模式，后续可升级为Microsoft.Extensions.DependencyInjection

**Rationale**:
- 框架阶段依赖关系简单，不需要完整的DI容器
- 使用接口抽象导航服务，便于后续升级
- 符合宪法要求的依赖注入原则（通过构造函数注入）

**Alternatives considered**:
- Microsoft.Extensions.DependencyInjection: 功能完整，但框架阶段可能过度设计
- SimpleInjector/Autofac: 第三方DI容器，增加依赖复杂度

**Future consideration**: 当项目复杂度增加时，可以迁移到Microsoft.Extensions.DependencyInjection

### UI Style: 现代化扁平风格

**Decision**: 采用现代化扁平UI风格，类似Material Design

**Rationale**:
- 符合用户需求（现代化扁平风格）
- 提供良好的视觉体验
- 易于实现和维护
- 支持hover效果和选中状态高亮

**Implementation approach**:
- 使用WPF样式和模板定义菜单项外观
- 使用触发器（Triggers）实现hover和选中状态
- 使用动画（Animations）实现平滑的展开/折叠效果

### Navigation Pattern: ViewModel-based Navigation

**Decision**: 使用ViewModel作为导航目标，通过ContentControl切换视图

**Rationale**:
- 符合MVVM模式，导航逻辑在ViewModel中
- 简单直接，易于理解和维护
- 支持后续扩展（如导航历史、参数传递）

**Alternatives considered**:
- 路由导航（类似Web路由）：对于桌面应用来说过于复杂
- 区域导航（Prism Regions）：需要引入Prism框架，增加复杂度

**Implementation**:
- INavigationService接口定义导航方法
- MainViewModel管理导航状态
- ContentControl绑定CurrentView属性

### Project Structure: Single Project

**Decision**: 使用单项目结构，所有代码在Timer项目中

**Rationale**:
- 框架阶段功能简单，不需要多项目结构
- 便于开发和维护
- 符合宪法要求的模块化设计（通过文件夹组织）

**Future consideration**: 如果项目规模扩大，可以考虑拆分为多个项目（如Timer.Core、Timer.UI）

## Best Practices Research

### WPF MVVM Best Practices

1. **ViewModel命名**: ViewModel类名应该以"ViewModel"结尾，对应View去掉"View"后缀
   - 例如: MainWindow.xaml → MainViewModel.cs

2. **命令模式**: 使用RelayCommand或AsyncRelayCommand处理用户交互
   - 避免在代码隐藏中处理业务逻辑

3. **数据绑定**: 优先使用数据绑定，避免直接操作UI元素
   - 使用INotifyPropertyChanged通知属性变更

4. **资源管理**: ViewModel实现IDisposable，确保资源清理
   - 取消事件订阅
   - 释放非托管资源

### Navigation Best Practices

1. **导航服务接口**: 定义INavigationService接口，便于测试和替换实现
2. **导航状态管理**: 在MainViewModel中集中管理导航状态
3. **视图生命周期**: 考虑视图的创建和销毁时机，避免内存泄漏

### UI/UX Best Practices

1. **响应式设计**: 使用Grid和DockPanel等布局容器，支持窗口大小调整
2. **视觉反馈**: 所有交互操作都应该有视觉反馈（hover、选中、禁用状态）
3. **动画效果**: 使用WPF动画实现平滑的过渡效果，提升用户体验

## Unresolved Questions

无未解决的问题。所有技术选择都已明确，符合宪法要求和项目需求。

## Next Steps

1. 创建数据模型文档（data-model.md）
2. 定义导航服务接口契约
3. 创建快速开始指南（quickstart.md）

