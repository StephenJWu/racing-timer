@echo off
REM 批处理脚本：构建 Windows 安装程序
REM 需要先安装 Inno Setup: https://jrsoftware.org/isdl.php

echo === 构建 Windows 安装程序 ===

REM 1. 清理旧的发布文件
if exist "Timer\Timer\bin\Publish\x64\Release" (
    echo 清理旧的发布文件...
    rmdir /s /q "Timer\Timer\bin\Publish\x64\Release"
)

REM 2. 发布应用（单文件，包含所有依赖）
echo 发布应用...
dotnet publish "Timer\Timer\Timer.csproj" --configuration Release --runtime win-x64 --self-contained true --output "Timer\Timer\bin\Publish\x64\Release" -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true

if errorlevel 1 (
    echo 发布失败！
    pause
    exit /b 1
)

echo 发布成功！

REM 3. 检查 Inno Setup 是否安装
set "INNO_SETUP=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%INNO_SETUP%" (
    set "INNO_SETUP=%ProgramFiles%\Inno Setup 6\ISCC.exe"
)

if not exist "%INNO_SETUP%" (
    echo.
    echo 未找到 Inno Setup！
    echo 请先安装 Inno Setup: https://jrsoftware.org/isdl.php
    echo.
    echo 或者手动运行 Inno Setup 编译脚本: Timer.iss
    pause
    exit /b 1
)

REM 4. 编译安装程序
echo.
echo 编译安装程序...
"%INNO_SETUP%" "Timer.iss"

if errorlevel 1 (
    echo.
    echo 安装程序构建失败！
    pause
    exit /b 1
)

echo.
echo 安装程序构建成功！
echo 安装程序位置: Output\Timer-Setup.exe
pause

