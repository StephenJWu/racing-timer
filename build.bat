@echo off
chcp 65001 >nul
echo ========================================
echo   比赛计时系统 - 打包脚本
echo ========================================
echo.

:: 关闭可能运行的应用
echo [1/3] 关闭运行中的应用...
taskkill /F /IM Timer.exe 2>nul
echo.

:: 发布最新代码
echo [2/3] 发布最新代码...
cd /d D:\ai_coding\test2\tt\Timer\Timer
dotnet publish -c Release -r win-x64 --self-contained true -o publish
if %errorlevel% neq 0 (
    echo 发布失败！
    pause
    exit /b 1
)
echo.

:: 打包安装程序
echo [3/3] 打包安装程序...
"D:\Inno Setup 6\ISCC.exe" "D:\ai_coding\test2\tt\setup.iss"
if %errorlevel% neq 0 (
    echo 打包失败！
    pause
    exit /b 1
)

echo.
echo ========================================
echo   打包完成！
echo   安装包位置: D:\ai_coding\test2\tt\installer\
echo ========================================
echo.
pause



