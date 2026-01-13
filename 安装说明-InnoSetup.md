# 安装 Inno Setup 步骤

## 下载和安装

1. **下载 Inno Setup**
   - 访问：https://jrsoftware.org/isdl.php
   - 下载最新版本（推荐 Inno Setup 6）

2. **安装**
   - 运行下载的安装程序
   - 使用默认安装路径即可
   - 安装完成后，重新运行 `build-installer.bat`

## 验证安装

安装完成后，Inno Setup 编译器通常位于：
- `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`
- 或 `C:\Program Files\Inno Setup 6\ISCC.exe`

## 安装完成后

重新运行构建脚本：
```cmd
build-installer.bat
```

或使用 PowerShell：
```powershell
.\build-installer.ps1
```

