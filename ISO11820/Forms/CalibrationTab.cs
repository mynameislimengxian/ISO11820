using ISO11820.Core;
using ISO11820.Data;
using System.Data;
using System.Text.Json;

namespace ISO11820.Forms;

/// <summary>
/// 设备校准 Tab — 实时显示校准温度、记录炉壁9点温度、计算均匀性偏差。
/// 作为 UserControl 嵌入到 MainForm 的 TabControl 中。
/// </summary>
public class CalibrationTab : UserControl
{
    private readonly DbHelper _db;
    private readonly TestController _controller;

    // 实时校准温显示
    private Label _lblTcalTitle;
    private Label _lblTcalValue;

    // 9 点温度输入
    private TextBox[] _tempInputs = new TextBox[9];
    private Label[] _inputLabels = new Label[9];

    // 计算结果标签
    private Label _lblTAvg;
    private Label _lblMaxDev;
    private Label _lblUniformResult;
    private Label _lblAxis1Avg;
    private Label _lblAxis2Avg;
    private Label _lblAxis3Avg;
    private Label _lblLevelAAvg;
    private Label _lblLevelBAvg;
    private Label _lblLevelCAvg;
    private Label _lblAxis1Dev;
    private Label _lblAxis2Dev;
    private Label _lblAxis3Dev;
    private Label _lblLevelADev;
    private Label _lblLevelBDev;
    private Label _lblLevelCDev;
    private Label _lblAxialDev;
    private Label _lblLevelDev;

    // 按钮
    private Button _btnCalculate;
    private Button _btnSave;
    private Button _btnQueryHistory;

    // 历史记录
    private DataGridView _dgvHistory;
    private Label _lblHistoryStatus;

    public CalibrationTab(DbHelper db, TestController controller)
    {
        _db = db;
        _controller = controller;
        _controller.DataBroadcast += OnDataBroadcast;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        // ===== 顶部：实时校准温度 =====
        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(30, 30, 30)
        };

        _lblTcalTitle = new Label
        {
            Text = "校准温度 (TCal)",
            ForeColor = Color.LightGray,
            Font = new Font("Microsoft YaHei", 10),
            Location = new Point(20, 8),
            AutoSize = true
        };

        _lblTcalValue = new Label
        {
            Text = "— °C",
            ForeColor = Color.Lime,
            Font = new Font("Consolas", 28, FontStyle.Bold),
            Location = new Point(20, 28),
            AutoSize = true
        };

        topPanel.Controls.Add(_lblTcalTitle);
        topPanel.Controls.Add(_lblTcalValue);

