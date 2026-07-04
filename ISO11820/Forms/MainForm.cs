using System;
using System.Drawing;
using System.Windows.Forms;
using ISO11820.Core;
using ISO11820.Data;
using ISO11820.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace ISO11820.Forms;

/// <summary>
/// 主窗体 — 包含温度面板、曲线图、消息日志、按钮互锁
/// 角色 C 负责。版本：2026-07-03
/// </summary>
public partial class MainForm : Form
{
    // ========== 核心状态机 ==========
    private readonly TestController Controller = new TestController();

    // ========== 登录信息（从构造传入） ==========
    private readonly string _loggedInUser;
    private readonly string _loggedInRole;
    private readonly DbHelper? _dbHelper;

    // ========== 温度面板控件 ==========
    private Label lblTf1Value = null!;
    private Label lblTf2Value = null!;
    private Label lblTsValue = null!;
    private Label lblTcValue = null!;
    private Label lblTcalValue = null!;
    private Label lblElapsedValue = null!;
    private Label lblDriftValue = null!;
    private Label lblStateValue = null!;
    private Label lblProductIdValue = null!;

    // ========== 曲线图 ==========
    private PlotView plotView = null!;
    private PlotModel plotModel = null!;
    private LineSeries seriesTf1 = null!;
    private LineSeries seriesTf2 = null!;
    private LineSeries seriesTs = null!;
    private LineSeries seriesTc = null!;

    private const double PlotWindowSeconds = 600;
    private DateTime _chartStartTime;

    // ========== 系统消息日志 ==========
    private RichTextBox rtbLog = null!;

    // ========== 6 个按钮 ==========
    private Button btnNewTest = null!;
    private Button btnStartHeat = null!;
    private Button btnStopHeat = null!;
    private Button btnStartRecord = null!;
    private Button btnStopRecord = null!;
    private Button btnSettings = null!;

// ========== TabControl ==========
    private TabControl _tabControl = null!;
    private TabPage _experimentTab = null!;
    private TabPage _recordQueryTab = null!;
    private TabPage _calibrationTab = null!;
    private RecordQueryTab _recordQueryControl = null!;
    private CalibrationTab _calibrationControl = null!;

    // ========== 温度记录缓冲区（用于 CSV 导出） ==========
    private readonly List<TemperatureRecord> _temperatureRecords = new();

    // ==================== 构造函数 ====================

    public MainForm() : this("", "", (DbHelper?)null) { }

    public MainForm(string loggedInUser, string loggedInRole, DbHelper? dbHelper)
    {
        _loggedInUser = loggedInUser;
        _loggedInRole = loggedInRole;
        _dbHelper = dbHelper;

        Text = "ISO 11820 建筑材料不燃性试验系统";
        Size = new Size(1280, 920);
        StartPosition = FormStartPosition.CenterScreen;

        BuildTabControl();
        BuildTemperaturePanel();
        BuildTemperatureChart();
        BuildButtons();
        BuildMessageLog();

        Controller.DataBroadcast += OnDataBroadcast;
        _chartStartTime = DateTime.Now;
        UpdateButtonStates(Controller.State);
    }

    public MainForm(DbHelper dbHelper, string loggedInUser, string loggedInRole)
        : this(loggedInUser, loggedInRole, dbHelper)
    {
    }

    // ==================== 构建 TabControl ====================
    private void BuildTabControl()
    {
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei", 10)
        };

        // 试验控制 Tab
        _experimentTab = new TabPage("试验控制");
        _tabControl.TabPages.Add(_experimentTab);

        // 记录查询 Tab
        if (_dbHelper != null)
        {
            _recordQueryTab = new TabPage("记录查询");
            _recordQueryControl = new RecordQueryTab(_dbHelper);
            _recordQueryControl.Dock = DockStyle.Fill;
            _recordQueryTab.Controls.Add(_recordQueryControl);
            _tabControl.TabPages.Add(_recordQueryTab);
        }

        // 设备校准 Tab
        if (_dbHelper != null)
        {
            _calibrationTab = new TabPage("设备校准");
            _calibrationControl = new CalibrationTab(_dbHelper, Controller);
            _calibrationControl.Dock = DockStyle.Fill;
            _calibrationTab.Controls.Add(_calibrationControl);
            _tabControl.TabPages.Add(_calibrationTab);
        }

