using ISO11820.Data;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;

namespace ISO11820.Services;

/// <summary>
/// Excel 导出服务 — 生成包含三个 Sheet 的 Excel 报告。
///
/// Sheet 1：试验信息表（样品/日期/操作员/质量/温升/判定结论）
/// Sheet 2：温度数据表（从 CSV 读取，完整导入）
/// Sheet 3：温度曲线图（4 条折线：TF1/TF2/TS/TC）
///
/// 使用方式：
///   var excel = new ExcelExportService(dbHelper);
///   excel.Export(productId, testId, outputPath);
/// </summary>
public class ExcelExportService
{
    private readonly DbHelper _db;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="db">DbHelper 实例，用于查询试验信息</param>
    public ExcelExportService(DbHelper db)
    {
        _db = db;
    }

    /// <summary>
    /// 导出 Excel 报告。
    /// </summary>
    /// <param name="productId">样品编号</param>
    /// <param name="testId">试验标识</param>
    /// <param name="outputPath">Excel 文件完整路径</param>
    /// <param name="csvPath">CSV 温度数据文件路径</param>
    public void Export(string productId, string testId, string outputPath, string csvPath)
    {
        // EPPlus 教育用途许可
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        // 确保目录存在
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // 读取数据
        var test = _db.GetTest(productId, testId);
        var product = _db.GetProduct(productId);
        var records = CsvExportService.Read(csvPath);

        using var package = new ExcelPackage();

        // Sheet 1：试验信息表
        CreateInfoSheet(package, test, product);

        // Sheet 2：温度数据表
        CreateDataSheet(package, records);

        // Sheet 3：温度曲线图
        CreateChartSheet(package, records);

        // 保存
        package.SaveAs(new FileInfo(outputPath));
    }

    // ====================================================================
    // Sheet 1：试验信息表
    // ====================================================================

    private static void CreateInfoSheet(ExcelPackage package, TestMaster? test, ProductMaster? product)
    {
        var sheet = package.Workbook.Worksheets.Add("试验信息");

        // 标题
        sheet.Cells["A1:B1"].Merge = true;
        sheet.Cells["A1"].Value = "ISO 11820 建筑材料不燃性试验报告";
        sheet.Cells["A1"].Style.Font.Size = 16;
        sheet.Cells["A1"].Style.Font.Bold = true;

        // 基本信息
        int row = 3;
        AddInfoRow(sheet, ref row, "试验编号", $"{test?.ProductId ?? "—"} / {test?.TestId ?? "—"}");
        AddInfoRow(sheet, ref row, "试验日期", test?.TestDate.ToString("yyyy-MM-dd") ?? "—");
        AddInfoRow(sheet, ref row, "样品名称", product?.ProductName ?? "—");
        AddInfoRow(sheet, ref row, "规格型号", product?.Specific ?? "—");
        AddInfoRow(sheet, ref row, "操作员", test?.Operator ?? "—");
        AddInfoRow(sheet, ref row, "试验依据", test?.According ?? "ISO 11820:2022");

        row++;
        AddInfoRow(sheet, ref row, "环境温度", $"{test?.AmbTemp ?? 0:F1} °C");
        AddInfoRow(sheet, ref row, "环境湿度", $"{test?.AmbHumi ?? 0:F1} %");

        row++;
        AddInfoRow(sheet, ref row, "试验前质量", $"{test?.PreWeight ?? 0:F2} g");
        AddInfoRow(sheet, ref row, "试验后质量", $"{test?.PostWeight ?? 0:F2} g");
        AddInfoRow(sheet, ref row, "失重量", $"{test?.LostWeight ?? 0:F2} g");
        AddInfoRow(sheet, ref row, "失重率", $"{test?.LostWeightPer ?? 0:F2} %");
        AddInfoRow(sheet, ref row, "样品温升", $"{test?.DeltaTf ?? 0:F1} °C");
        AddInfoRow(sheet, ref row, "总试验时长", $"{test?.TotalTestTime ?? 0} 秒");

        row++;
        AddInfoRow(sheet, ref row, "火焰发生时刻", $"{test?.FlameTime ?? 0} 秒");
        AddInfoRow(sheet, ref row, "火焰持续时间", $"{test?.FlameDuration ?? 0} 秒");

        row++;
        // 判定结论
        bool passed = (test?.DeltaTf ?? 999) <= 50
                   && (test?.LostWeightPer ?? 999) <= 50
                   && (test?.FlameDuration ?? 999) < 5;
        AddInfoRow(sheet, ref row, "判定结论", passed ? "通过" : "不通过");

        if (!string.IsNullOrEmpty(test?.Memo))
        {
            row++;
            AddInfoRow(sheet, ref row, "备注", test.Memo);
        }

        // 列宽
        sheet.Column(1).Width = 18;
        sheet.Column(2).Width = 35;
    }

