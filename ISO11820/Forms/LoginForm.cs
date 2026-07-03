using ISO11820.Data;

namespace ISO11820.Forms;

/// <summary>
/// 登录窗体 — 角色选择 + 密码验证。
/// 廖紫彤同学负责美化界面，当前为功能骨架。
/// </summary>
public class LoginForm : Form
{
    private RadioButton _rbAdmin;
    private RadioButton _rbExperimenter;
    private TextBox _txtPassword;
    private Button _btnLogin;
    private Label _lblTitle;
    private Label _lblError = null!;

    private readonly DbHelper _db;

    /// <summary>登录成功后的用户名</summary>
    public string LoggedInUser { get; private set; } = string.Empty;

    /// <summary>登录成功后的角色</summary>
    public string LoggedInRole { get; private set; } = string.Empty;

    public LoginForm(DbHelper db)
    {
        _db = db;

        Text = "ISO 11820 建筑材料不燃性试验系统 — 登录";
        Size = new Size(450, 350);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        InitializeComponent();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        // 标题
        _lblTitle = new Label
        {
            Text = "ISO 11820 建筑材料不燃性试验系统",
            Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Size = new Size(400, 40),
            Location = new Point(25, 30)
        };

        // 角色选择
        var lblRole = new Label
        {
            Text = "选择角色：",
            Location = new Point(100, 90),
            AutoSize = true
        };

        _rbAdmin = new RadioButton
        {
            Text = "管理员 (admin)",
            Location = new Point(100, 115),
            Checked = true,
            AutoSize = true
        };

        _rbExperimenter = new RadioButton
        {
            Text = "试验员 (experimenter)",
            Location = new Point(230, 115),
            AutoSize = true
        };

        // 密码
        var lblPwd = new Label
        {
            Text = "输入密码：",
            Location = new Point(100, 155),
            AutoSize = true
        };

        _txtPassword = new TextBox
        {
            Location = new Point(100, 180),
            Width = 220,
            PasswordChar = '*',
            Font = new Font("Consolas", 12)
        };
        _txtPassword.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter) BtnLogin_Click(this, EventArgs.Empty);
        };

        // 登录按钮
        _btnLogin = new Button
        {
            Text = "登 录",
            Location = new Point(100, 225),
            Width = 220,
            Height = 35,
            Font = new Font("Microsoft YaHei", 10)
        };
        _btnLogin.Click += BtnLogin_Click;

        // 错误提示
        _lblError = new Label
        {
            Location = new Point(100, 270),
            Width = 220,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Red,
            AutoSize = false
        };

        Controls.AddRange(new Control[]
        {
            _lblTitle, lblRole, _rbAdmin, _rbExperimenter,
            lblPwd, _txtPassword, _btnLogin, _lblError
        });

        ResumeLayout(false);
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        var username = _rbAdmin.Checked ? "admin" : "experimenter";
        var password = _txtPassword.Text;

        if (string.IsNullOrEmpty(password))
        {
            _lblError.Text = "请输入密码";
            return;
        }

        if (_db.Login(username, password, out var userid, out var usertype))
        {
            LoggedInUser = username;
            LoggedInRole = usertype;
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            _lblError.Text = "密码错误，请重新输入";
            _txtPassword.SelectAll();
            _txtPassword.Focus();
        }
    }
}