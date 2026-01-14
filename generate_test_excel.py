#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
生成芯片设备管理模块测试用的Excel文件
"""

try:
    from openpyxl import Workbook
    from openpyxl.styles import Font, Alignment
except ImportError:
    print("请先安装 openpyxl 库：pip install openpyxl")
    exit(1)

# 创建工作簿和工作表
wb = Workbook()
ws = wb.active
ws.title = "芯片信息"

# 设置表头
headers = ["序号", "芯片标签号码", "芯片内部编号", "组号"]
ws.append(headers)

# 设置表头样式
header_font = Font(bold=True)
header_alignment = Alignment(horizontal='center', vertical='center')
for col in range(1, len(headers) + 1):
    cell = ws.cell(row=1, column=col)
    cell.font = header_font
    cell.alignment = header_alignment

# 添加数据
data = [
    [1, 300, "00000768", "分组1"],
    [2, 301, "00000769", "分组1"],
    [3, 302, "00000770", "分组1"],
    [4, 303, "00000771", "分组1"],
    [5, 304, "00000772", "分组1"],
    [6, 305, "00000773", "分组1"],
    [7, 306, "00000774", "分组1"],
    [8, 307, "00000775", "分组1"],
    [9, 308, "00000776", "分组1"],
    [10, 309, "00000777", "分组1"],
    [11, 310, "00000778", "分组1"],
]

for row_data in data:
    ws.append(row_data)

# 调整列宽
ws.column_dimensions['A'].width = 10
ws.column_dimensions['B'].width = 15
ws.column_dimensions['C'].width = 18
ws.column_dimensions['D'].width = 15

# 保存文件
output_file = "芯片测试数据.xlsx"
wb.save(output_file)
print(f"Excel文件已生成：{output_file}")
print(f"包含 {len(data)} 条数据记录")

