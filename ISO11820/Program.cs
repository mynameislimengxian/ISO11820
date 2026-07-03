using ISO11820.Core;
using ISO11820.Data;
using ISO11820.Forms;
using ISO11820.Services;

namespace ISO11820;

static class Program
{
    [STAThread]
    static void Main()
    {
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
}