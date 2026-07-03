using ISO11820.Core;

namespace ISO11820.Services;

/// <summary>
/// 文件存储管理器 — 统一管理所有文件路径和目录创建。
/// 纯静态方法，无需实例化。
/// </summary>
public static class FileStorageManager
{
    /// <summary>
    /// 获取基础存储目录
    /// </summary>
    public static string BaseDirectory => AppGlobal.Instance.BaseDirectory;

    /// <summary>
    /// 获取报告输出目录
    /// </summary>
    public static string ReportsDirectory => AppGlobal.Instance.ReportsDirectory;

    /// <summary>
    /// 确保所有基础目录存在。
    /// 程序启动时调用一次即可。
    /// </summary>
    public static void EnsureDirectories()
    {
        var dirs = new[]
        {
            BaseDirectory,
            Path.Combine(BaseDirectory, "TestData"),
            ReportsDirectory
        };

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }

    /// <summary>
    /// 获取试验温度数据 CSV 文件路径。
    /// 格式：{BaseDir}\TestData\{productId}\{testId}\sensor_data.csv
    /// </summary>
    public static string GetCsvPath(string productId, string testId)
    {
        var dir = Path.Combine(BaseDirectory, "TestData", productId, testId);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return Path.Combine(dir, "sensor_data.csv");
    }

    /// <summary>
    /// 获取试验数据目录路径。
    /// </summary>
    public static string GetTestDataDirectory(string productId, string testId)
    {
        var dir = Path.Combine(BaseDirectory, "TestData", productId, testId);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// 获取报告目录路径（确保存在）。
    /// </summary>
    public static string GetReportDirectory()
    {
        if (!Directory.Exists(ReportsDirectory))
            Directory.CreateDirectory(ReportsDirectory);
        return ReportsDirectory;
    }

    /// <summary>
    /// 获取 Excel 报告文件路径。
    /// 格式：{ReportsDir}\{testId}_报告.xlsx
    /// </summary>
    public static string GetExcelPath(string testId)
    {
        GetReportDirectory();
        return Path.Combine(ReportsDirectory, $"{testId}_报告.xlsx");
    }

    /// <summary>
    /// 获取 PDF 报告文件路径。
    /// 格式：{ReportsDir}\{testId}_报告.pdf
    /// </summary>
    public static string GetPdfPath(string testId)
    {
        GetReportDirectory();
        return Path.Combine(ReportsDirectory, $"{testId}_报告.pdf");
    }

    /// <summary>
    /// 获取曲线图 PNG 图片路径（用于 PDF 嵌入）。
    /// 格式：{ReportsDir}\{testId}_曲线图.png
    /// </summary>
    public static string GetChartImagePath(string testId)
    {
        GetReportDirectory();
        return Path.Combine(ReportsDirectory, $"{testId}_曲线图.png");
    }
}