namespace ISO11820.Services;

/// <summary>
/// 单行温度记录（对应 CSV 一行）
/// </summary>
public class TemperatureRecord
{
    /// <summary>秒序号（从 0 开始）</summary>
    public int Time { get; set; }

    /// <summary>炉温1 TF1（°C）</summary>
    public double Tf1 { get; set; }

    /// <summary>炉温2 TF2（°C）</summary>
    public double Tf2 { get; set; }

    /// <summary>表面温度 TS（°C）</summary>
    public double Ts { get; set; }

    /// <summary>中心温度 TC（°C）</summary>
    public double Tc { get; set; }

    /// <summary>校准温度 TCal（°C）</summary>
    public double Tcal { get; set; }
}

/// <summary>
/// CSV 导出服务 — 试验完成后自动生成温度时序数据 CSV 文件。
///
/// 使用方式 1（实时逐行写入）：
///   var csv = new CsvExportService();
///   csv.Open(filePath);
///   csv.AppendRow(new TemperatureRecord { Time=0, Tf1=25.0, ... });
///   csv.Close();
///
/// 使用方式 2（批量导出）：
///   CsvExportService.Export(filePath, records);
/// </summary>
public class CsvExportService : IDisposable
{
    private StreamWriter? _writer;
    private readonly object _lock = new();

    /// <summary>
    /// 打开 CSV 文件准备写入（实时模式）。
    /// 自动创建目录，写入列标题行。
    /// </summary>
    public void Open(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _writer = new StreamWriter(filePath, append: false, System.Text.Encoding.UTF8);
        _writer.WriteLine("Time,Temp1,Temp2,TempSurface,TempCenter,TempCalibration");
        _writer.Flush();
    }

    /// <summary>
    /// 追加一行温度数据（实时模式）。
    /// 线程安全，可在 DataBroadcast 回调中直接调用。
    /// </summary>
    public void AppendRow(TemperatureRecord record)
    {
        lock (_lock)
        {
            if (_writer == null)
                throw new InvalidOperationException("CSV 文件未打开，请先调用 Open()");

            _writer.WriteLine(
                $"{record.Time}," +
                $"{record.Tf1:F1}," +
                $"{record.Tf2:F1}," +
                $"{record.Ts:F1}," +
                $"{record.Tc:F1}," +
                $"{record.Tcal:F1}");
            _writer.Flush();
        }
    }

    /// <summary>
    /// 关闭 CSV 文件（实时模式）。
    /// </summary>
    public void Close()
    {
        lock (_lock)
        {
            _writer?.Flush();
            _writer?.Close();
            _writer = null;
        }
    }

    public void Dispose()
    {
        Close();
    }

    // ====================================================================
    // 批量导出（静态方法）
    // ====================================================================

    /// <summary>
    /// 批量导出：一次性将所有温度记录写入 CSV 文件。
    /// </summary>
    /// <param name="filePath">CSV 文件完整路径</param>
    /// <param name="records">温度记录列表</param>
    public static void Export(string filePath, List<TemperatureRecord> records)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using var writer = new StreamWriter(filePath, append: false, System.Text.Encoding.UTF8);
        writer.WriteLine("Time,Temp1,Temp2,TempSurface,TempCenter,TempCalibration");

        foreach (var r in records)
        {
            writer.WriteLine(
                $"{r.Time}," +
                $"{r.Tf1:F1}," +
                $"{r.Tf2:F1}," +
                $"{r.Ts:F1}," +
                $"{r.Tc:F1}," +
                $"{r.Tcal:F1}");
        }
    }

    /// <summary>
    /// 读取 CSV 文件，返回温度记录列表。
    /// 供 Excel 导出服务等下游使用。
    /// </summary>
    public static List<TemperatureRecord> Read(string filePath)
    {
        var records = new List<TemperatureRecord>();

        if (!File.Exists(filePath))
            return records;

        using var reader = new StreamReader(filePath, System.Text.Encoding.UTF8);
        reader.ReadLine(); // 跳过标题行

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(',');
            if (parts.Length < 6) continue;

            records.Add(new TemperatureRecord
            {
                Time = int.Parse(parts[0]),
                Tf1 = double.Parse(parts[1]),
                Tf2 = double.Parse(parts[2]),
                Ts = double.Parse(parts[3]),
                Tc = double.Parse(parts[4]),
                Tcal = double.Parse(parts[5])
            });
        }

        return records;
    }
}