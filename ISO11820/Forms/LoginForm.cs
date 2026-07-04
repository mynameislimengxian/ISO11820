using System;
using System.Drawing;
using System.Windows.Forms;
using ISO11820.Core;
using ISO11820.Data;

namespace ISO11820.Forms;

/// <summary>
/// 登录窗体 — 角色选择 + 密码验证。
/// 角色 C 负责。提交序号：#5
/// </summary>
public class LoginForm : Form
{
    // ========== UI 控件字段 ==========
    private RadioButton rbAdmin = null!;
    private RadioButton rbOperator = null!;
    private TextBox txtPwd = null!;
    private Button btnLogin = null!;
    private Label lblError = null!;

    private readonly DbHelper? _dbHelper;   // 可选的数据库帮助类实例

    /// <summary> 登录成功后的用户 ID（如 "1"） </summary>
    public string LoggedInUserId { get; private set; } = string.Empty;

    /// <summary> 登录成功后的用户名（admin / experimenter） </summary>
    public string LoggedInUser { get; private set; } = string.Empty;

    /// <summary> 登录成功后的角色（admin / operator） </summary>
    public string LoggedInRole { get; private set; } = string.Empty;

    // ========== 构造函数 ==========

    // 无参构造（兼容旧用法）
    public LoginForm() : this((DbHelper?)null) { }

    // 带 DbHelper 的构造
    public LoginForm(DbHelper? dbHelper)
    {
        _dbHelper = dbHelper;
        InitializeForm();
        BuildUi();
    }

    // 带数据库路径字符串的构造
    public LoginForm(string dbPath) : this(new DbHelper(dbPath)) { }

    // ========== 初始化窗体 ==========
    private void InitializeForm()
    {
        Text = "ISO 11820 建筑材料不燃性试验系统 — 登录";
        Size = new Size(480, 380);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
    }

    // ========== 构建 UI ==========
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

        // -------- 角色选择 --------
        var lblRole = new Label
        {
            Text = "角色：",
            Location = new Point(50, 95),
            Size = new Size(80, 25),
            Font = new Font("微软雅黑", 10),
            TextAlign = ContentAlignment.MiddleLeft
        };

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

        // -------- 密码输入 --------
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

        // -------- 错误提示 --------
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

        // -------- 退出按钮 --------
        var btnCancel = new Button
        {
            Text = "退出",
            Location = new Point(320, 240),
            Size = new Size(90, 40),
            Font = new Font("微软雅黑", 11),
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.Click += (s, e) => Application.Exit();

        // -------- 添加控件 --------
        Controls.Add(lblTitle);
        Controls.Add(lblRole);
        Controls.Add(rbAdmin);
        Controls.Add(rbOperator);
        Controls.Add(lblPwd);
        Controls.Add(txtPwd);
        Controls.Add(lblError);
        Controls.Add(btnLogin);
        Controls.Add(btnCancel);

        AcceptButton = btnLogin;
        CancelButton = btnCancel;
    }

    private void TxtPwd_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            BtnLogin_Click(sender, e);
        }
    }

    // ==================== 登录逻辑 ====================
    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        lblError.Text = string.Empty;

        // 优先使用传入的 DbHelper，否则从 AppGlobal 创建
        DbHelper dbHelper = _dbHelper ?? new DbHelper(AppGlobal.Instance.DbFullPath);

        string username = rbAdmin.Checked ? "admin" : "experimenter";
        string pwd = txtPwd.Text;

        if (string.IsNullOrWhiteSpace(pwd))
        {
            lblError.Text = "请输入密码";
            return;
        }

        try
        {
            bool success = dbHelper.Login(username, pwd, out string userid, out string usertype);
            if (success)
            {
                LoggedInUserId = userid;
                LoggedInUser = username;
                LoggedInRole = usertype;
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