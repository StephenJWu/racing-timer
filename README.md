# 比赛计时系统 (Race Timer Pro)

<div align="center">

⚡ **专业的比赛计时与人员管理解决方案**

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-Windows-0078D6?logo=windows)
![SQLite](https://img.shields.io/badge/SQLite-Database-003B57?logo=sqlite)
![License](https://img.shields.io/badge/License-MIT-green)

</div>

---

## 📖 项目简介

**比赛计时系统**是一款基于 WPF 开发的专业比赛计时与人员管理软件，适用于学校运动会、体育赛事等场景。系统支持参赛人员管理、芯片分配、分组比赛、成绩记录与导出等完整功能。

## ✨ 功能特性

### 👥 人员管理
- 支持 Excel 批量导入参赛人员
- 管理参赛者信息（学校、年级、班级、姓名、性别、准考证号）
- 高级搜索与筛选功能
- 批量编辑与删除

### 🏷️ 芯片管理
- 芯片组管理（支持颜色标识）
- 芯片标签号码与内部编号映射
- Excel 批量导入芯片数据
- 芯片分配与回收

### 🏃 比赛分组
- 按学校/年级/班级/组别灵活分组
- 支持设置比赛圈数（1-20 圈）
- 芯片组与比赛组关联
- 参赛人员芯片自动分配

### ⏱️ 比赛计时
- 实时计时功能
- 多圈次成绩记录
- 芯片感应计时支持

### 📊 成绩管理
- 成绩查询与统计
- Excel 成绩导出
- 分组成绩排名

### 🔧 设备管理
- 计时设备配置
- 设备状态监控

## 🛠️ 技术架构

| 技术 | 说明 |
|------|------|
| **.NET 10** | 运行时框架 |
| **WPF** | Windows 桌面 UI 框架 |
| **MVVM** | 使用 CommunityToolkit.Mvvm 实现 |
| **SQLite** | 本地数据库存储 |
| **ClosedXML** | Excel 文件读写 |

## 📁 项目结构

```
Timer/
├── Timer/
│   ├── Models/              # 数据模型
│   │   ├── Participant.cs   # 参赛人员
│   │   ├── Chip.cs          # 芯片
│   │   ├── ChipGroup.cs     # 芯片组
│   │   └── RaceGroup.cs     # 比赛分组
│   ├── ViewModels/          # 视图模型
│   ├── Views/               # 视图界面
│   ├── Services/            # 业务服务
│   │   ├── ParticipantRepository.cs
│   │   ├── ChipRepository.cs
│   │   ├── RaceGroupRepository.cs
│   │   └── ExcelImportService.cs
│   ├── Converters/          # 值转换器
│   ├── Data/                # 数据库上下文
│   └── Resources/           # 样式资源
├── data/
│   └── timer.db             # SQLite 数据库
└── Timer.slnx               # 解决方案文件
```

## 🚀 快速开始

### 环境要求

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 或 VS Code

### 开发运行

```bash
# 克隆项目
git clone <repository-url>
cd tt

# 进入项目目录
cd Timer/Timer

# 还原依赖
dotnet restore

# 运行项目
dotnet run
```

### 发布构建

```bash
# 发布自包含的 x64 版本
dotnet publish -c Release -r win-x64 --self-contained true -o publish
```

## 📦 打包安装程序

项目使用 [Inno Setup](https://jrsoftware.org/isinfo.php) 创建安装程序。

### 一键打包

运行根目录下的 `build.bat` 脚本：

```batch
build.bat
```

该脚本会自动完成：
1. 关闭运行中的应用
2. 发布最新代码
3. 生成安装包到 `installer/` 目录

### 手动打包

1. 安装 [Inno Setup 6](https://jrsoftware.org/isinfo.php)
2. 先发布项目到 `publish` 目录
3. 运行 `setup.iss` 生成安装程序

## 📋 数据导入模板

项目提供以下 Excel 模板文件：

| 文件 | 说明 |
|------|------|
| `人员导入模板.xls` | 参赛人员批量导入模板 |
| `芯片测试数据.xlsx` | 芯片数据导入示例 |
| `测试数据.xlsx` | 综合测试数据 |

### 人员导入字段

| 字段 | 必填 | 说明 |
|------|------|------|
| 序号 | ✅ | 从 1 开始的连续编号 |
| 日期 | ✅ | 比赛日期 |
| 学校 | ❌ | 学校名称 |
| 年级 | ❌ | 年级 |
| 班级 | ❌ | 班级 |
| 姓名 | ✅ | 参赛者姓名 |
| 性别 | ✅ | 男/女 |
| 准考证号 | ❌ | 唯一标识 |
| 组别 | ❌ | 组别名称 |

## 📸 界面预览

系统采用现代化深色主题设计，界面美观、操作简便：

- 左侧导航菜单，层级清晰
- 右侧内容区域，功能丰富
- 支持响应式布局

## 🔧 配置说明

### 数据库位置

数据库文件默认存储在 `data/timer.db`，安装后用户有修改权限。

### 日志与调试

调试模式下可查看详细日志输出。

## 📄 许可证

本项目采用 MIT 许可证。

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！

1. Fork 本项目
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 提交 Pull Request

---

<div align="center">

**比赛计时系统** © 2026 Race Timer Pro

</div>


