using ISO11820.Core;
using ISO11820.Data;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using MdColor = MigraDoc.DocumentObjectModel.Color;
using MdFont = MigraDoc.DocumentObjectModel.Font;

namespace ISO11820.Services;

/// <summary>
/// PDF 报告生成服务。
/// 使用 PDFsharp-MigraDoc 6.x 生成包含试验概要、曲线图和判定结论的 PDF 报告。
/// </summary>
public class PdfExportService
{
    private readonly DbHelper _db;

    public PdfExportService(DbHelper db)
    {
        _db = db;
    }

    /// <summary>
    /// 生成 PDF 报告。
    /// </summary>
    /// <param name="productId">样品编号</param>
    /// <param name="testId">试验 ID</param>
    /// <param name="chartImagePath">曲线图 PNG 文件路径（可选，不存在则跳过）</param>
    /// <returns>生成的 PDF 文件路径</returns>
    public string ExportPdf(string productId, string testId, string? chartImagePath = null)
    {
        var test = _db.GetTest(productId, testId)
            ?? throw new InvalidOperationException($"未找到试验记录：{productId}/{testId}");

        var product = _db.GetProduct(productId);

        var outputPath = FileStorageManager.GetPdfPath(testId);

        var doc = new Document();
        DefineStyles(doc);

        var section = doc.AddSection();
        section.PageSetup.TopMargin = Unit.FromCentimeter(2);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(2);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(2.5);
        section.PageSetup.RightMargin = Unit.FromCentimeter(2.5);

        // ===== 标题 =====
        AddTitle(section);

        // ===== 试验概要信息 =====
        AddSummaryTable(section, test, product);

        // ===== 温度曲线图 =====
        if (!string.IsNullOrEmpty(chartImagePath) && File.Exists(chartImagePath))
        {
            AddChartImage(section, chartImagePath);
        }

        // ===== 判定结论 =====
        AddConclusion(section, test);

        // ===== 渲染 =====
        var renderer = new PdfDocumentRenderer
        {
            Document = doc
        };
        renderer.RenderDocument();
        renderer.PdfDocument.Save(outputPath);

        return outputPath;
    }

    // ========================================================================
    // 样式
    // ========================================================================

    private static void DefineStyles(Document doc)
    {
        // 默认样式
        var style = doc.Styles["Normal"];
        style.Font.Name = GetChineseFont();
        style.Font.Size = 10;
        style.ParagraphFormat.SpaceAfter = Unit.FromPoint(4);

        // 标题样式
        var titleStyle = doc.Styles.AddStyle("ReportTitle", "Normal");
        titleStyle.Font.Size = 18;
        titleStyle.Font.Bold = true;
        titleStyle.ParagraphFormat.Alignment = ParagraphAlignment.Center;
        titleStyle.ParagraphFormat.SpaceAfter = Unit.FromPoint(10);

        // 副标题
        var subtitleStyle = doc.Styles.AddStyle("Subtitle", "Normal");
        subtitleStyle.Font.Size = 13;
        subtitleStyle.Font.Bold = true;
        subtitleStyle.ParagraphFormat.SpaceBefore = Unit.FromPoint(16);
        subtitleStyle.ParagraphFormat.SpaceAfter = Unit.FromPoint(8);

        // 表头
        var headerStyle = doc.Styles.AddStyle("TableHeader", "Normal");
        headerStyle.Font.Bold = true;
        headerStyle.Font.Size = 9;
        headerStyle.ParagraphFormat.Alignment = ParagraphAlignment.Center;
    }

    // ========================================================================
    // 标题
    // ========================================================================

