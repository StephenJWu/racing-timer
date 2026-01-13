# PowerShell 脚本：构建 Windows 安装程序
# 需要先安装 Inno Setup: https://jrsoftware.org/isdl.php

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

Write-Host "=== 构建 Windows 安装程序 ===" -ForegroundColor Green

# 1. 清理旧的发布文件
$publishDir = "Timer\Timer\bin\Publish\$Platform\$Configuration"
if (Test-Path $publishDir) {
    Write-Host "清理旧的发布文件..." -ForegroundColor Yellow
    Remove-Item -Path $publishDir -Recurse -Force
}

# 2. 发布应用（单文件，包含所有依赖）
Write-Host "发布应用..." -ForegroundColor Yellow
dotnet publish "Timer\Timer\Timer.csproj" `
    --configuration $Configuration `
    --runtime win-$Platform `
    --self-contained true `
    --output $publishDir `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "发布失败！" -ForegroundColor Red
    exit 1
}

Write-Host "发布成功！" -ForegroundColor Green

# 3. 检查 Inno Setup 是否安装
$innoSetupPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $innoSetupPath)) {
    $innoSetupPath = "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
}

if (-not (Test-Path $innoSetupPath)) {
    Write-Host "`n未找到 Inno Setup！" -ForegroundColor Red
    Write-Host "请先安装 Inno Setup: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "`n或者手动运行 Inno Setup 编译脚本: Timer.iss" -ForegroundColor Yellow
    exit 1
}

# 4. 编译安装程序
Write-Host "`n编译安装程序..." -ForegroundColor Yellow
& $innoSetupPath "Timer.iss"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n安装程序构建成功！" -ForegroundColor Green
    Write-Host "安装程序位置: Output\Timer-Setup.exe" -ForegroundColor Cyan
} else {
    Write-Host "`n安装程序构建失败！" -ForegroundColor Red
    exit 1
}

