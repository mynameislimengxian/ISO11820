namespace ISO11820.Forms;

/// <summary>
/// 登录窗体 — 角色选择 + 密码验证。
/// 当前为占位骨架，角色 C 负责完善。
/// </summary>
public class LoginForm : Form
{
    public LoginForm()
    {
        Text = "ISO 11820 建筑材料不燃性试验系统 — 登录";
        Size = new Size(450, 350);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
    }
}
