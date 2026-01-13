using System;
using ClosedXML.Excel;
using System.IO;

namespace Timer.Tools
{
    /// <summary>
    /// 创建测试Excel文件的工具类
    /// </summary>
    public class CreateTestExcel
    {
        public static void CreateTestFile(string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("参赛人员");

            // 设置表头
            worksheet.Cell(1, 1).Value = "序号";
            worksheet.Cell(1, 2).Value = "日期";
            worksheet.Cell(1, 3).Value = "学校";
            worksheet.Cell(1, 4).Value = "年级";
            worksheet.Cell(1, 5).Value = "班级";
            worksheet.Cell(1, 6).Value = "姓名";
            worksheet.Cell(1, 7).Value = "性别";
            worksheet.Cell(1, 8).Value = "准考证号";
            worksheet.Cell(1, 9).Value = "组别名称";

            // 设置表头样式
            var headerRange = worksheet.Range(1, 1, 1, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // 添加测试数据
            var data = new[]
            {
                new { Seq = 1, Date = "2026-1-13", School = "A学校", Grade = "一年级", Class = "1班", Name = "张三", Gender = "男", ExamNumber = "2024001", GroupName = "1组" },
                new { Seq = 2, Date = "2026-1-13", School = "A学校", Grade = "一年级", Class = "1班", Name = "李四", Gender = "女", ExamNumber = "2024002", GroupName = "1组" },
                new { Seq = 3, Date = "2026-1-13", School = "A学校", Grade = "一年级", Class = "1班", Name = "王五", Gender = "男", ExamNumber = "2024003", GroupName = "1组" },
                new { Seq = 4, Date = "2026-1-13", School = "A学校", Grade = "一年级", Class = "1班", Name = "赵六", Gender = "男", ExamNumber = "2024004", GroupName = "2组" },
                new { Seq = 5, Date = "2026-1-13", School = "A学校", Grade = "一年级", Class = "1班", Name = "钱七", Gender = "女", ExamNumber = "2024005", GroupName = "2组" },
                new { Seq = 6, Date = "2026-1-13", School = "B学校", Grade = "一年级", Class = "1班", Name = "孙八", Gender = "男", ExamNumber = "2024101", GroupName = "1组" },
                new { Seq = 7, Date = "2026-1-13", School = "B学校", Grade = "一年级", Class = "1班", Name = "周九", Gender = "女", ExamNumber = "2024102", GroupName = "1组" }
            };

            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.Seq;
                worksheet.Cell(row, 2).Value = item.Date;
                worksheet.Cell(row, 3).Value = item.School;
                worksheet.Cell(row, 4).Value = item.Grade;
                worksheet.Cell(row, 5).Value = item.Class;
                worksheet.Cell(row, 6).Value = item.Name;
                worksheet.Cell(row, 7).Value = item.Gender;
                worksheet.Cell(row, 8).Value = item.ExamNumber;
                worksheet.Cell(row, 9).Value = item.GroupName;
                row++;
            }

            // 自动调整列宽
            worksheet.Columns().AdjustToContents();

            // 保存文件
            workbook.SaveAs(filePath);
            Console.WriteLine($"测试Excel文件已创建: {filePath}");
        }
    }
}

