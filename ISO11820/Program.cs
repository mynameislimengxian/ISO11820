using ISO11820.Core;
using ISO11820.Forms;

namespace ISO11820;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 初始化全局上下文
        AppGlobal.Instance.Initialize();

        // 启动登录窗体
        Application.Run(new LoginForm());
    }
}
