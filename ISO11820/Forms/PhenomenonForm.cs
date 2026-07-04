using System;
using System.Drawing;
using System.Windows.Forms;
using ISO11820.Core;
using ISO11820.Data;

namespace ISO11820.Forms;

/// <summary>
/// 试验现象记录窗体 — 火焰观察 + 试验后质量 + 自动计算失重率和温升
/// 角色 C 负责。提交序号：#11
/// </summary>
public class PhenomenonForm : Form
{
    // ========== 控件字段 ==========
    private CheckBox chkFlame = null!;
    private NumericUpDown nudFlameTime = null!;
    private NumericUpDown nudFlameDuration = null!;
    private TextBox txtPostWeight = null!;
    private TextBox txtRemark = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;
    private Label lblError = null!;

    private Label lblLostWeight = null!;
    private Label lblLostWeightPer = null!;
    private Label lblTempRise = null!;

    private readonly TestController _controller;
    private readonly string _productId;
    private readonly string _testId;
    private readonly double _preWeight;
    private readonly double _initialTf1;
    private readonly double _initialTf2;
    private readonly double _initialTs;
    private readonly double _initialTc;
    private readonly DbHelper _dbHelper;

    public PhenomenonForm(TestController controller, DbHelper dbHelper, string productId, string testId, double preWeight,
        double initialTf1, double initialTf2, double initialTs, double initialTc)
    {
        _controller = controller;
        _dbHelper = dbHelper;
        _productId = productId;
        _testId = testId;
        _preWeight = preWeight;
        _initialTf1 = initialTf1;
        _initialTf2 = initialTf2;
        _initialTs = initialTs;
        _initialTc = initialTc;

        InitializeForm();
        BuildUi();
    }

