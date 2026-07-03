using System.Text.Json;
using System.Text.Json.Nodes;
using ISO11820.Core;

namespace ISO11820.Forms;

/// <summary>
/// 参数设置窗体 — 允许用户在试验空闲时调整仿真参数和存储路径。
/// 修改后直接写回 appsettings.json，并通知 AppGlobal 重新加载配置。
/// 李孟鲜（A）负责。
/// </summary>
public class SettingsForm : Form
{
    // 仿真参数
    private NumericUpDown nudHeatingRate = null!;
    private NumericUpDown nudTargetTemp = null!;
    private NumericUpDown nudStableThreshold = null!;
    private NumericUpDown nudDriftThreshold = null!;
    private NumericUpDown nudFluctuation = null!;

    // 存储路径
    private TextBox txtBaseDirectory = null!;
    private TextBox txtReportDirectory = null!;

    // 按钮
    private Button btnSave = null!;
    private Button btnCancel = null!;
    private Label lblError = null!;

    public SettingsForm()
    {
        InitializeForm();
        BuildUi();
        LoadCurrentValues();
    }

    private void InitializeForm()
    {
        Text = "参数设置 — ISO 11820";
        Size = new Size(550, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private void BuildUi()
    {
        int y = 15;
        int labelWidth = 140;
        int fieldWidth = 160;
        int rowHeight = 32;

        // ===== 仿真参数 =====
        var grpSimulation = new GroupBox
        {
            Text = "仿真参数",
            Location = new Point(15, y),
            Size = new Size(505, 195)
        };
        y += 205;

        int rowY = 28;

        // 升温速率
        var lblHeatingRate = new Label
        {
            Text = "升温速率 (°C/s)：",
            Location = new Point(20, rowY),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        nudHeatingRate = new NumericUpDown
        {
            Location = new Point(170, rowY),
            Size = new Size(fieldWidth, 25),
            Minimum = 0.5m,
            Maximum = 100.0m,
            DecimalPlaces = 1,
            Increment = 1.0m
        };
        var lblHeatingRateHint = new Label
        {
            Text = "（建议 5.0，演示可用 40.0）",
            Location = new Point(340, rowY + 2),
            Size = new Size(200, 20),
            ForeColor = Color.Gray,
            Font = new Font("微软雅黑", 8)
        };
        rowY += rowHeight;

        // 目标炉温
        var lblTargetTemp = new Label
        {
            Text = "目标炉温 (°C)：",
            Location = new Point(20, rowY),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        nudTargetTemp = new NumericUpDown
        {
            Location = new Point(170, rowY),
            Size = new Size(fieldWidth, 25),
            Minimum = 100.0m,
            Maximum = 1200.0m,
            DecimalPlaces = 0,
            Increment = 10.0m,
            Value = 750
        };
        var lblTargetTempHint = new Label
        {
            Text = "（ISO 11820 标准：750°C）",
            Location = new Point(340, rowY + 2),
            Size = new Size(200, 20),
            ForeColor = Color.Gray,
            Font = new Font("微软雅黑", 8)
        };
        rowY += rowHeight;

        // 稳定阈值
        var lblStableThreshold = new Label
        {
            Text = "稳定阈值 (°C)：",
            Location = new Point(20, rowY),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        nudStableThreshold = new NumericUpDown
        {
            Location = new Point(170, rowY),
            Size = new Size(fieldWidth, 25),
            Minimum = 0.5m,
            Maximum = 20.0m,
            DecimalPlaces = 1,
            Increment = 0.5m,
            Value = 3.0m
        };
        var lblStableThresholdHint = new Label
        {
            Text = "（炉温在目标±此值内视为稳定）",
            Location = new Point(340, rowY + 2),
            Size = new Size(200, 20),
            ForeColor = Color.Gray,
            Font = new Font("微软雅黑", 8)
        };
        rowY += rowHeight;

        // 温漂阈值
        var lblDriftThreshold = new Label
        {
            Text = "温漂阈值 (°C/10min)：",
            Location = new Point(20, rowY),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        nudDriftThreshold = new NumericUpDown
        {
            Location = new Point(170, rowY),
            Size = new Size(fieldWidth, 25),
            Minimum = 0.1m,
            Maximum = 10.0m,
            DecimalPlaces = 1,
            Increment = 0.5m,
            Value = 2.0m
        };
        rowY += rowHeight;

        // 温度波动
        var lblFluctuation = new Label
        {
            Text = "温度波动幅度 (°C)：",
            Location = new Point(20, rowY),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        nudFluctuation = new NumericUpDown
        {
            Location = new Point(170, rowY),
            Size = new Size(fieldWidth, 25),
            Minimum = 0.1m,
            Maximum = 10.0m,
            DecimalPlaces = 1,
            Increment = 0.5m,
            Value = 0.5m
        };
        var lblFluctuationHint = new Label
        {
            Text = "（仿真随机噪声幅度）",
            Location = new Point(340, rowY + 2),
            Size = new Size(200, 20),
            ForeColor = Color.Gray,
            Font = new Font("微软雅黑", 8)
        };

        grpSimulation.Controls.Add(lblHeatingRate);
        grpSimulation.Controls.Add(nudHeatingRate);
        grpSimulation.Controls.Add(lblHeatingRateHint);
        grpSimulation.Controls.Add(lblTargetTemp);
        grpSimulation.Controls.Add(nudTargetTemp);
        grpSimulation.Controls.Add(lblTargetTempHint);
        grpSimulation.Controls.Add(lblStableThreshold);
        grpSimulation.Controls.Add(nudStableThreshold);
        grpSimulation.Controls.Add(lblStableThresholdHint);
        grpSimulation.Controls.Add(lblDriftThreshold);
        grpSimulation.Controls.Add(nudDriftThreshold);
        grpSimulation.Controls.Add(lblFluctuation);
        grpSimulation.Controls.Add(nudFluctuation);
        grpSimulation.Controls.Add(lblFluctuationHint);
        Controls.Add(grpSimulation);

        // ===== 存储路径 =====
        var grpStorage = new GroupBox
        {
            Text = "存储路径",
            Location = new Point(15, y),
            Size = new Size(505, 100)
        };
        y += 110;

        rowY = 28;

        var lblBaseDir = new Label
        {
            Text = "数据存储路径：",
            Location = new Point(20, rowY),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        // 路径选择面板
        var baseDirPanel = new Panel
        {
            Location = new Point(170, rowY),
            Size = new Size(310, 25)
        };
        txtBaseDirectory = new TextBox
        {
            Location = new Point(0, 0),
            Size = new Size(250, 25),
            Text = "D:\\ISO11820"
        };
        var btnBrowseBase = new Button
        {
            Text = "浏览...",
            Location = new Point(255, 0),
            Size = new Size(55, 25),
            Font = new Font("微软雅黑", 8)
        };
        btnBrowseBase.Click += (s, e) =>
        {
            using var dlg = new FolderBrowserDialog { Description = "选择数据存储目录" };
            if (dlg.ShowDialog() == DialogResult.OK)
                txtBaseDirectory.Text = dlg.SelectedPath;
        };
        baseDirPanel.Controls.Add(txtBaseDirectory);
        baseDirPanel.Controls.Add(btnBrowseBase);
        rowY += rowHeight;

        var lblReportDir = new Label
        {
            Text = "报告输出路径：",
            Location = new Point(20, rowY),
            Size = new Size(labelWidth, 25),
            TextAlign = ContentAlignment.MiddleRight
        };
        var reportDirPanel = new Panel
        {
            Location = new Point(170, rowY),
            Size = new Size(310, 25)
        };
        txtReportDirectory = new TextBox
        {
            Location = new Point(0, 0),
            Size = new Size(250, 25),
            Text = "D:\\ISO11820\\Reports"
        };
        var btnBrowseReport = new Button
        {
            Text = "浏览...",
            Location = new Point(255, 0),
            Size = new Size(55, 25),
            Font = new Font("微软雅黑", 8)
        };
        btnBrowseReport.Click += (s, e) =>
        {
            using var dlg = new FolderBrowserDialog { Description = "选择报告输出目录" };
            if (dlg.ShowDialog() == DialogResult.OK)
                txtReportDirectory.Text = dlg.SelectedPath;
        };
        reportDirPanel.Controls.Add(txtReportDirectory);
        reportDirPanel.Controls.Add(btnBrowseReport);

        grpStorage.Controls.Add(lblBaseDir);
        grpStorage.Controls.Add(baseDirPanel);
        grpStorage.Controls.Add(lblReportDir);
        grpStorage.Controls.Add(reportDirPanel);
        Controls.Add(grpStorage);

        // ===== 提示信息 =====
        var lblNote = new Label
        {
            Text = "⚠ 参数修改后立即生效，部分参数可能影响当前试验。建议在 Idle 状态下修改。",
            Location = new Point(15, y),
            Size = new Size(505, 35),
            ForeColor = Color.DarkOrange,
            Font = new Font("微软雅黑", 8)
        };
        y += 40;
        Controls.Add(lblNote);

        // ===== 错误提示 =====
        lblError = new Label
        {
            Location = new Point(15, y),
            Size = new Size(505, 25),
            ForeColor = Color.Red,
            Font = new Font("微软雅黑", 9),
            TextAlign = ContentAlignment.MiddleLeft
        };
        y += 30;
        Controls.Add(lblError);

        // ===== 按钮 =====
        btnSave = new Button
        {
            Text = "保存设置",
            Location = new Point(90, y),
            Size = new Size(140, 42),
            Font = new Font("微软雅黑", 11, FontStyle.Bold),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += BtnSave_Click;

        btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(280, y),
            Size = new Size(120, 42),
            Font = new Font("微软雅黑", 11),
            FlatStyle = FlatStyle.Flat
        };
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.Add(btnSave);
        Controls.Add(btnCancel);

        // 调整 AcceptButton/CancelButton（需要在控件创建后设置）
        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private void LoadCurrentValues()
    {
        var g = AppGlobal.Instance;
        nudHeatingRate.Value = (decimal)g.HeatingRatePerSecond;
        nudTargetTemp.Value = (decimal)g.TargetFurnaceTemp;
        nudStableThreshold.Value = (decimal)g.StableThreshold;
        nudDriftThreshold.Value = (decimal)g.MaxTemperatureDriftPerTenMinutes;
        nudFluctuation.Value = (decimal)g.TempFluctuation;
        txtBaseDirectory.Text = g.BaseDirectory;
        txtReportDirectory.Text = g.ReportsDirectory;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        lblError.Text = "";

        // 验证路径
        if (string.IsNullOrWhiteSpace(txtBaseDirectory.Text))
        {
            lblError.Text = "数据存储路径不能为空";
            txtBaseDirectory.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(txtReportDirectory.Text))
        {
            lblError.Text = "报告输出路径不能为空";
            txtReportDirectory.Focus();
            return;
        }

        try
        {
            // 确保目录存在
            Directory.CreateDirectory(txtBaseDirectory.Text);
            Directory.CreateDirectory(txtReportDirectory.Text);

            // 读取并更新 appsettings.json（使用 JsonNode 方便修改）
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            string json = File.ReadAllText(configPath);
            var root = JsonNode.Parse(json) ?? throw new InvalidOperationException("无法解析配置文件");

            // 更新 Simulation 节点
            var sim = root["Simulation"]!;
            sim["HeatingRatePerSecond"] = (double)nudHeatingRate.Value;
            sim["TargetFurnaceTemp"] = (double)nudTargetTemp.Value;
            sim["StableThreshold"] = (double)nudStableThreshold.Value;
            sim["MaxTemperatureDriftPerTenMinutes"] = (double)nudDriftThreshold.Value;
            sim["TempFluctuation"] = (double)nudFluctuation.Value;

            // 更新 FileStorage 节点
            var fs = root["FileStorage"]!;
            fs["BaseDirectory"] = txtBaseDirectory.Text;
            fs["TestDataDirectory"] = Path.Combine(txtBaseDirectory.Text, "TestData");

            // 更新 Report 节点
            var rpt = root["Report"]!;
            rpt["OutputDirectory"] = txtReportDirectory.Text;

            // 写回文件
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(configPath, root.ToJsonString(options));

            // 重新初始化 AppGlobal 配置
            AppGlobal.Instance.Initialize();

            MessageBox.Show("参数设置已保存并生效。", "保存成功",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            lblError.Text = $"保存失败：{ex.Message}";
        }
    }
}