    private static void AddInfoRow(ExcelWorksheet sheet, ref int row, string label, string value)
    {
        sheet.Cells[$"A{row}"].Value = label;
        sheet.Cells[$"A{row}"].Style.Font.Bold = true;
        sheet.Cells[$"B{row}"].Value = value;
        row++;
    }

    // ====================================================================
    // Sheet 2：温度数据表
    // ====================================================================

    private static void CreateDataSheet(ExcelPackage package, List<TemperatureRecord> records)
    {
        var sheet = package.Workbook.Worksheets.Add("温度数据");

        // 标题行
        sheet.Cells["A1"].Value = "Time (s)";
        sheet.Cells["B1"].Value = "TF1 (°C)";
        sheet.Cells["C1"].Value = "TF2 (°C)";
        sheet.Cells["D1"].Value = "TS (°C)";
        sheet.Cells["E1"].Value = "TC (°C)";
        sheet.Cells["F1"].Value = "TCal (°C)";

        // 标题样式
        using (var header = sheet.Cells["A1:F1"])
        {
            header.Style.Font.Bold = true;
            header.Style.Fill.PatternType = ExcelFillStyle.Solid;
            header.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            header.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        // 数据行
        for (int i = 0; i < records.Count; i++)
        {
            int row = i + 2;
            var r = records[i];
            sheet.Cells[$"A{row}"].Value = r.Time;
            sheet.Cells[$"B{row}"].Value = Math.Round(r.Tf1, 1);
            sheet.Cells[$"C{row}"].Value = Math.Round(r.Tf2, 1);
            sheet.Cells[$"D{row}"].Value = Math.Round(r.Ts, 1);
            sheet.Cells[$"E{row}"].Value = Math.Round(r.Tc, 1);
            sheet.Cells[$"F{row}"].Value = Math.Round(r.Tcal, 1);
        }

        // 列宽
        sheet.Column(1).Width = 10;
        sheet.Column(2).Width = 14;
        sheet.Column(3).Width = 14;
        sheet.Column(4).Width = 14;
        sheet.Column(5).Width = 14;
        sheet.Column(6).Width = 14;
    }

    // ====================================================================
    // Sheet 3：温度曲线图
    // ====================================================================

    private static void CreateChartSheet(ExcelPackage package, List<TemperatureRecord> records)
    {
        var sheet = package.Workbook.Worksheets.Add("温度曲线");

        if (records.Count == 0) return;

        // 将数据写入 Sheet 3 的隐藏区域（供图表引用）
        for (int i = 0; i < records.Count; i++)
        {
            int row = i + 1;
            sheet.Cells[$"A{row}"].Value = records[i].Time;
            sheet.Cells[$"B{row}"].Value = Math.Round(records[i].Tf1, 1);
            sheet.Cells[$"C{row}"].Value = Math.Round(records[i].Tf2, 1);
            sheet.Cells[$"D{row}"].Value = Math.Round(records[i].Ts, 1);
            sheet.Cells[$"E{row}"].Value = Math.Round(records[i].Tc, 1);
        }

        int dataCount = records.Count;

        // 创建折线图
        var chart = sheet.Drawings.AddChart("TemperatureChart", eChartType.Line);
        chart.Title.Text = "温度曲线";
        chart.SetPosition(0, 0, 6, 0);
        chart.SetSize(900, 500);

        // X 轴：时间
        var xRange = sheet.Cells[$"A1:A{dataCount}"];

        // 4 条折线
        AddSeries(chart, sheet, sheet.Cells[$"B1:B{dataCount}"], xRange, "炉温1 (TF1)");
        AddSeries(chart, sheet, sheet.Cells[$"C1:C{dataCount}"], xRange, "炉温2 (TF2)");
        AddSeries(chart, sheet, sheet.Cells[$"D1:D{dataCount}"], xRange, "表面温 (TS)");
        AddSeries(chart, sheet, sheet.Cells[$"E1:E{dataCount}"], xRange, "中心温 (TC)");

        // X 轴标签
        chart.XAxis.Title.Text = "时间 (秒)";
        chart.YAxis.Title.Text = "温度 (°C)";
        chart.YAxis.MaxValue = 800;
        chart.YAxis.MinValue = 0;

        // 图例位置
        chart.Legend.Position = eLegendPosition.Bottom;
    }

    private static void AddSeries(ExcelChart chart, ExcelWorksheet sheet,
        ExcelRange yRange, ExcelRange xRange, string name)
    {
        var series = chart.Series.Add(xRange, yRange);
        series.Header = name;
    }
}