    private void InitializeForm()
    {
        Text = "试验现象记录 — ISO 11820";
        Size = new Size(650, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScroll = true;
    }

    private void BuildUi()
    {
        int y = 20;
        int labelWidth = 140;
        int controlWidth = 260;

        // ===== 火焰观察 =====
        chkFlame = new CheckBox
        {
            Text = "是否出现持续火焰",
            Location = new Point(30, y),
            Size = new Size(180, 30),
            Font = new Font("微软雅黑", 10)
        };
        chkFlame.CheckedChanged += (s, e) =>
        {
            nudFlameTime.Enabled = chkFlame.Checked;
            nudFlameDuration.Enabled = chkFlame.Checked;
            if (!chkFlame.Checked)
            {
                nudFlameTime.Value = 0;
                nudFlameDuration.Value = 0;
            }
        };
        y += 35;

        var lblFlameTime = new Label
        {
            Text = "火焰发生时刻 (秒)：",
            Location = new Point(30, y),
            Size = new Size(labelWidth, 25),
            Font = new Font("微软雅黑", 9)
        };
        nudFlameTime = new NumericUpDown
        {
            Location = new Point(180, y),
            Size = new Size(120, 25),
            Minimum = 0,
            Maximum = 7200,
            Value = 0,
            Enabled = false
        };
        y += 35;

        var lblFlameDuration = new Label
        {
            Text = "火焰持续时间 (秒)：",
            Location = new Point(30, y),
            Size = new Size(labelWidth, 25),
            Font = new Font("微软雅黑", 9)
        };
        nudFlameDuration = new NumericUpDown
        {
            Location = new Point(180, y),
            Size = new Size(120, 25),
            Minimum = 0,
            Maximum = 7200,
            Value = 0,
            Enabled = false
        };
        y += 35;

        // ===== 试验后质量（必填） =====
        var lblPostWeight = new Label
        {
            Text = "试验后质量 (g)*：",
            Location = new Point(30, y),
            Size = new Size(labelWidth, 25),
            Font = new Font("微软雅黑", 9)
        };
        txtPostWeight = new TextBox
        {
            Location = new Point(180, y),
            Size = new Size(controlWidth, 25),
            Font = new Font("微软雅黑", 10)
        };
        txtPostWeight.TextChanged += (s, e) => CalculateResults();
        y += 35;

        // ===== 计算结果显示 =====
        var grpResult = new GroupBox
        {
            Text = "自动计算结果",
            Location = new Point(30, y),
            Size = new Size(420, 90)
        };
        y += 70;

        int rY = 22;
        var lblLostWeightTitle = new Label
        {
            Text = "失重量 (g)：",
            Location = new Point(15, rY),
            Size = new Size(80, 25)
        };
        lblLostWeight = new Label
        {
            Text = "--",
            Location = new Point(100, rY),
            Size = new Size(80, 25),
            ForeColor = Color.Blue,
            Font = new Font("微软雅黑", 10, FontStyle.Bold)
        };

        var lblLostWeightPerTitle = new Label
        {
            Text = "失重率 (%)：",
            Location = new Point(200, rY),
            Size = new Size(80, 25)
        };
        lblLostWeightPer = new Label
        {
            Text = "--",
            Location = new Point(285, rY),
            Size = new Size(80, 25),
            ForeColor = Color.Blue,
            Font = new Font("微软雅黑", 10, FontStyle.Bold)
        };
        rY += 30;

        var lblTempRiseTitle = new Label
        {
            Text = "样品温升 (°C)：",
            Location = new Point(15, rY),
            Size = new Size(120, 25)
        };
        lblTempRise = new Label
        {
            Text = "--",
            Location = new Point(120, rY),
            Size = new Size(120, 25),
            ForeColor = Color.Blue,
            Font = new Font("微软雅黑", 10, FontStyle.Bold)
        };

        grpResult.Controls.Add(lblLostWeightTitle);
        grpResult.Controls.Add(lblLostWeight);
        grpResult.Controls.Add(lblLostWeightPerTitle);
        grpResult.Controls.Add(lblLostWeightPer);
        grpResult.Controls.Add(lblTempRiseTitle);
        grpResult.Controls.Add(lblTempRise);
        Controls.Add(grpResult);

        // ===== 备注 =====
        var lblRemark = new Label
        {
            Text = "备注：",
            Location = new Point(30, y),
            Size = new Size(60, 25),
            Font = new Font("微软雅黑", 9)
        };
        txtRemark = new TextBox
        {
            Location = new Point(100, y),
            Size = new Size(350, 25),
            Font = new Font("微软雅黑", 10)
        };
        y += 35;

        // ===== 错误提示 =====
        lblError = new Label
        {
            Text = "",
            ForeColor = Color.Red,
            Location = new Point(30, y),
            Size = new Size(420, 25),
            Font = new Font("微软雅黑", 9),
            TextAlign = ContentAlignment.MiddleCenter
        };
        y += 30;

        // ===== 按钮 =====
        btnSave = new Button
        {
            Text = "保存记录",
            Location = new Point(100, y),
            Size = new Size(130, 40),
            Font = new Font("微软雅黑", 11),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += BtnSave_Click;

        btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(260, y),
            Size = new Size(110, 40),
            Font = new Font("微软雅黑", 11),
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

        Controls.Add(chkFlame);
        Controls.Add(lblFlameTime);
        Controls.Add(nudFlameTime);
        Controls.Add(lblFlameDuration);
        Controls.Add(nudFlameDuration);
        Controls.Add(lblPostWeight);
        Controls.Add(txtPostWeight);
        Controls.Add(lblRemark);
        Controls.Add(txtRemark);
        Controls.Add(lblError);
        Controls.Add(btnSave);
        Controls.Add(btnCancel);
    }

    private void CalculateResults()
    {
        if (double.TryParse(txtPostWeight.Text, out double postWeight) && postWeight > 0 && postWeight <= _preWeight)
        {
            double lostWeight = _preWeight - postWeight;
            double lostWeightPer = (lostWeight / _preWeight) * 100;

            lblLostWeight.Text = $"{lostWeight:F2}";
            lblLostWeightPer.Text = $"{lostWeightPer:F2}";

            double tempRise = _controller.Ts - _initialTs;
            lblTempRise.Text = $"{tempRise:F2}";
        }
        else
        {
            lblLostWeight.Text = "--";
            lblLostWeightPer.Text = "--";
            lblTempRise.Text = "--";
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        lblError.Text = "";

        if (!double.TryParse(txtPostWeight.Text, out double postWeight) || postWeight <= 0)
        {
            lblError.Text = "请输入有效的试验后质量（正数）";
            txtPostWeight.Focus();
            return;
        }

        if (postWeight > _preWeight)
        {
            lblError.Text = $"试验后质量（{postWeight:F2}g）不能大于试验前质量（{_preWeight:F2}g）";
            txtPostWeight.Focus();
            return;
        }

        double lostWeight = _preWeight - postWeight;
        double lostWeightPer = (lostWeight / _preWeight) * 100;

        int flameTime = chkFlame.Checked ? (int)nudFlameTime.Value : 0;
        int flameDuration = chkFlame.Checked ? (int)nudFlameDuration.Value : 0;
        string phenocode = chkFlame.Checked ? "has_flame" : "no_flame";

        try
        {
            // 获取当前试验记录
            var dbHelper = _dbHelper;
            var existingTest = dbHelper.GetTest(_productId, _testId);

            if (existingTest == null)
            {
                lblError.Text = "找不到对应的试验记录";
                return;
            }

            // 更新字段
            existingTest.PostWeight = postWeight;
            existingTest.LostWeight = lostWeight;
            existingTest.LostWeightPer = lostWeightPer;
            existingTest.PhenoCode = phenocode;
            existingTest.FlameTime = flameTime;
            existingTest.FlameDuration = flameDuration;
            existingTest.Memo = txtRemark.Text;

            // 温升（使用当前最终温度 - 初始温度）
            // 从控制器获取当前温度
            // 温升 = 最终温度 - 记录开始时的初始温度
            existingTest.DeltaTf1 = _controller.Tf1 - _initialTf1;
            existingTest.DeltaTf2 = _controller.Tf2 - _initialTf2;
            existingTest.DeltaTf = (_controller.Tf1 - _initialTf1 + _controller.Tf2 - _initialTf2) / 2;
            existingTest.DeltaTs = _controller.Ts - _initialTs;
            existingTest.DeltaTc = _controller.Tc - _initialTc;
            existingTest.TotalTestTime = _controller.ElapsedSeconds;

            // 记录当前温度作为最终值
            existingTest.FinalTf1 = _controller.Tf1;
            existingTest.FinalTf2 = _controller.Tf2;
            existingTest.FinalTs = _controller.Ts;
            existingTest.FinalTc = _controller.Tc;
            existingTest.FinalTf1Time = _controller.ElapsedSeconds;
            existingTest.FinalTf2Time = _controller.ElapsedSeconds;
            existingTest.FinalTsTime = _controller.ElapsedSeconds;
            existingTest.FinalTcTime = _controller.ElapsedSeconds;

            // 最大值（如果当前温度更高则更新）
            existingTest.MaxTf1 = Math.Max(existingTest.MaxTf1, _controller.Tf1);
            existingTest.MaxTf2 = Math.Max(existingTest.MaxTf2, _controller.Tf2);
            existingTest.MaxTs = Math.Max(existingTest.MaxTs, _controller.Ts);
            existingTest.MaxTc = Math.Max(existingTest.MaxTc, _controller.Tc);

            dbHelper.UpdateTestResult(existingTest);

            // 标记已保存
            _controller.MarkSaved();

            MessageBox.Show("试验记录已保存！",
                "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            lblError.Text = $"保存失败：{ex.Message}";
        }
    }
}