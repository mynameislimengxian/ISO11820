using ISO11820.Core;
using ISO11820.Data;

namespace ISO11820.Forms;

/// <summary>
/// 登录窗体 — 角色选择 + 密码验证。
/// 角色 C 负责。
/// </summary>
public class LoginForm : Form
{
    private RadioButton rbAdmin = null!;
    private RadioButton rbOperator = null!;
    private TextBox txtPwd = null!;
    private Button btnLogin = null!;
    private Label lblError = null!;

    /// <summary> 登录成功后的用户 ID，供外部读取 </summary>
    public string LoggedInUserId { get; private set; } = string.Empty;

    /// <summary> 登录成功后的角色（admin / operator） </summary>
    public string LoggedInUserType { get; private set; } = string.Empty;

    public LoginForm()
    {
        Text = "ISO 11820 建筑材料不燃性试验系统 — 登录";
        Size = new Size(450, 350);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        BuildUi();
    }

    private void BuildUi()
    {
        var lblTitle = new Label
        {
            Text = "ISO 11820 建筑材料不燃性试验系统",
            Font = new Font("微软雅黑", 13, FontStyle.Bold),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(20, 25),
            Size = new Size(390, 40)
        };

        var lblRole = new Label
        {
            Text = "角色：",
            Location = new Point(60, 90),
            Size = new Size(60, 25),
            Font = new Font("微软雅黑", 10)
        };

        rbAdmin = new RadioButton
        {
            Text = "管理员",
            Location = new Point(130, 90),
            Size = new Size(90, 25),
            Font = new Font("微软雅黑", 10),
            Checked = true
        };

        rbOperator = new RadioButton
        {
            Text = "试验员",
            Location = new Point(240, 90),
            Size = new Size(90, 25),
            Font = new Font("微软雅黑", 10)
        };

        var lblPwd = new Label
        {
            Text = "密码：",
            Location = new Point(60, 150),
            Size = new Size(60, 25),
            Font = new Font("微软雅黑", 10)
        };

        txtPwd = new TextBox
        {
            Location = new Point(130, 148),
            Size = new Size(200, 25),
            PasswordChar = '*',
            Font = new Font("微软雅黑", 10)
        };
        txtPwd.KeyDown += TxtPwd_KeyDown;

        lblError = new Label
        {
            Text = string.Empty,
            ForeColor = Color.Red,
            Location = new Point(60, 190),
            Size = new Size(320, 25),
            Font = new Font("微软雅黑", 9),
            TextAlign = ContentAlignment.MiddleCenter
        };

        btnLogin = new Button
        {
            Text = "登录",
            Location = new Point(150, 230),
            Size = new Size(140, 40),
            Font = new Font("微软雅黑", 11),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += BtnLogin_Click;

        Controls.Add(lblTitle);
        Controls.Add(lblRole);
        Controls.Add(rbAdmin);
        Controls.Add(rbOperator);
        Controls.Add(lblPwd);
        Controls.Add(txtPwd);
        Controls.Add(lblError);
        Controls.Add(btnLogin);

        AcceptButton = btnLogin;
    }

    private void TxtPwd_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            BtnLogin_Click(sender, e);
        }
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        lblError.Text = string.Empty;

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
