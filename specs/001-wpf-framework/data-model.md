# Data Model: WPF应用框架和导航菜单

**Feature**: WPF应用框架和导航菜单  
**Date**: 2025-01-27  
**Phase**: Phase 1 - Design & Contracts

## Entities

### NavigationItem

**Purpose**: 表示导航菜单中的一个菜单项，支持嵌套子菜单

**Properties**:
- `Title` (string): 菜单项显示文本
- `Icon` (string): 图标路径或字符（可选）
- `Children` (ObservableCollection<NavigationItem>): 子菜单项集合（用于嵌套菜单）
- `IsExpanded` (bool): 是否展开（用于父菜单项）
- `IsSelected` (bool): 是否选中
- `Command` (ICommand): 导航命令
- `ViewModel` (object): 关联的ViewModel实例

**Relationships**:
- 可以包含多个子NavigationItem（Children属性）
- 关联一个ViewModel实例（ViewModel属性）

**State Transitions**:
- 初始状态: IsExpanded = false, IsSelected = false
- 点击父菜单项: IsExpanded 切换（true ↔ false）
- 点击菜单项: IsSelected = true，其他菜单项 IsSelected = false
- 导航到其他页面: 当前菜单项 IsSelected = false，新菜单项 IsSelected = true

**Validation Rules**:
- Title不能为空
- 如果Children不为空，则IsExpanded状态才有意义
- ViewModel必须实现INotifyPropertyChanged（如果使用）

### PageViewModel (Base/Interface)

**Purpose**: 所有页面ViewModel的基类或接口，提供通用功能

**Properties**:
- `Title` (string): 页面标题
- `IsLoading` (bool): 是否正在加载（用于显示加载状态）

**Methods**:
- `OnNavigatedTo()`: 页面导航到时的回调
- `OnNavigatedFrom()`: 页面导航离开时的回调

**Note**: 框架阶段，每个页面ViewModel可以简单继承ObservableObject，后续可以提取基类。

## View Models

### MainViewModel

**Purpose**: 主窗口的ViewModel，管理导航菜单和页面切换

**Properties**:
- `MenuItems` (ObservableCollection<NavigationItem>): 导航菜单项集合
- `CurrentView` (object): 当前显示的视图（UserControl）
- `SelectedMenuItem` (NavigationItem): 当前选中的菜单项

**Commands**:
- `NavigateCommand` (RelayCommand<NavigationItem>): 导航命令

**Methods**:
- `InitializeMenuItems()`: 初始化菜单项数据
- `NavigateTo(NavigationItem)`: 导航到指定菜单项对应的页面

### RaceTimerViewModel, ScoreViewModel, ParticipantViewModel, GroupViewModel, DeviceViewModel, ChipViewModel

**Purpose**: 各个功能页面的占位ViewModel

**Properties**:
- `Title` (string): 页面标题（如"比赛计时"、"成绩管理"等）
- `Message` (string): 占位提示信息（"功能待实现"）

**Note**: 框架阶段，这些ViewModel只包含基本属性，不包含业务逻辑。

## Services

### INavigationService

**Purpose**: 导航服务接口，定义页面导航功能

**Methods**:
- `NavigateTo<TViewModel>()`: 导航到指定ViewModel类型的页面
- `NavigateTo(object viewModel)`: 导航到指定ViewModel实例的页面
- `GoBack()`: 返回上一页（可选，框架阶段不实现）

**Implementation**: 
- 框架阶段使用简单实现：NavigationService
- 通过ContentControl切换视图
- 管理ViewModel实例生命周期

## Data Flow

```
User clicks menu item
    ↓
MainViewModel.NavigateCommand executes
    ↓
MainViewModel.NavigateTo(NavigationItem)
    ↓
INavigationService.NavigateTo(ViewModel)
    ↓
NavigationService creates/retrieves View instance
    ↓
MainViewModel.CurrentView = View
    ↓
ContentControl displays View (data binding)
```

## Notes

- 框架阶段不涉及数据持久化，所有数据都在内存中
- NavigationItem的ViewModel属性在首次访问时创建（懒加载）
- 视图实例可以缓存，避免重复创建
- 后续功能实现时，可以添加导航历史、参数传递等功能

