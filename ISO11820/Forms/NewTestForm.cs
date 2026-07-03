using System;
using System.Drawing;
using System.Windows.Forms;
using ISO11820.Core;
using ISO11820.Data;
using Microsoft.Data.Sqlite;

namespace ISO11820.Forms;
//1
public class NewTestForm : Form
{
    private TextBox txtAmbTemp = null!;
    private TextBox txtAmbHumi = null!;
    private TextBox txtProductId = null!;
    private TextBox txtProductName = null!;
    private TextBox txtSpecification = null!;
    private TextBox txtDiameter = null!;
    private TextBox txtHeight = null!;
    private TextBox txtPreWeight = null!;
    private ComboBox cboOperator = null!;
    private ComboBox cboTestMode = null!;
    private NumericUpDown nudCustomDuration = null!;
    private Label lblDeviceId = null!;
    private Label lblDeviceName = null!;
    private Label lblCalibrationDate = null!;
    private Label lblConstPower = null!;
    private Button btnCreate = null!;
    private Button btnCancel = null!;
    private Label lblError = null!;

    private readonly TestController _controller;

    public NewTestForm(TestController controller)
    {
        _controller = controller;
        InitializeForm();
        BuildUi();
        LoadDeviceInfo();
        LoadOperators();
    }

    private void InitializeForm()
    {
        Text = "新建试验 — ISO 11820";
        Size = new Size(620, 680);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
    }