    private static void AddTitle(Section section)
    {
        var p = section.AddParagraph("ISO 11820 建筑材料不燃性试验报告", "ReportTitle");
        p.Format.SpaceAfter = Unit.FromPoint(4);

        var p2 = section.AddParagraph($"生成日期：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        p2.Format.Alignment = ParagraphAlignment.Center;
        p2.Format.SpaceAfter = Unit.FromPoint(16);
    }

    // ========================================================================
    // 试验概要表
    // ========================================================================

    private void AddSummaryTable(Section section, TestMaster test, ProductMaster? product)
    {
        section.AddParagraph("一、试验概要信息", "Subtitle");

        var table = section.AddTable();
        table.Borders.Width = 0.5;
        table.Borders.Color = Colors.Gray;

        // 列定义：标签列 + 值列
        var col1 = table.AddColumn(Unit.FromCentimeter(3.5));
        var col2 = table.AddColumn(Unit.FromCentimeter(5));
        var col3 = table.AddColumn(Unit.FromCentimeter(3.5));
        var col4 = table.AddColumn(Unit.FromCentimeter(5));

        // 行数据
        AddSummaryRow(table, "样品编号", test.ProductId);
        AddSummaryRow(table, "样品名称", product?.ProductName ?? "—");
        AddSummaryRow(table, "规格型号", product?.Specific ?? "—");
        AddSummaryRow(table, "样品尺寸", product != null ? $"Φ{product.Diameter}mm × {product.Height}mm" : "—");
        AddSummaryRow(table, "试验 ID", test.TestId);
        AddSummaryRow(table, "试验日期", test.TestDate.ToString("yyyy-MM-dd"));
        AddSummaryRow(table, "操作员", test.Operator);
        AddSummaryRow(table, "设备名称", test.ApparatusName);
        AddSummaryRow(table, "设备编号", test.ApparatusId);
        AddSummaryRow(table, "环境温度", $"{test.AmbTemp:F1} °C");
        AddSummaryRow(table, "环境湿度", $"{test.AmbHumi:F1} %");
        AddSummaryRow(table, "试验依据", test.According);
        AddSummaryRow(table, "试验时长", $"{test.TotalTestTime} 秒");
        AddSummaryRow(table, "恒功率值", $"{test.ConstPower}");
        AddSummaryRow(table, "试验前质量", $"{test.PreWeight:F2} g");
        AddSummaryRow(table, "试验后质量", $"{test.PostWeight:F2} g");
        AddSummaryRow(table, "失重量", $"{test.LostWeight:F2} g");
        AddSummaryRow(table, "失重率", $"{test.LostWeightPer:F2} %");
        AddSummaryRow(table, "样品温升 (ΔTf)", $"{test.DeltaTf:F2} °C");

        if (test.FlameDuration > 0)
        {
            AddSummaryRow(table, "火焰发生时刻", $"{test.FlameTime} 秒");
            AddSummaryRow(table, "火焰持续时间", $"{test.FlameDuration} 秒");
        }
        else
        {
            AddSummaryRow(table, "持续火焰", "未出现");
        }

        if (!string.IsNullOrEmpty(test.Memo))
        {
            AddSummaryRow(table, "备注", test.Memo);
        }
    }

    private static void AddSummaryRow(Table table, string label, string value)
    {
        var row = table.AddRow();
        row.HeightRule = RowHeightRule.AtLeast;
        row.Height = Unit.FromCentimeter(0.7);

        var cellLabel = row.Cells[0];
        cellLabel.AddParagraph(label);
        cellLabel.Format.Font.Bold = true;
        cellLabel.Shading.Color = MdColor.FromRgb(240, 240, 240);
        cellLabel.VerticalAlignment = VerticalAlignment.Center;

        var cellValue = row.Cells[1];
        cellValue.AddParagraph(value);
        cellValue.VerticalAlignment = VerticalAlignment.Center;

        // 合并到第4列
        cellValue.MergeRight = 1;
    }

    // ========================================================================
    // 曲线图
    // ========================================================================

    private static void AddChartImage(Section section, string imagePath)
    {
        section.AddParagraph("二、温度曲线图", "Subtitle");

        try
        {
            var image = section.AddImage(imagePath);
            image.Width = Unit.FromCentimeter(16);
            image.LockAspectRatio = true;
        }
        catch (Exception ex)
        {
            var p = section.AddParagraph($"[无法加载曲线图：{ex.Message}]");
            p.Format.Font.Color = Colors.Red;
        }
    }

    // ========================================================================
    // 判定结论
    // ========================================================================

    private static void AddConclusion(Section section, TestMaster test)
    {
        section.AddParagraph("三、判定结论", "Subtitle");

        // 判定规则
        bool deltaTfOk = test.DeltaTf <= 50;
        bool lostWeightOk = test.LostWeightPer <= 50;
        bool flameOk = test.FlameDuration < 5;
        bool passed = deltaTfOk && lostWeightOk && flameOk;

        // 判定指标表
        var table = section.AddTable();
        table.Borders.Width = 0.5;
        table.Borders.Color = Colors.Gray;

        table.AddColumn(Unit.FromCentimeter(4));
        table.AddColumn(Unit.FromCentimeter(3));
        table.AddColumn(Unit.FromCentimeter(3));
        table.AddColumn(Unit.FromCentimeter(3));

        // 表头
        var headerRow = table.AddRow();
        headerRow.Height = Unit.FromCentimeter(0.8);
        AddCell(headerRow, 0, "判定指标", true, true);
        AddCell(headerRow, 1, "实测值", true, true);
        AddCell(headerRow, 2, "标准值", true, true);
        AddCell(headerRow, 3, "结果", true, true);

        // 温升
        var row1 = table.AddRow();
        AddCell(row1, 0, "样品温升 (ΔTf)");
        AddCell(row1, 1, $"{test.DeltaTf:F1} °C");
        AddCell(row1, 2, "≤ 50 °C");
        AddCell(row1, 3, deltaTfOk ? "✅ 合格" : "❌ 不合格");

        // 失重率
        var row2 = table.AddRow();
        AddCell(row2, 0, "失重率");
        AddCell(row2, 1, $"{test.LostWeightPer:F1} %");
        AddCell(row2, 2, "≤ 50 %");
        AddCell(row2, 3, lostWeightOk ? "✅ 合格" : "❌ 不合格");

        // 火焰持续时间
        var row3 = table.AddRow();
        AddCell(row3, 0, "火焰持续时间");
        AddCell(row3, 1, $"{test.FlameDuration} 秒");
        AddCell(row3, 2, "< 5 秒");
        AddCell(row3, 3, flameOk ? "✅ 合格" : "❌ 不合格");

        section.AddParagraph(); // 空行

        // 综合结论
        var conclusion = section.AddParagraph();
        conclusion.Format.Alignment = ParagraphAlignment.Center;
        conclusion.Format.SpaceBefore = Unit.FromPoint(12);

        if (passed)
        {
            conclusion.AddFormattedText("综合判定结论：通过", new MdFont(GetChineseFont(), 14) { Bold = true, Color = MdColor.FromRgb(0, 128, 0) });
        }
        else
        {
            conclusion.AddFormattedText("综合判定结论：不通过", new MdFont(GetChineseFont(), 14) { Bold = true, Color = MdColor.FromRgb(200, 0, 0) });
        }

        // 说明
        var note = section.AddParagraph();
        note.Format.SpaceBefore = Unit.FromPoint(8);
        note.AddText("判定依据：ISO 11820:2020 建筑材料不燃性试验方法。");
        note.AddText("三项指标（温升 ≤ 50°C、失重率 ≤ 50%、火焰持续时间 < 5秒）全部满足时判定为通过。");
    }

    private static void AddCell(Row row, int index, string text, bool bold = false, bool center = false)
    {
        var cell = row.Cells[index];
        var p = cell.AddParagraph(text);
        if (bold) p.Format.Font.Bold = true;
        if (center) p.Format.Alignment = ParagraphAlignment.Center;
        cell.VerticalAlignment = VerticalAlignment.Center;
    }

    // ========================================================================
    // 中文字体
    // ========================================================================

    /// <summary>
    /// 获取系统中可用的中文字体名称。
    /// 优先使用微软雅黑，回退到宋体。
    /// </summary>
    private static string GetChineseFont()
    {
        // 尝试常见中文字体
        var fontsToTry = new[] { "Microsoft YaHei", "微软雅黑", "SimSun", "宋体", "SimHei", "黑体" };
        return fontsToTry.FirstOrDefault(f => FontExists(f)) ?? "Arial";
    }

    private static bool FontExists(string fontName)
    {
        try
        {
            var font = new MdFont(fontName, 10);
            return font.Name.Contains(fontName, StringComparison.OrdinalIgnoreCase)
                || fontName.Contains(font.Name, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}