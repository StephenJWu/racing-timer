# 创建测试Excel文件的PowerShell脚本
# 需要先安装 ClosedXML NuGet包

Add-Type -Path "Timer\Timer\bin\Debug\net10.0-windows\ClosedXML.dll"
Add-Type -Path "Timer\Timer\bin\Debug\net10.0-windows\DocumentFormat.OpenXml.dll"

$filePath = Join-Path $PSScriptRoot "测试数据.xlsx"

# 创建Excel工作簿
$workbook = New-Object ClosedXML.Excel.XLWorkbook
$worksheet = $workbook.Worksheets.Add("参赛人员")

# 设置表头
$worksheet.Cell(1, 1).Value = "序号"
$worksheet.Cell(1, 2).Value = "日期"
$worksheet.Cell(1, 3).Value = "学校"
$worksheet.Cell(1, 4).Value = "年级"
$worksheet.Cell(1, 5).Value = "班级"
$worksheet.Cell(1, 6).Value = "姓名"
$worksheet.Cell(1, 7).Value = "性别"
$worksheet.Cell(1, 8).Value = "准考证号"
$worksheet.Cell(1, 9).Value = "组别名称"

# 添加测试数据
$data = @(
    @{Seq=1; Date="2026-1-13"; School="A学校"; Grade="一年级"; Class="1班"; Name="张三"; Gender="男"; ExamNumber="2024001"; GroupName="1组"},
    @{Seq=2; Date="2026-1-13"; School="A学校"; Grade="一年级"; Class="1班"; Name="李四"; Gender="女"; ExamNumber="2024002"; GroupName="1组"},
    @{Seq=3; Date="2026-1-13"; School="A学校"; Grade="一年级"; Class="1班"; Name="王五"; Gender="男"; ExamNumber="2024003"; GroupName="1组"},
    @{Seq=4; Date="2026-1-13"; School="A学校"; Grade="一年级"; Class="1班"; Name="赵六"; Gender="男"; ExamNumber="2024004"; GroupName="2组"},
    @{Seq=5; Date="2026-1-13"; School="A学校"; Grade="一年级"; Class="1班"; Name="钱七"; Gender="女"; ExamNumber="2024005"; GroupName="2组"},
    @{Seq=6; Date="2026-1-13"; School="B学校"; Grade="一年级"; Class="1班"; Name="孙八"; Gender="男"; ExamNumber="2024101"; GroupName="1组"},
    @{Seq=7; Date="2026-1-13"; School="B学校"; Grade="一年级"; Class="1班"; Name="周九"; Gender="女"; ExamNumber="2024102"; GroupName="1组"}
)

$row = 2
foreach ($item in $data) {
    $worksheet.Cell($row, 1).Value = $item.Seq
    $worksheet.Cell($row, 2).Value = $item.Date
    $worksheet.Cell($row, 3).Value = $item.School
    $worksheet.Cell($row, 4).Value = $item.Grade
    $worksheet.Cell($row, 5).Value = $item.Class
    $worksheet.Cell($row, 6).Value = $item.Name
    $worksheet.Cell($row, 7).Value = $item.Gender
    $worksheet.Cell($row, 8).Value = $item.ExamNumber
    $worksheet.Cell($row, 9).Value = $item.GroupName
    $row++
}

# 保存文件
$workbook.SaveAs($filePath)
Write-Host "测试Excel文件已创建: $filePath"

