using Microsoft.Extensions.Configuration;

namespace ISO11820.Core;

/// <summary>
/// 全局上下文 — 单例，持有所有核心对象引用。
/// 其他模块通过 AppGlobal.Instance 访问该单例。
/// </summary>
public class AppGlobal
{
    private static readonly Lazy<AppGlobal> _instance = new(() => new AppGlobal());
    public static AppGlobal Instance => _instance.Value;

    public IConfiguration Configuration { get; private set; } = null!;

    /// <summary>
    /// 初始化配置，由 Program.Main() 调用一次。
    /// </summary>
    public void Initialize()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    // ========== 配置快捷访问 ==========

    /// <summary>
    /// 数据库文件相对路径（从配置读取）。
    /// </summary>
    public string DbPath => Configuration["Database:SqlitePath"]
        ?? "Data\\ISO11820.db";

    /// <summary>
    /// 数据库文件绝对路径。
    /// </summary>
    public string DbFullPath => Path.GetFullPath(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbPath));

    private string? _baseDir;
    private string? _reportsDir;

    /// <summary>
    /// 从配置读取路径，如果是相对路径则转为相对于程序运行目录的绝对路径。
    /// </summary>
    private string ResolvePath(string key, string fallback)
    {
        var raw = Configuration[key] ?? fallback;
        return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, raw));
    }

    public string BaseDirectory => _baseDir ??= ResolvePath("FileStorage:BaseDirectory", @"Data\ISO11820_Data");

    public string ReportsDirectory => _reportsDir ??= ResolvePath("Report:OutputDirectory", @"Data\ISO11820_Data\Reports");

    public bool EnableSimulation => bool.Parse(
        Configuration["Simulation:EnableSimulation"] ?? "true");

    public double TargetFurnaceTemp => double.Parse(
        Configuration["Simulation:TargetFurnaceTemp"] ?? "750.0");

    public double HeatingRatePerSecond => double.Parse(
        Configuration["Simulation:HeatingRatePerSecond"] ?? "5.0");

    public double TempFluctuation => double.Parse(
        Configuration["Simulation:TempFluctuation"] ?? "0.5");

    public double StableThreshold => double.Parse(
        Configuration["Simulation:StableThreshold"] ?? "3.0");

    public double MaxTemperatureDriftPerTenMinutes => double.Parse(
        Configuration["Simulation:MaxTemperatureDriftPerTenMinutes"] ?? "2.0");

    public int DriftWindowSeconds => int.Parse(
        Configuration["Simulation:DriftWindowSeconds"] ?? "600");
}
