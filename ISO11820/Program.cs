using ISO11820.Core;
using ISO11820.Data;
using ISO11820.Forms;
using ISO11820.Services;

namespace ISO11820;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // 高DPI适配：解决高分屏文字显示不全问题
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        ApplicationConfiguration.Initialize();

        // 1. 初始化全局配置
        AppGlobal.Instance.Initialize();

        // 2. 确保文件存储目录存在
        FileStorageManager.EnsureDirectories();

        // 3. 初始化数据库（首次运行自动建库建表 + 插入初始数据）
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppGlobal.Instance.DbPath);
        var dbDir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
            Directory.CreateDirectory(dbDir);

        var initializer = new DatabaseInitializer(dbPath);
        initializer.Initialize();

        // ===== 测试模式 =====
        if (args.Contains("--test"))
        {
            RunTests();
            return;
        }

        // 4. 创建共享依赖
        var db = new DbHelper(dbPath);

        // 5. 显示登录窗体
        using var loginForm = new LoginForm(db);
        if (loginForm.ShowDialog() != DialogResult.OK)
        {
            // 用户取消登录
            return;
        }

        // 6. 登录成功，启动主窗体
        Application.Run(new MainForm(
            db,
            loginForm.LoggedInUser,
            loginForm.LoggedInRole
        ));
    }

    /// <summary>
    /// 运行自动测试（命令行模式：dotnet run -- --test）
    /// </summary>
    private static void RunTests()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  ISO 11820 核心流程自动测试");
        Console.WriteLine("  龚小倩");
        Console.WriteLine("========================================");
        Console.WriteLine();

        var results = TestRunner.RunAutoTests();
        int passed = 0, failed = 0;

        foreach (var r in results)
        {
            if (r.Passed)
            {
                Console.WriteLine($"  ✅ {r.Name}: {r.Message}");
                passed++;
            }
            else
            {
                Console.WriteLine($"  ❌ {r.Name}: {r.Message}");
                failed++;
            }
        }

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine($"  结果: {passed} 通过, {failed} 失败, {results.Count} 总计");
        Console.WriteLine("========================================");

        // 人工测试清单
        Console.WriteLine();
        Console.WriteLine("--- 人工测试检查清单 ---");
        var manual = TestRunner.GetManualChecklist();
        foreach (var item in manual)
        {
            Console.WriteLine($"  □ [{item.Id}] {item.Name} — {item.Criteria} ({item.Method})");
        }

        Console.WriteLine();
        try { Console.ReadKey(); } catch { /* 无控制台输入时忽略 */ }
    }
}