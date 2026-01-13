# 创建便携版 ZIP 包（不依赖 Inno Setup）
# 这是一个临时方案，用于在没有安装程序工具时快速打包

param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

Write-Host "=== 创建便携版 ZIP 包 ===" -ForegroundColor Green

# 1. 确保应用已发布
$publishDir = "Timer\Timer\bin\Publish\$Platform\$Configuration"
if (-not (Test-Path "$publishDir\Timer.exe")) {
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
}

# 2. 创建输出目录
$outputDir = "Output"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

# 3. 创建临时打包目录
$zipDir = "$outputDir\Timer-Portable"
if (Test-Path $zipDir) {
    Remove-Item -Path $zipDir -Recurse -Force
}
New-Item -ItemType Directory -Path $zipDir | Out-Null

# 4. 复制文件
Write-Host "复制文件..." -ForegroundColor Yellow
Copy-Item "$publishDir\Timer.exe" -Destination $zipDir -Force

# 5. 创建说明文件
$readmeFile = Join-Path $zipDir "README.txt"
$readmeLines = @(
    "比赛计时系统 - 便携版",
    "",
    "使用说明：",
    "1. 解压此 ZIP 文件到任意目录",
    "2. 双击 Timer.exe 运行",
    "3. 数据库文件会自动创建在 data\timer.db",
    "",
    "注意：",
    "- 首次运行可能需要几秒钟解压",
    "- 请勿删除 Timer.exe 文件",
    "- 建议将整个文件夹放在固定位置使用",
    "",
    "版本：1.0.0"
)
$readmeLines | Out-File -FilePath $readmeFile -Encoding UTF8

# 6. 创建 ZIP 文件
$zipFile = "$outputDir\Timer-Portable-v1.0.0.zip"
if (Test-Path $zipFile) {
    Remove-Item -Path $zipFile -Force
}

Write-Host "创建 ZIP 文件..." -ForegroundColor Yellow
Compress-Archive -Path "$zipDir\*" -DestinationPath $zipFile -Force

# 7. 清理临时目录
Remove-Item -Path $zipDir -Recurse -Force

Write-Host "`n便携版 ZIP 包创建成功！" -ForegroundColor Green
Write-Host "文件位置: $zipFile" -ForegroundColor Cyan
Write-Host "`n注意：这是便携版，不是安装程序。用户需要手动解压使用。" -ForegroundColor Yellow

