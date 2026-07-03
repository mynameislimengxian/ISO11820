using ISO11820.Core;
using ISO11820.Data;

namespace ISO11820.Forms;

/// <summary>
/// 登录窗体 — 角色选择 + 密码验证。
/// 角色 C（廖紫彤）负责开发。
/// 提交序号：#5
/// 功能：选择管理员/试验员 -> 输入密码 -> 调用 DbHelper 验证 -> 进入主界面
/// </summary>
public class LoginForm : Form
{
    // ========== UI 控件字段 ==========
    private RadioButton rbAdmin = null!;      // 管理员单选按钮
    private RadioButton rbOperator = null!;   // 试验员单选按钮
    private TextBox txtPwd = null!;           // 密码输入框（掩码显示）
    private Button btnLogin = null!;          // 登录按钮
    private Label lblError = null!;           // 错误提示标签（红色）

    /// <summary> 登录成功后的用户 ID（如 "1"），供外部（MainForm）读取 </summary>
    public string LoggedInUserId { get; private set; } = string.Empty;

    /// <summary> 登录成功后的角色（admin / operator），供外部（MainForm）读取 </summary>
    public string LoggedInUserType { get; private set; } = string.Empty;

    public LoginForm()
    {
        // 窗口基础属性：固定大小、居中、不可最大化
        Text = "ISO 11820 建筑材料不燃性试验系统 — 登录";
        Size = new Size(480, 380);  // 稍微加宽窗口，容纳完整文字
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        BuildUi();
    }

    /// <summary>
    /// 构建登录窗体的所有控件布局。
    /// </summary>
    private void BuildUi()
    {
        // -------- 标题 --------
        var lblTitle = new Label
        {
            Text = "ISO 11820 建筑材料不燃性试验系统",
            Font = new Font("微软雅黑", 13, FontStyle.Bold),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(20, 25),
            Size = new Size(420, 40)
        };

        // -------- 角色选择（默认选中“管理员”） --------
        // 使用固定宽度 80，避免遮挡后面的单选按钮
        var lblRole = new Label
        {
            Text = "角色：",
            Location = new Point(50, 95),
            Size = new Size(80, 25),
            Font = new Font("微软雅黑", 10),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // 宽度加宽到 120，确保"管理员"3个字完整显示
        rbAdmin = new RadioButton
        {
            Text = "管理员",
            Location = new Point(140, 95),
            Size = new Size(120, 25),
            Font = new Font("微软雅黑", 10),
            Checked = true
        };

        rbOperator = new RadioButton
        {
            Text = "试验员",
            Location = new Point(270, 95),
            Size = new Size(120, 25),
            Font = new Font("微软雅黑", 10)
        };

        // -------- 密码输入（掩码显示） --------
        var lblPwd = new Label
        {
            Text = "密码：",
            Location = new Point(50, 150),
            Size = new Size(80, 25),
            Font = new Font("微软雅黑", 10),
            TextAlign = ContentAlignment.MiddleLeft
        };

        txtPwd = new TextBox
        {
            Location = new Point(140, 148),
            Size = new Size(220, 25),
            PasswordChar = '*',
            Font = new Font("微软雅黑", 10)
        };
        txtPwd.KeyDown += TxtPwd_KeyDown;

        // -------- 错误提示（默认隐藏） --------
        lblError = new Label
        {
            Text = string.Empty,
            ForeColor = Color.Red,
            Location = new Point(50, 195),
            Size = new Size(370, 25),
            Font = new Font("微软雅黑", 9),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // -------- 登录按钮 --------
        btnLogin = new Button
        {
            Text = "登录",
            Location = new Point(150, 240),
            Size = new Size(150, 40),
            Font = new Font("微软雅黑", 11),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += BtnLogin_Click;

        // -------- 退出按钮（按 ESC 可退出） --------
        var btnCancel = new Button
        {
            Text = "退出",
            Location = new Point(320, 240),
            Size = new Size(90, 40),
            Font = new Font("微软雅黑", 11),
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.Click += (s, e) => Application.Exit();

        // -------- 将所有控件添加到窗体 --------
        Controls.Add(lblTitle);
        Controls.Add(lblRole);
        Controls.Add(rbAdmin);
        Controls.Add(rbOperator);
        Controls.Add(lblPwd);
        Controls.Add(txtPwd);
        Controls.Add(lblError);
        Controls.Add(btnLogin);
        Controls.Add(btnCancel);

        // 设置默认按钮：按 Enter 键触发登录，按 ESC 键触发退出
        AcceptButton = btnLogin;
        CancelButton = btnCancel;
    }

    /// <summary>
    /// 密码框按下回车键时，直接触发登录逻辑（提升操作效率）。
    /// </summary>
    private void TxtPwd_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            BtnLogin_Click(sender, e);
        }
    }

    /// <summary>
    /// 登录按钮点击事件处理。
    /// </summary>
    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        lblError.Text = string.Empty;

        if (AppGlobal.Instance == null || string.IsNullOrEmpty(AppGlobal.Instance.DbPath))
        {
            lblError.Text = "系统初始化失败，请重新启动程序";
            return;
        }

        string username = rbAdmin.Checked ? "admin" : "experimenter";
        string pwd = txtPwd.Text;

        if (string.IsNullOrWhiteSpace(pwd))
        {
            lblError.Text = "请输入密码";
            return;
        }

        try
        {
            var dbHelper = new DbHelper(AppGlobal.Instance.DbPath);
            bool success = dbHelper.Login(username, pwd, out string userid, out string usertype);

            if (success)
            {
                LoggedInUserId = userid;
                LoggedInUserType = usertype;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                lblError.Text = "密码错误，请重新输入";
                txtPwd.Clear();
                txtPwd.Focus();
            }
        }
        catch (Exception ex)
        {
            lblError.Text = "登录失败：" + ex.Message;
        }
    }
}