    private void BuildUi()
    {
        int y = 15;
        int labelWidth = 110;
        int rowHeight = 32;

        // ===== 环境信息 =====
        var grpEnvironment = new GroupBox
        {
            Text = "环境信息",
            Location = new Point(15, y),
            Size = new Size(570, 75)
        };
        y += 85;

        var lblAmbTemp = new Label
        {
            Text = "环境温度 (°C)：",
            Location = new Point(20, 28),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        txtAmbTemp = new TextBox
        {
            Location = new Point(140, 26),
            Size = new Size(120, 25),
            Text = "25.0"
        };

        var lblAmbHumi = new Label
        {
            Text = "环境湿度 (%)：",
            Location = new Point(300, 28),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        txtAmbHumi = new TextBox
        {
            Location = new Point(420, 26),
            Size = new Size(120, 25),
            Text = "60.0"
        };

        grpEnvironment.Controls.Add(lblAmbTemp);
        grpEnvironment.Controls.Add(txtAmbTemp);
        grpEnvironment.Controls.Add(lblAmbHumi);
        grpEnvironment.Controls.Add(txtAmbHumi);
        Controls.Add(grpEnvironment);

        // ===== 样品信息 =====
        var grpSample = new GroupBox
        {
            Text = "样品信息",
            Location = new Point(15, y),
            Size = new Size(570, 205)
        };
        y += 215;

        int rowY = 28;

        var lblProductId = new Label
        {
            Text = "样品编号*：",
            Location = new Point(20, rowY),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        txtProductId = new TextBox
        {
            Location = new Point(140, rowY),
            Size = new Size(240, 25),
            Text = "TEST-001"
        };
        rowY += rowHeight;

        var lblProductName = new Label
        {
            Text = "样品名称*：",
            Location = new Point(20, rowY),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        txtProductName = new TextBox
        {
            Location = new Point(140, rowY),
            Size = new Size(240, 25),
            Text = "石膏板"
        };
        rowY += rowHeight;

        var lblSpecification = new Label
        {
            Text = "规格：",
            Location = new Point(20, rowY),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        txtSpecification = new TextBox
        {
            Location = new Point(140, rowY),
            Size = new Size(240, 25)
        };
        rowY += rowHeight;

        var lblDiameter = new Label
        {
            Text = "直径 (mm)*：",
            Location = new Point(20, rowY),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        txtDiameter = new TextBox
        {
            Location = new Point(140, rowY),
            Size = new Size(100, 25),
            Text = "50.0"
        };

        var lblHeight = new Label
        {
            Text = "高度 (mm)*：",
            Location = new Point(280, rowY),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        txtHeight = new TextBox
        {
            Location = new Point(400, rowY),
            Size = new Size(100, 25),
            Text = "60.0"
        };
        rowY += rowHeight;

        var lblPreWeight = new Label
        {
            Text = "试验前质量 (g)*：",
            Location = new Point(20, rowY),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        txtPreWeight = new TextBox
        {
            Location = new Point(140, rowY),
            Size = new Size(240, 25),
            Text = "100.0"
        };

        grpSample.Controls.Add(lblProductId);
        grpSample.Controls.Add(txtProductId);
        grpSample.Controls.Add(lblProductName);
        grpSample.Controls.Add(txtSpecification);
        grpSample.Controls.Add(lblDiameter);
        grpSample.Controls.Add(txtDiameter);
        grpSample.Controls.Add(lblHeight);
        grpSample.Controls.Add(txtHeight);
        grpSample.Controls.Add(lblPreWeight);
        grpSample.Controls.Add(txtPreWeight);
        Controls.Add(grpSample);

        // ===== 试验参数 =====
        var grpParams = new GroupBox
        {
            Text = "试验参数",
            Location = new Point(15, y),
            Size = new Size(570, 85)
        };
        y += 95;

        rowY = 28;

        var lblOperator = new Label
        {
            Text = "操作员*：",
            Location = new Point(20, rowY),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        cboOperator = new ComboBox
        {
            Location = new Point(140, rowY),
            Size = new Size(150, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        rowY += rowHeight;

        var lblTestMode = new Label
        {
            Text = "试验模式：",
            Location = new Point(20, rowY),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        cboTestMode = new ComboBox
        {
            Location = new Point(140, rowY),
            Size = new Size(150, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboTestMode.Items.Add("标准60分钟");
        cboTestMode.Items.Add("固定时长");
        cboTestMode.SelectedIndex = 0;
        cboTestMode.SelectedIndexChanged += (s, e) =>
        {
            nudCustomDuration.Enabled = cboTestMode.SelectedIndex == 1;
        };

        var lblCustomDuration = new Label
        {
            Text = "时长 (秒)：",
            Location = new Point(310, rowY),
            Size = new Size(80, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        nudCustomDuration = new NumericUpDown
        {
            Location = new Point(400, rowY),
            Size = new Size(100, 25),
            Minimum = 60,
            Maximum = 7200,
            Value = 600,
            Enabled = false
        };

        grpParams.Controls.Add(lblOperator);
        grpParams.Controls.Add(cboOperator);
        grpParams.Controls.Add(lblTestMode);
        grpParams.Controls.Add(cboTestMode);
        grpParams.Controls.Add(lblCustomDuration);
        grpParams.Controls.Add(nudCustomDuration);
        Controls.Add(grpParams);

        // ===== 设备信息 =====
        var grpDevice = new GroupBox
        {
            Text = "设备信息（自动读取）",
            Location = new Point(15, y),
            Size = new Size(570, 85)
        };
        y += 95;

        rowY = 28;

        var lblDeviceIdTitle = new Label
        {
            Text = "设备编号：",
            Location = new Point(20, rowY),
            Size = new Size(80, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        lblDeviceId = new Label
        {
            Text = "--",
            Location = new Point(110, rowY),
            Size = new Size(130, 25),
            ForeColor = Color.Blue
        };

        var lblDeviceNameTitle = new Label
        {
            Text = "设备名称：",
            Location = new Point(260, rowY),
            Size = new Size(80, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        lblDeviceName = new Label
        {
            Text = "--",
            Location = new Point(350, rowY),
            Size = new Size(180, 25),
            ForeColor = Color.Blue
        };
        rowY += rowHeight;

        var lblCalDateTitle = new Label
        {
            Text = "检定日期：",
            Location = new Point(20, rowY),
            Size = new Size(80, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        lblCalibrationDate = new Label
        {
            Text = "--",
            Location = new Point(110, rowY),
            Size = new Size(130, 25),
            ForeColor = Color.Blue
        };

        var lblConstPowerTitle = new Label
        {
            Text = "恒功率值：",
            Location = new Point(260, rowY),
            Size = new Size(80, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        lblConstPower = new Label
        {
            Text = "--",
            Location = new Point(350, rowY),
            Size = new Size(180, 25),
            ForeColor = Color.Blue
        };

        grpDevice.Controls.Add(lblDeviceIdTitle);
        grpDevice.Controls.Add(lblDeviceId);
        grpDevice.Controls.Add(lblDeviceNameTitle);
        grpDevice.Controls.Add(lblDeviceName);
        grpDevice.Controls.Add(lblCalDateTitle);
        grpDevice.Controls.Add(lblCalibrationDate);
        grpDevice.Controls.Add(lblConstPowerTitle);
        grpDevice.Controls.Add(lblConstPower);
        Controls.Add(grpDevice);

        // ===== 错误提示 =====
        lblError = new Label
        {
            Text = "",
            ForeColor = Color.Red,
            Location = new Point(15, y),
            Size = new Size(570, 28),
            Font = new Font("微软雅黑", 9),
            TextAlign = ContentAlignment.MiddleCenter
        };
        y += 35;

        // ===== 按钮 =====
        btnCreate = new Button
        {
            Text = "创建试验",
            Location = new Point(150, y),
            Size = new Size(140, 42),
            Font = new Font("微软雅黑", 11, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnCreate.FlatAppearance.BorderSize = 0;
        btnCreate.Click += BtnCreate_Click;

        btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(320, y),
            Size = new Size(120, 42),
            Font = new Font("微软雅黑", 11),
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

        Controls.Add(btnCreate);
        Controls.Add(btnCancel);

        // ===== ✅ 关键修复：把按钮提到最上层 =====
        btnCreate.BringToFront();
        btnCancel.BringToFront();
    }

    private void LoadDeviceInfo()
    {
        try
        {
            string dbPath = AppGlobal.Instance.DbPath;
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            string sql = "SELECT apparatusid, apparatusname, calibrationdate, constpower FROM apparatus WHERE apparatusid = 'FURNACE-01'";
            using var cmd = new SqliteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                lblDeviceId.Text = reader.GetString(0);
                lblDeviceName.Text = reader.GetString(1);
                lblCalibrationDate.Text = reader.GetString(2);
                lblConstPower.Text = reader.GetInt32(3).ToString();
            }
        }
        catch
        {
            lblDeviceId.Text = "FURNACE-01";
            lblDeviceName.Text = "一号试验炉";
            lblCalibrationDate.Text = "2026-01-01";
            lblConstPower.Text = "2048";
        }
    }

    private void LoadOperators()
    {
        try
        {
            string dbPath = AppGlobal.Instance.DbPath;
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            string sql = "SELECT username FROM operators";
            using var cmd = new SqliteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            cboOperator.Items.Clear();
            while (reader.Read()) cboOperator.Items.Add(reader.GetString(0));
            if (cboOperator.Items.Count > 0) cboOperator.SelectedIndex = 0;
        }
        catch
        {
            cboOperator.Items.Add("admin");
            cboOperator.SelectedIndex = 0;
        }
    }

    private void BtnCreate_Click(object? sender, EventArgs e)
    {
        lblError.Text = "";

        if (string.IsNullOrWhiteSpace(txtProductId.Text))
        {
            lblError.Text = "样品编号不能为空";
            txtProductId.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(txtProductName.Text))
        {
            lblError.Text = "样品名称不能为空";
            txtProductName.Focus();
            return;
        }
        if (!double.TryParse(txtPreWeight.Text, out double preWeight) || preWeight <= 0)
        {
            lblError.Text = "请输入有效质量";
            txtPreWeight.Focus();
            return;
        }

        string testId = DateTime.Now.ToString("yyyyMMdd-HHmmss");

        try
        {
            var dbHelper = new DbHelper(AppGlobal.Instance.DbPath);
            var test = new TestMaster
            {
                ProductId = txtProductId.Text,
                TestId = testId,
                TestDate = DateTime.Now,
                AmbTemp = double.TryParse(txtAmbTemp.Text, out var at) ? at : 25,
                AmbHumi = double.TryParse(txtAmbHumi.Text, out var ah) ? ah : 60,
                According = "ISO 11820",
                Operator = "admin",
                ApparatusId = "FURNACE-01",
                ApparatusName = "一号试验炉",
                ApparatusChkDate = DateTime.Now,
                RptNo = "",
                PreWeight = preWeight,
                PostWeight = 0,
                LostWeight = 0,
                LostWeightPer = 0,
                TotalTestTime = 0,
                ConstPower = 2048,
                PhenoCode = "",
                FlameTime = 0,
                FlameDuration = 0,
                Memo = "",
                Flag = null
            };

            _controller.SetTestMode(TestMode.Standard60Min, 3600);
            dbHelper.InsertTest(test);
            _controller.CurrentProductId = txtProductId.Text;
            _controller.CurrentTestId = testId;
            _controller.CurrentPreWeight = preWeight;

            MessageBox.Show($"试验创建成功！\n样品编号：{txtProductId.Text}",
                "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            lblError.Text = $"创建失败：{ex.Message}";
        }
    }
}