        Controls.Add(_tabControl);
    }

    // ==================== 构建温度面板 ====================
    private void BuildTemperaturePanel()
    {
        var panel = new Panel
        {
            Location = new Point(20, 20),
            Size = new Size(1230, 150),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(30, 30, 30)
        };

        (Label label, Label value) tf1 = CreateChannel(panel, "TF1 炉温1", 20, Color.Red);
        lblTf1Value = tf1.value;
        (Label label, Label value) tf2 = CreateChannel(panel, "TF2 炉温2", 160, Color.Orange);
        lblTf2Value = tf2.value;
        (Label label, Label value) ts = CreateChannel(panel, "TS 表面温度", 300, Color.DeepSkyBlue);
        lblTsValue = ts.value;
        (Label label, Label value) tc = CreateChannel(panel, "TC 中心温度", 440, Color.LimeGreen);
        lblTcValue = tc.value;
        (Label label, Label value) tcal = CreateChannel(panel, "TCal 校准温度", 580, Color.Violet);
        lblTcalValue = tcal.value;

        var lblElapsedTitle = new Label { Text = "记录时长", ForeColor = Color.Gainsboro, Font = new Font("微软雅黑", 9), Location = new Point(760, 15), Size = new Size(100, 20) };
        lblElapsedValue = new Label { Text = "00:00:00", ForeColor = Color.White, Font = new Font("Consolas", 16, FontStyle.Bold), Location = new Point(760, 35), Size = new Size(140, 30) };

        var lblDriftTitle = new Label { Text = "温漂 (°C/10min)", ForeColor = Color.Gainsboro, Font = new Font("微软雅黑", 9), Location = new Point(760, 70), Size = new Size(140, 20) };
        lblDriftValue = new Label { Text = "TF1: 0.00 / TF2: 0.00", ForeColor = Color.White, Font = new Font("Consolas", 11), Location = new Point(760, 90), Size = new Size(200, 25) };

        var lblStateTitle = new Label { Text = "当前状态", ForeColor = Color.Gainsboro, Font = new Font("微软雅黑", 9), Location = new Point(980, 15), Size = new Size(100, 20) };
        lblStateValue = new Label { Text = "空闲", ForeColor = Color.Yellow, Font = new Font("微软雅黑", 16, FontStyle.Bold), Location = new Point(980, 35), Size = new Size(200, 30) };

        var lblProductIdTitle = new Label { Text = "样品编号", ForeColor = Color.Gainsboro, Font = new Font("微软雅黑", 9), Location = new Point(980, 70), Size = new Size(100, 20) };
        lblProductIdValue = new Label { Text = "（未新建试验）", ForeColor = Color.White, Font = new Font("微软雅黑", 11), Location = new Point(980, 90), Size = new Size(220, 25) };

        panel.Controls.Add(lblElapsedTitle);
        panel.Controls.Add(lblElapsedValue);
        panel.Controls.Add(lblDriftTitle);
        panel.Controls.Add(lblDriftValue);
        panel.Controls.Add(lblStateTitle);
        panel.Controls.Add(lblStateValue);
        panel.Controls.Add(lblProductIdTitle);
        panel.Controls.Add(lblProductIdValue);
        _experimentTab.Controls.Add(panel);
    }

    private (Label label, Label value) CreateChannel(Panel panel, string title, int x, Color ledColor)
    {
        var lblTitle = new Label { Text = title, ForeColor = Color.Gainsboro, Font = new Font("微软雅黑", 9), Location = new Point(x, 15), Size = new Size(130, 20) };
        var lblValue = new Label { Text = "--.- °C", ForeColor = ledColor, Font = new Font("Consolas", 20, FontStyle.Bold), Location = new Point(x, 40), Size = new Size(130, 45) };
        panel.Controls.Add(lblTitle);
        panel.Controls.Add(lblValue);
        return (lblTitle, lblValue);
    }

    // ==================== 构建温度曲线 ====================
    private void BuildTemperatureChart()
    {
        plotModel = new PlotModel
        {
            Title = "温度实时曲线",
            TitleColor = OxyColors.White,
            PlotAreaBorderColor = OxyColors.Gray,
            Background = OxyColor.FromRgb(20, 20, 20),
            TextColor = OxyColors.Gainsboro
        };

        var xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "时间 (秒)",
            TitleColor = OxyColors.Gainsboro,
            TextColor = OxyColors.Gainsboro,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60),
            Minimum = 0,
            Maximum = PlotWindowSeconds
        };

        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "温度 (°C)",
            TitleColor = OxyColors.Gainsboro,
            TextColor = OxyColors.Gainsboro,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60),
            Minimum = 0,
            Maximum = 800
        };

        plotModel.Axes.Add(xAxis);
        plotModel.Axes.Add(yAxis);

        seriesTf1 = new LineSeries { Title = "TF1 炉温1", Color = OxyColors.Red, StrokeThickness = 2.5 };
        seriesTf2 = new LineSeries { Title = "TF2 炉温2", Color = OxyColors.Orange, StrokeThickness = 1.5 };
        seriesTs = new LineSeries { Title = "TS 表面温度", Color = OxyColors.DeepSkyBlue, StrokeThickness = 1.5 };
        seriesTc = new LineSeries { Title = "TC 中心温度", Color = OxyColors.LimeGreen, StrokeThickness = 2.0 };

        plotModel.Series.Add(seriesTf2);
        plotModel.Series.Add(seriesTf1);  // TF1 画在 TF2 上面，确保两条线都可见
        plotModel.Series.Add(seriesTs);
        plotModel.Series.Add(seriesTc);

        plotModel.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendTextColor = OxyColors.Gainsboro,
            LegendBackground = OxyColor.FromAColor(180, OxyColors.Black)
        });

        plotView = new PlotView
        {
            Model = plotModel,
            Location = new Point(20, 185),
            Size = new Size(1230, 360),
            BackColor = Color.FromArgb(20, 20, 20)
        };
        _experimentTab.Controls.Add(plotView);
    }

    private void AppendChartPoint(double elapsedSeconds, double tf1, double tf2, double ts, double tc)
    {
        seriesTf1.Points.Add(new DataPoint(elapsedSeconds, tf1));
        seriesTf2.Points.Add(new DataPoint(elapsedSeconds, tf2));
        seriesTs.Points.Add(new DataPoint(elapsedSeconds, ts));
        seriesTc.Points.Add(new DataPoint(elapsedSeconds, tc));

        TrimSeries(seriesTf1, elapsedSeconds);
        TrimSeries(seriesTf2, elapsedSeconds);
        TrimSeries(seriesTs, elapsedSeconds);
        TrimSeries(seriesTc, elapsedSeconds);

        var xAxis = plotModel.Axes[0];
        if (elapsedSeconds > PlotWindowSeconds)
        {
            xAxis.Minimum = elapsedSeconds - PlotWindowSeconds;
            xAxis.Maximum = elapsedSeconds;
        }
        plotModel.InvalidatePlot(true);
    }

    private void TrimSeries(LineSeries series, double currentSeconds)
    {
        double cutoff = currentSeconds - PlotWindowSeconds;
        while (series.Points.Count > 0 && series.Points[0].X < cutoff)
        {
            series.Points.RemoveAt(0);
        }
    }

    private void ResetChart()
    {
        if (seriesTf1.Points.Count == 0) return;
        _chartStartTime = DateTime.Now;
        seriesTf1.Points.Clear();
        seriesTf2.Points.Clear();
        seriesTs.Points.Clear();
        seriesTc.Points.Clear();

        var xAxis = plotModel.Axes[0];
        xAxis.Minimum = 0;
        xAxis.Maximum = PlotWindowSeconds;
        plotModel.InvalidatePlot(true);
    }

    // ==================== 构建按钮 ====================
    private void BuildButtons()
    {
        // 曲线图底部：185 + 360 = 545，按钮从 555 开始
        int buttonY = 555;

        btnNewTest = new Button { Text = "新建试验", Location = new Point(20, buttonY), Size = new Size(100, 30) };
        btnStartHeat = new Button { Text = "开始升温", Location = new Point(130, buttonY), Size = new Size(100, 30) };
        btnStopHeat = new Button { Text = "停止升温", Location = new Point(240, buttonY), Size = new Size(100, 30) };
        btnStartRecord = new Button { Text = "开始记录", Location = new Point(350, buttonY), Size = new Size(100, 30) };
        btnStopRecord = new Button { Text = "停止记录", Location = new Point(460, buttonY), Size = new Size(100, 30) };
        btnSettings = new Button { Text = "参数设置", Location = new Point(570, buttonY), Size = new Size(100, 30) };

        btnNewTest.Click += BtnNewTest_Click;
        btnStartHeat.Click += BtnStartHeat_Click;
        btnStopHeat.Click += BtnStopHeat_Click;
        btnStartRecord.Click += BtnStartRecord_Click;
        btnStopRecord.Click += BtnStopRecord_Click;
        btnSettings.Click += BtnSettings_Click;

        _experimentTab.Controls.Add(btnNewTest);
        _experimentTab.Controls.Add(btnStartHeat);
        _experimentTab.Controls.Add(btnStopHeat);
        _experimentTab.Controls.Add(btnStartRecord);
        _experimentTab.Controls.Add(btnStopRecord);
        _experimentTab.Controls.Add(btnSettings);
    }

    // ==================== 构建消息日志 ====================
    private void BuildMessageLog()
    {
        // 按钮在 555-585，日志标题放在 595，日志框从 620 开始
        var lblTitle = new Label
        {
            Text = "系统消息日志",
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            Location = new Point(20, 595),
            Size = new Size(200, 25)
        };

        rtbLog = new RichTextBox
        {
            Location = new Point(20, 620),
            Size = new Size(1230, 140),
            ReadOnly = true,
            BackColor = Color.White,
            Font = new Font("Consolas", 10),
            ScrollBars = RichTextBoxScrollBars.Vertical
        };

        _experimentTab.Controls.Add(lblTitle);
        _experimentTab.Controls.Add(rtbLog);
    }

    private void AppendLogMessage(MasterMessage msg)
    {
        Color color = msg.Type == "warning" ? Color.OrangeRed : Color.Black;
        rtbLog.SelectionStart = rtbLog.TextLength;
        rtbLog.SelectionLength = 0;
        rtbLog.SelectionColor = color;
        rtbLog.AppendText($"{msg.Time} {msg.Content}{Environment.NewLine}");
        rtbLog.SelectionColor = rtbLog.ForeColor;
        rtbLog.ScrollToCaret();
    }

    private void UpdateButtonStates(TestState state)
    {
        btnNewTest.Enabled = false;
        btnStartHeat.Enabled = false;
        btnStopHeat.Enabled = false;
        btnStartRecord.Enabled = false;
        btnStopRecord.Enabled = false;
        btnSettings.Enabled = false;

        switch (state)
        {
            case TestState.Idle:
                btnNewTest.Enabled = true;
                btnStartHeat.Enabled = true;
                btnSettings.Enabled = true;
                break;
            case TestState.Preparing:
                btnStopHeat.Enabled = true;
                break;
            case TestState.Ready:
                btnStartRecord.Enabled = true;
                btnStopHeat.Enabled = true;
                break;
            case TestState.Recording:
                btnStopRecord.Enabled = true;
                break;
            case TestState.Complete:
                btnNewTest.Enabled = true;
                btnStopHeat.Enabled = true;
                btnSettings.Enabled = true;
                break;
        }
    }

    // ==================== 按钮点击事件 ====================

    private void BtnNewTest_Click(object? sender, EventArgs e)
    {
        using var form = new NewTestForm(Controller, _dbHelper ?? new DbHelper(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppGlobal.Instance.DbPath)));
        if (form.ShowDialog() == DialogResult.OK)
        {
            // 更新产品编号显示
            if (!string.IsNullOrEmpty(Controller.CurrentProductId))
                lblProductIdValue.Text = Controller.CurrentProductId;
            UpdateButtonStates(Controller.State);
        }
    }

    private void BtnStartHeat_Click(object? sender, EventArgs e)
    {
        if (!Controller.StartHeating())
        {
            MessageBox.Show("无法开始升温，请检查当前状态", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        _chartStartTime = DateTime.Now;
        ResetChart();
        UpdateButtonStates(Controller.State);
    }

    private void BtnStopHeat_Click(object? sender, EventArgs e)
    {
        if (!Controller.StopHeating())
        {
            MessageBox.Show("无法停止升温，请检查当前状态", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        UpdateButtonStates(Controller.State);
    }

    private void BtnStartRecord_Click(object? sender, EventArgs e)
    {
        if (!Controller.StartRecording())
        {
            MessageBox.Show("无法开始记录，请检查当前状态或是否有未保存的试验", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        UpdateButtonStates(Controller.State);
    }

    private void BtnStopRecord_Click(object? sender, EventArgs e)
    {
        if (!Controller.StopRecording())
        {
            MessageBox.Show("无法停止记录，请检查当前状态", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        UpdateButtonStates(Controller.State);

        if (Controller.State == TestState.Complete)
        {
            string productId = Controller.CurrentProductId;
            string testId = Controller.CurrentTestId;
            double preWeight = Controller.CurrentPreWeight;
            double initialTemp = Controller.Ts; // 样品初始温度（记录开始时的温度）

            // 如果 preWeight 为 0（兼容旧数据），尝试从数据库读取
            var db = _dbHelper ?? new DbHelper(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppGlobal.Instance.DbPath));
            if (preWeight <= 0 && !string.IsNullOrEmpty(productId) && !string.IsNullOrEmpty(testId))
            {
                var existingTest = db.GetTest(productId, testId);
                if (existingTest != null)
                {
                    preWeight = existingTest.PreWeight;
                    Controller.CurrentPreWeight = preWeight;
                }
            }

            using var form = new PhenomenonForm(Controller, db, productId, testId, preWeight, initialTemp);
            if (form.ShowDialog() == DialogResult.OK)
            {
                UpdateButtonStates(Controller.State);

                // 触发导出服务
                ExportTestResults(productId, testId);
            }
        }
    }

    private void BtnSettings_Click(object? sender, EventArgs e)
    {
        using var form = new SettingsForm();
        form.ShowDialog(this);
    }

    // ==================== 数据广播事件 ====================
    private void OnDataBroadcast(object? sender, DataBroadcastEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => OnDataBroadcast(sender, e)));
            return;
        }
        if (lblTf1Value == null || plotView == null) return;

        lblTf1Value.Text = $"{e.Tf1:F1} °C";
        lblTf2Value.Text = $"{e.Tf2:F1} °C";
        lblTsValue.Text = $"{e.Ts:F1} °C";
        lblTcValue.Text = $"{e.Tc:F1} °C";
        lblTcalValue.Text = $"{e.Tcal:F1} °C";

        var elapsed = TimeSpan.FromSeconds(e.ElapsedSeconds);
        lblElapsedValue.Text = elapsed.ToString(@"hh\:mm\:ss");
        lblDriftValue.Text = $"TF1: {e.DriftTf1:F2} / TF2: {e.DriftTf2:F2}";

        string stateText = e.State switch
        {
            TestState.Idle => "空闲",
            TestState.Preparing => "升温准备",
            TestState.Ready => "就绪待测",
            TestState.Recording => "记录中",
            TestState.Complete => "试验完成",
            _ => "未知"
        };
        lblStateValue.Text = stateText;
        lblStateValue.ForeColor = e.State switch
        {
            TestState.Idle => Color.Gray,
            TestState.Preparing => Color.Orange,
            TestState.Ready => Color.LimeGreen,
            TestState.Recording => Color.Red,
            TestState.Complete => Color.DeepSkyBlue,
            _ => Color.White
        };

        if (!string.IsNullOrEmpty(Controller.CurrentProductId))
        {
            lblProductIdValue.Text = Controller.CurrentProductId;
        }

        UpdateButtonStates(e.State);

        if (e.State == TestState.Idle)
            ResetChart();
        else
        {
            double realElapsed = (DateTime.Now - _chartStartTime).TotalSeconds;
            AppendChartPoint(realElapsed, e.Tf1, e.Tf2, e.Ts, e.Tc);
        }

        // 记录阶段：收集温度数据用于 CSV 导出
        if (e.State == TestState.Recording)
        {
            _temperatureRecords.Add(new TemperatureRecord
            {
                Time = e.ElapsedSeconds,
                Tf1 = e.Tf1,
                Tf2 = e.Tf2,
                Ts = e.Ts,
                Tc = e.Tc,
                Tcal = e.Tcal
            });
        }

        foreach (var msg in e.Messages)
            AppendLogMessage(msg);
    }

    // ==================== 导出服务 ====================

    private void ExportTestResults(string productId, string testId)
    {
        try
        {
            var db = _dbHelper ?? new DbHelper(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppGlobal.Instance.DbPath));

            // 1. CSV 导出 — 温度时间序列
            if (_temperatureRecords.Count > 0)
            {
                string csvPath = FileStorageManager.GetCsvPath(productId, testId);
                CsvExportService.Export(csvPath, _temperatureRecords);
                AppendLogMessage(new MasterMessage
                {
                    Time = DateTime.Now.ToString("HH:mm:ss"),
                    Content = $"CSV 数据已导出：{csvPath}",
                    Type = "normal"
                });
            }
            _temperatureRecords.Clear();

            // 2. PDF 导出 — 试验报告
            var pdfService = new PdfExportService(db);
            string pdfPath = pdfService.ExportPdf(productId, testId);
            AppendLogMessage(new MasterMessage
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Content = $"PDF 报告已导出：{pdfPath}",
                Type = "normal"
            });

            // 3. Excel 导出 — 可选（用户可在记录查询 Tab 中手动导出）
            // 不在此处自动导出，避免不必要的文件生成
        }
        catch (Exception ex)
        {
            AppendLogMessage(new MasterMessage
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Content = $"导出失败：{ex.Message}",
                Type = "warning"
            });
        }
    }

    // ==================== 窗体关闭 ====================
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Controller.DataBroadcast -= OnDataBroadcast;
        base.OnFormClosing(e);
    }
}