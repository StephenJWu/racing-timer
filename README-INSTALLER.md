# 构建 Windows 安装程序指南

本指南说明如何将 Timer 应用打包成 Windows 安装程序（install.exe）。

## 方法一：使用 Inno Setup（推荐）

### 步骤 1：安装 Inno Setup

1. 下载 Inno Setup：https://jrsoftware.org/isdl.php
2. 安装 Inno Setup（建议安装到默认路径）

### 步骤 2：发布应用

```powershell
# 发布为单文件（推荐，文件更小）
dotnet publish Timer\Timer\Timer.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output Timer\Timer\bin\Publish\x64\Release `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true
```

### 步骤 3：构建安装程序

**选项 A：使用 PowerShell 脚本（自动）**

```powershell
.\build-installer.ps1
```

**选项 B：手动使用 Inno Setup**

1. 打开 Inno Setup Compiler
2. 打开 `Timer.iss` 文件
3. 点击 "Build" -> "Compile" 或按 F9
4. 安装程序将生成在 `Output\Timer-Setup.exe`

## 方法二：使用 WiX Toolset（高级）

WiX 更专业但配置更复杂，适合需要高级功能的场景。

### 安装 WiX

1. 下载 WiX Toolset：https://wixtoolset.org/releases/
2. 安装 WiX Toolset

### 创建 WiX 项目

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" Name="比赛计时系统" Language="2052" Version="1.0.0" Manufacturer="Timer Team" UpgradeCode="YOUR-GUID-HERE">
    <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />
    <MajorUpgrade DowngradeErrorMessage="A newer version is already installed." />
    <MediaTemplate />
    <Feature Id="ProductFeature" Title="Timer" Level="1">
      <ComponentGroupRef Id="ProductComponents" />
    </Feature>
  </Product>
  <Fragment>
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="比赛计时系统" />
      </Directory>
    </Directory>
  </Fragment>
  <Fragment>
    <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
      <Component Id="Timer.exe">
        <File Id="Timer.exe" Source="Timer\Timer\bin\Publish\x64\Release\Timer.exe" KeyPath="yes" />
      </Component>
    </ComponentGroup>
  </Fragment>
</Wix>
```

编译：
```cmd
candle Timer.wxs
light Timer.wixobj
```

## 方法三：使用 .NET 自带的单文件发布（最简单，但不是安装程序）

```powershell
dotnet publish Timer\Timer\Timer.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true
```

生成的文件在 `Timer\Timer\bin\Release\net10.0-windows\win-x64\publish\Timer.exe`，可以直接运行，但不是安装程序。

## 安装程序功能

生成的安装程序包含：

- ✅ 安装向导界面
- ✅ 自动创建桌面快捷方式（可选）
- ✅ 创建开始菜单项
- ✅ 卸载程序支持
- ✅ 管理员权限安装（可选）
- ✅ 安装进度显示

## 自定义安装程序

编辑 `Timer.iss` 文件可以自定义：

- 安装程序图标（SetupIconFile）
- 安装向导图片（WizardImageFile）
- 安装路径
- 安装选项
- 许可证文件
- 等等

## 注意事项

1. **.NET 运行时**：如果使用 `--self-contained true`，安装程序会包含 .NET 运行时，文件较大（约 100MB+）。如果目标机器已安装 .NET，可以使用 `--self-contained false` 减小体积。

2. **数据库文件**：应用使用 SQLite，数据库文件存储在 `data/timer.db`。安装程序不会覆盖已存在的数据库文件。

3. **测试**：在发布前，建议在干净的 Windows 虚拟机中测试安装程序。

## 快速开始

```powershell
# 1. 确保已安装 Inno Setup
# 2. 运行构建脚本
.\build-installer.ps1

# 3. 安装程序在 Output\Timer-Setup.exe
```