        // ===== 中部：9点温度输入 + 计算结果 =====
        var midPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 320,
            Padding = new Padding(10, 10, 10, 10),
            ColumnCount = 7,
            RowCount = 6
        };

        // 列宽
        midPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));  // 标签
        midPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));  // 输入框
        midPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        midPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        midPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20));  // 间距
        midPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); // 计算标签
        midPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // 行高
        for (int i = 0; i < 6; i++)
            midPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));

        // 标题行
        midPanel.Controls.Add(MakeLabel("", ContentAlignment.MiddleCenter), 0, 0);
        midPanel.Controls.Add(MakeLabel("轴1", ContentAlignment.MiddleCenter), 1, 0);
        midPanel.Controls.Add(MakeLabel("轴2", ContentAlignment.MiddleCenter), 2, 0);
        midPanel.Controls.Add(MakeLabel("轴3", ContentAlignment.MiddleCenter), 3, 0);

        // 轴线标签
        midPanel.Controls.Add(MakeLabel("轴向均值", ContentAlignment.MiddleLeft), 5, 0);
        midPanel.Controls.Add(MakeLabel("轴向偏差", ContentAlignment.MiddleLeft), 6, 0);

        // A 层
        var rowA = 1;
        midPanel.Controls.Add(MakeLabel("A层", ContentAlignment.MiddleCenter), 0, rowA);
        _tempInputs[0] = MakeTempInput(); midPanel.Controls.Add(_tempInputs[0], 1, rowA);
        _tempInputs[1] = MakeTempInput(); midPanel.Controls.Add(_tempInputs[1], 2, rowA);
        _tempInputs[2] = MakeTempInput(); midPanel.Controls.Add(_tempInputs[2], 3, rowA);

        _lblAxis1Avg = MakeResultLabel(); midPanel.Controls.Add(_lblAxis1Avg, 5, rowA);
        _lblAxis1Dev = MakeResultLabel(); midPanel.Controls.Add(_lblAxis1Dev, 6, rowA);

        // B 层
        var rowB = 2;
        midPanel.Controls.Add(MakeLabel("B层", ContentAlignment.MiddleCenter), 0, rowB);
        _tempInputs[3] = MakeTempInput(); midPanel.Controls.Add(_tempInputs[3], 1, rowB);
        _tempInputs[4] = MakeTempInput(); midPanel.Controls.Add(_tempInputs[4], 2, rowB);
        _tempInputs[5] = MakeTempInput(); midPanel.Controls.Add(_tempInputs[5], 3, rowB);

        _lblAxis2Avg = MakeResultLabel(); midPanel.Controls.Add(_lblAxis2Avg, 5, rowB);
        _lblAxis2Dev = MakeResultLabel(); midPanel.Controls.Add(_lblAxis2Dev, 6, rowB);

        // C 层
        var rowC = 3;
        midPanel.Controls.Add(MakeLabel("C层", ContentAlignment.MiddleCenter), 0, rowC);
        _tempInputs[6] = MakeTempInput(); midPanel.Controls.Add(_tempInputs[6], 1, rowC);
        _tempInputs[7] = MakeTempInput(); midPanel.Controls.Add(_tempInputs[7], 2, rowC);
        _tempInputs[8] = MakeTempInput(); midPanel.Controls.Add(_tempInputs[8], 3, rowC);

        _lblAxis3Avg = MakeResultLabel(); midPanel.Controls.Add(_lblAxis3Avg, 5, rowC);
        _lblAxis3Dev = MakeResultLabel(); midPanel.Controls.Add(_lblAxis3Dev, 6, rowC);

        // 层均值/偏差 标签行
        var rowLabel = 4;
        midPanel.Controls.Add(MakeLabel("层均值", ContentAlignment.MiddleLeft), 1, rowLabel);
        midPanel.Controls.Add(MakeLabel("层偏差", ContentAlignment.MiddleLeft), 5, rowLabel);

        _lblLevelAAvg = MakeResultLabel(); midPanel.Controls.Add(_lblLevelAAvg, 1, 5);
        _lblLevelBAvg = MakeResultLabel(); midPanel.Controls.Add(_lblLevelBAvg, 2, 5);
        _lblLevelCAvg = MakeResultLabel(); midPanel.Controls.Add(_lblLevelCAvg, 3, 5);

        _lblLevelADev = MakeResultLabel(); midPanel.Controls.Add(_lblLevelADev, 5, 5);
        _lblLevelBDev = MakeResultLabel(); midPanel.Controls.Add(_lblLevelBDev, 6, 5);

        // 综合偏差行
        var rowExtra = 4;
        _lblLevelCDev = MakeResultLabel(); midPanel.Controls.Add(_lblLevelCDev, 6, 5);

        // 汇总行
        var rowSummary = 5;
        midPanel.Controls.Add(MakeLabel("总均值", ContentAlignment.MiddleLeft), 0, 4);
        _lblTAvg = MakeResultLabel(); midPanel.Controls.Add(_lblTAvg, 1, 4);
        _lblMaxDev = MakeResultLabel(); midPanel.Controls.Add(_lblMaxDev, 2, 4);
        _lblUniformResult = MakeResultLabel();
        _lblUniformResult.ForeColor = Color.Gray;
        _lblUniformResult.Text = "等待计算";
        midPanel.Controls.Add(_lblUniformResult, 3, 4);

        _lblAxialDev = MakeResultLabel(); midPanel.Controls.Add(_lblAxialDev, 5, 4);
        _lblLevelDev = MakeResultLabel(); midPanel.Controls.Add(_lblLevelDev, 6, 4);

        // ===== 按钮栏 =====
        var btnPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(10, 4, 10, 4)
        };

        _btnCalculate = new Button { Text = "计算均匀性", Width = 100, Left = 0, Top = 4 };
        _btnCalculate.Click += BtnCalculate_Click;

        _btnSave = new Button { Text = "保存校准记录", Width = 110, Left = 110, Top = 4, Enabled = false };
        _btnSave.Click += BtnSave_Click;

        _btnQueryHistory = new Button { Text = "查询历史", Width = 90, Left = 230, Top = 4 };
        _btnQueryHistory.Click += BtnQueryHistory_Click;

        btnPanel.Controls.Add(_btnCalculate);
        btnPanel.Controls.Add(_btnSave);
        btnPanel.Controls.Add(_btnQueryHistory);

        // ===== 底部：历史记录 =====
        _lblHistoryStatus = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 20,
            Text = "历史校准记录",
            ForeColor = Color.Gray,
            Padding = new Padding(10, 2, 0, 0)
        };

        _dgvHistory = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false
        };

        // ===== 组装 =====
        Controls.Add(_dgvHistory);
        Controls.Add(_lblHistoryStatus);
        Controls.Add(btnPanel);
        Controls.Add(midPanel);
        Controls.Add(topPanel);

        ResumeLayout(false);
    }

    // ========================================================================
    // 实时温度更新
    // ========================================================================

    private void OnDataBroadcast(object? sender, DataBroadcastEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => OnDataBroadcast(sender, e)));
            return;
        }
        _lblTcalValue.Text = $"{e.Tcal:F1} °C";
    }

    // ========================================================================
    // 计算均匀性
    // ========================================================================

    // 缓存上次计算结果
    private double[] _lastTemps = Array.Empty<double>();
    private double _lastTAvg;
    private double _lastMaxDev;
    private bool _lastPassed;

    private void BtnCalculate_Click(object? sender, EventArgs e)
    {
        try
        {
            // 读取所有温度值
            var temps = new double[9];
            for (int i = 0; i < 9; i++)
            {
                if (!double.TryParse(_tempInputs[i].Text.Trim(), out temps[i]))
                {
                    MessageBox.Show($"请确保所有 9 个测温点都填写了有效的温度值。\n问题：{GetPointName(i)}",
                        "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // 计算轴向均值：Axis1 = (A1+B1+C1)/3, Axis2 = (A2+B2+C2)/3, Axis3 = (A3+B3+C3)/3
            double axis1Avg = (temps[0] + temps[3] + temps[6]) / 3.0; // A1, B1, C1
            double axis2Avg = (temps[1] + temps[4] + temps[7]) / 3.0; // A2, B2, C2
            double axis3Avg = (temps[2] + temps[5] + temps[8]) / 3.0; // A3, B3, C3

            // 计算层均值：LevelA = (A1+A2+A3)/3, LevelB = (B1+B2+B3)/3, LevelC = (C1+C2+C3)/3
            double levelAAvg = (temps[0] + temps[1] + temps[2]) / 3.0;
            double levelBAvg = (temps[3] + temps[4] + temps[5]) / 3.0;
            double levelCAvg = (temps[6] + temps[7] + temps[8]) / 3.0;

            // 总均值
            double tAvg = temps.Average();

            // 轴向偏差（各轴均值与总均值的偏差）
            double axis1Dev = Math.Abs(axis1Avg - tAvg);
            double axis2Dev = Math.Abs(axis2Avg - tAvg);
            double axis3Dev = Math.Abs(axis3Avg - tAvg);

            // 层偏差（各层均值与总均值的偏差）
            double levelADev = Math.Abs(levelAAvg - tAvg);
            double levelBDev = Math.Abs(levelBAvg - tAvg);
            double levelCDev = Math.Abs(levelCAvg - tAvg);

            // 最大偏差
            double maxDev = temps.Max(t => Math.Abs(t - tAvg));

            // 均匀性判定：最大偏差 ≤ 10°C
            bool passed = maxDev <= 10.0;

            // 更新显示
            _lblAxis1Avg.Text = $"{axis1Avg:F1}";
            _lblAxis2Avg.Text = $"{axis2Avg:F1}";
            _lblAxis3Avg.Text = $"{axis3Avg:F1}";
            _lblAxis1Dev.Text = $"{axis1Dev:F1}";
            _lblAxis2Dev.Text = $"{axis2Dev:F1}";
            _lblAxis3Dev.Text = $"{axis3Dev:F1}";

            _lblLevelAAvg.Text = $"{levelAAvg:F1}";
            _lblLevelBAvg.Text = $"{levelBAvg:F1}";
            _lblLevelCAvg.Text = $"{levelCAvg:F1}";
            _lblLevelADev.Text = $"{levelADev:F1}";
            _lblLevelBDev.Text = $"{levelBDev:F1}";
            _lblLevelCDev.Text = $"{levelCDev:F1}";

            _lblTAvg.Text = $"{tAvg:F1}";
            _lblMaxDev.Text = $"{maxDev:F1}";
            _lblAxialDev.Text = $"轴:{Math.Max(axis1Dev, Math.Max(axis2Dev, axis3Dev)):F1}";
            _lblLevelDev.Text = $"层:{Math.Max(levelADev, Math.Max(levelBDev, levelCDev)):F1}";

            _lblUniformResult.Text = passed ? "✅ 均匀性合格" : "❌ 均匀性不合格";
            _lblUniformResult.ForeColor = passed ? Color.Green : Color.Red;

            // 缓存结果
            _lastTemps = temps;
            _lastTAvg = tAvg;
            _lastMaxDev = maxDev;
            _lastPassed = passed;
            _btnSave.Enabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"计算失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ========================================================================
    // 保存校准记录
    // ========================================================================

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            if (_lastTemps.Length != 9)
            {
                MessageBox.Show("请先点击「计算均匀性」。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var record = new CalibrationRecord
            {
                Id = Guid.NewGuid().ToString(),
                CalibrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                CalibrationType = "Surface",
                ApparatusId = 0, // FURNACE-01
                Operator = "admin", // 后续从 MainForm 传入
                TemperatureData = JsonSerializer.Serialize(_lastTemps),
                AverageTemperature = _lastTAvg,
                MaxDeviation = _lastMaxDev,
                PassedCriteria = _lastPassed ? 1 : 0,
                Remarks = _lastPassed ? "均匀性合格" : "均匀性不合格",
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                // 9 点温度
                TempA1 = _lastTemps[0], TempA2 = _lastTemps[1], TempA3 = _lastTemps[2],
                TempB1 = _lastTemps[3], TempB2 = _lastTemps[4], TempB3 = _lastTemps[5],
                TempC1 = _lastTemps[6], TempC2 = _lastTemps[7], TempC3 = _lastTemps[8],
                TAvg = _lastTAvg
            };

            _db.InsertCalibration(record);

            MessageBox.Show("校准记录已保存。", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _btnSave.Enabled = false;

            // 刷新历史
            RefreshHistory();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ========================================================================
    // 历史查询
    // ========================================================================

    private void BtnQueryHistory_Click(object? sender, EventArgs e)
    {
        RefreshHistory();
    }

    private void RefreshHistory()
    {
        try
        {
            var from = DateTime.Today.AddDays(-90);
            var to = DateTime.Today.AddDays(1);
            var records = _db.QueryCalibrations(from, to);

            var dt = new DataTable();
            dt.Columns.Add("CalibrationDate", typeof(string));
            dt.Columns.Add("CalibrationType", typeof(string));
            dt.Columns.Add("Operator", typeof(string));
            dt.Columns.Add("AverageTemperature", typeof(double));
            dt.Columns.Add("MaxDeviation", typeof(double));
            dt.Columns.Add("Passed", typeof(string));

            foreach (var r in records)
            {
                dt.Rows.Add(
                    r.CalibrationDate,
                    r.CalibrationType,
                    r.Operator,
                    r.AverageTemperature ?? 0,
                    r.MaxDeviation ?? 0,
                    r.PassedCriteria == 1 ? "✅ 通过" : "❌ 未通过"
                );
            }

            _dgvHistory.DataSource = dt;

            var colNames = new[] { "校准日期", "类型", "操作员", "平均温度(°C)", "最大偏差(°C)", "判定" };
            for (int i = 0; i < colNames.Length && i < _dgvHistory.Columns.Count; i++)
            {
                _dgvHistory.Columns[i].HeaderText = colNames[i];
            }

            _lblHistoryStatus.Text = $"历史校准记录 — 共 {records.Count} 条";
            _lblHistoryStatus.ForeColor = Color.Gray;
        }
        catch (Exception ex)
        {
            _lblHistoryStatus.Text = $"查询失败：{ex.Message}";
            _lblHistoryStatus.ForeColor = Color.Red;
        }
    }

    // ========================================================================
    // 辅助方法
    // ========================================================================

    private static Label MakeLabel(string text, ContentAlignment align)
    {
        return new Label
        {
            Text = text,
            TextAlign = align,
            AutoSize = false,
            Width = align == ContentAlignment.MiddleLeft ? 100 : 60,
            Height = 25
        };
    }

    private static TextBox MakeTempInput()
    {
        return new TextBox
        {
            Width = 65,
            TextAlign = HorizontalAlignment.Center,
            Font = new Font("Consolas", 10)
        };
    }

    private static Label MakeResultLabel()
    {
        return new Label
        {
            Text = "—",
            AutoSize = false,
            Width = 90,
            Height = 25,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.DarkBlue,
            Font = new Font("Consolas", 9)
        };
    }

    private static string GetPointName(int index) => index switch
    {
        0 => "A1", 1 => "A2", 2 => "A3",
        3 => "B1", 4 => "B2", 5 => "B3",
        6 => "C1", 7 => "C2", 8 => "C3",
        _ => "?"
    };

    /// <summary>
    /// 清理事件订阅，防止内存泄漏。
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _controller.DataBroadcast -= OnDataBroadcast;
        }
        base.Dispose(disposing);
    }
}