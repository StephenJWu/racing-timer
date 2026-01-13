# Contracts: WPF应用框架和导航菜单

**Feature**: WPF应用框架和导航菜单  
**Date**: 2025-01-27

## Overview

框架阶段不涉及API契约，因为这是桌面应用程序，不提供HTTP API。

## Service Interfaces

### INavigationService

导航服务接口，定义页面导航功能。

**Location**: `Timer/Services/INavigationService.cs`

**Methods**:
- `NavigateTo<TViewModel>()`: 导航到指定ViewModel类型的页面
- `NavigateTo(object viewModel)`: 导航到指定ViewModel实例的页面

**Implementation**: `Timer/Services/NavigationService.cs`

## Future Contracts

后续功能实现时，可能需要定义以下接口：
- IDeviceService: 设备服务接口
- IDatabaseService: 数据库服务接口
- ILoggingService: 日志服务接口
- IExcelService: Excel导入导出服务接口

这些接口将在相应的功能规范中定义。

