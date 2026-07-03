using ISO11820.Core;
using ISO11820.Data;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace ISO11820.Forms;

/// <summary>
/// 主窗体 — 试验控制 + 记录查询 + 设备校准。
/// 使用 TabControl 分页，各 Tab 由团队成员分别负责。
/// </summary>
public partial class MainForm : Form
{
    // 由 A 提供的核心状态机，MainForm 持有一个实例
    private readonly TestController _controller = new();

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

    // 滚动窗口：只保留最近 10 分钟的数据点（10分钟 = 600秒）
    private const double PlotWindowSeconds = 600;

    // 曲线横轴使用的累计时间（秒）。DataBroadcast 每 800ms 触发一次，
    // 由 MainForm 自行累加，覆盖 Idle 之外的所有阶段（升温/就绪/记录/完成），
    // 不能直接用 e.ElapsedSeconds，因为它只在 Recording 状态才计时。
    private double _chartTimeSeconds;

    // ========== 系统消息日志 ==========
    private RichTextBox rtbLog = null!;

    // ========== TabControl ==========
    private TabControl _tabControl = null!;
    private TabPage _experimentTab = null!;
    private TabPage _recordQueryTab = null!;
    private TabPage _calibrationTab = null!;

    private RecordQueryTab _recordQueryControl = null!;
    private CalibrationTab _calibrationControl = null!;

    /// <summary>试验控制器（Core 层）</summary>
    public TestController Controller => _controller;

    /// <summary>数据库操作（Data 层）</summary>
    public DbHelper Db { get; }

    /// <summary>当前登录的操作员用户名</summary>
    public string CurrentOperator { get; }

    /// <summary>当前登录的角色（admin/operator）</summary>
    public string CurrentRole { get; }

    public MainForm(DbHelper db, string currentOperator, string currentRole)
    {
        Db = db;
        CurrentOperator = currentOperator;
        CurrentRole = currentRole;

        Text = "ISO 11820 建筑材料不燃性试验系统";
        Size = new Size(1280, 850);
        StartPosition = FormStartPosition.CenterScreen;

        BuildTabControl();
        BuildExperimentTab();
        BuildTemperaturePanel();
        BuildTemperatureChart();
        BuildMessageLog();

        // 订阅数据广播事件（在后台线程触发，必须用 Invoke 切回 UI 线程）
        _controller.DataBroadcast += OnDataBroadcast;
    }

    private void BuildTabControl()
    {
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei", 10)
        };

        // --- 试验控制 Tab（廖紫彤负责） ---
        _experimentTab = new TabPage("试验控制");
        _tabControl.TabPages.Add(_experimentTab);

        // --- 记录查询 Tab（龚小倩负责） ---
        _recordQueryTab = new TabPage("记录查询");
        _recordQueryControl = new RecordQueryTab(Db);
        _recordQueryControl.Dock = DockStyle.Fill;
        _recordQueryTab.Controls.Add(_recordQueryControl);
        _tabControl.TabPages.Add(_recordQueryTab);

        // --- 设备校准 Tab（龚小倩负责） ---
        _calibrationTab = new TabPage("设备校准");
        _calibrationControl = new CalibrationTab(Db, _controller);
        _calibrationControl.Dock = DockStyle.Fill;
        _calibrationTab.Controls.Add(_calibrationControl);
        _tabControl.TabPages.Add(_calibrationTab);

        Controls.Add(_tabControl);
    }

    private void BuildExperimentTab()
    {
        // 试验控制 Tab 的内容将由廖紫彤的 BuildTemperaturePanel/BuildTemperatureChart/BuildMessageLog 填充
        // 这些控件直接添加到 _experimentTab 中
    }

    private void BuildTemperaturePanel()
    {
        var panel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(1250, 150),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(30, 30, 30)
        };

        // 5 个温度通道：标签 + LED 风格数值
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

        // 右侧信息区：计时 / 温漂 / 状态 / 样品编号
        var lblElapsedTitle = new Label
        {
            Text = "记录时长",
            ForeColor = Color.Gainsboro,
            Font = new Font("微软雅黑", 9),
            Location = new Point(760, 15),
            Size = new Size(100, 20)
        };
        lblElapsedValue = new Label
        {
            Text = "00:00:00",
            ForeColor = Color.White,
            Font = new Font("Consolas", 16, FontStyle.Bold),
            Location = new Point(760, 35),
            Size = new Size(140, 30)
        };

        var lblDriftTitle = new Label
        {
            Text = "温漂 (°C/10min)",
            ForeColor = Color.Gainsboro,
            Font = new Font("微软雅黑", 9),
            Location = new Point(760, 70),
            Size = new Size(140, 20)
        };
        lblDriftValue = new Label
        {
            Text = "TF1: 0.00 / TF2: 0.00",
            ForeColor = Color.White,
            Font = new Font("Consolas", 11),
            Location = new Point(760, 90),
            Size = new Size(200, 25)
        };

        var lblStateTitle = new Label
        {
            Text = "当前状态",
            ForeColor = Color.Gainsboro,
            Font = new Font("微软雅黑", 9),
            Location = new Point(980, 15),
            Size = new Size(100, 20)
        };
        lblStateValue = new Label
        {
            Text = "空闲",
            ForeColor = Color.Yellow,
            Font = new Font("微软雅黑", 16, FontStyle.Bold),
            Location = new Point(980, 35),
            Size = new Size(200, 30)
        };

        var lblProductIdTitle = new Label
        {
            Text = "样品编号",
            ForeColor = Color.Gainsboro,
            Font = new Font("微软雅黑", 9),
            Location = new Point(980, 70),
            Size = new Size(100, 20)
        };
        lblProductIdValue = new Label
        {
            Text = "（未新建试验）",
            ForeColor = Color.White,
            Font = new Font("微软雅黑", 11),
            Location = new Point(980, 90),
            Size = new Size(220, 25)
        };

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

    /// <summary>
    /// 在温度面板里创建一个通道的 标签+LED数值 显示，返回控件引用供外部保存。
    /// </summary>
    private (Label label, Label value) CreateChannel(Panel panel, string title, int x, Color ledColor)
    {
        var lblTitle = new Label
        {
            Text = title,
            ForeColor = Color.Gainsboro,
            Font = new Font("微软雅黑", 9),
            Location = new Point(x, 15),
            Size = new Size(130, 20)
        };

        var lblValue = new Label
        {
            Text = "--.- °C",
            ForeColor = ledColor,
            Font = new Font("Consolas", 20, FontStyle.Bold),
            Location = new Point(x, 40),
            Size = new Size(130, 45)
        };

        panel.Controls.Add(lblTitle);
        panel.Controls.Add(lblValue);

        return (lblTitle, lblValue);
    }

    /// <summary>
    /// 构建系统消息日志：RichTextBox，每条消息 "HH:mm:ss 内容"，
    /// 普通消息黑色，警告消息橙红色，追加后自动滚动到底部。
    /// </summary>
    private void BuildMessageLog()
    {
        var lblTitle = new Label
        {
            Text = "系统消息日志",
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            Location = new Point(0, 570),
            Size = new Size(200, 25)
        };

        rtbLog = new RichTextBox
        {
            Location = new Point(0, 595),
            Size = new Size(1250, 150),
            ReadOnly = true,
            BackColor = Color.White,
            Font = new Font("Consolas", 10),
            ScrollBars = RichTextBoxScrollBars.Vertical
        };

        _experimentTab.Controls.Add(lblTitle);
        _experimentTab.Controls.Add(rtbLog);
    }

    /// <summary>
    /// 向日志追加一条消息。type 为 "warning" 时显示橙红色，其余为普通黑色。
    /// </summary>
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

    /// <summary>
    /// 构建 OxyPlot 实时温度曲线：4 条折线（TF1红/TF2橙/TS蓝/TC绿），
    /// X 轴时间（秒）滚动 10 分钟窗口，Y 轴固定 0~800°C。
    /// </summary>
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

        seriesTf1 = new LineSeries { Title = "TF1 炉温1", Color = OxyColors.Red, StrokeThickness = 1.5 };
        seriesTf2 = new LineSeries { Title = "TF2 炉温2", Color = OxyColors.Orange, StrokeThickness = 1.5 };
        seriesTs = new LineSeries { Title = "TS 表面温度", Color = OxyColors.DeepSkyBlue, StrokeThickness = 1.5 };
        seriesTc = new LineSeries { Title = "TC 中心温度", Color = OxyColors.LimeGreen, StrokeThickness = 1.5 };

        plotModel.Series.Add(seriesTf1);
        plotModel.Series.Add(seriesTf2);
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
            Location = new Point(0, 160),
            Size = new Size(1250, 400),
            BackColor = Color.FromArgb(20, 20, 20)
        };

        _experimentTab.Controls.Add(plotView);
    }

    /// <summary>
    /// 向曲线追加一个数据点，并维护滚动窗口（只保留最近 10 分钟）。
    /// </summary>
    private void AppendChartPoint(double elapsedSeconds, double tf1, double tf2, double ts, double tc)
    {
        seriesTf1.Points.Add(new DataPoint(elapsedSeconds, tf1));
        seriesTf2.Points.Add(new DataPoint(elapsedSeconds, tf2));
        seriesTs.Points.Add(new DataPoint(elapsedSeconds, ts));
        seriesTc.Points.Add(new DataPoint(elapsedSeconds, tc));

        // 滚动窗口：移除超出 10 分钟窗口之外的旧点
        TrimSeries(seriesTf1, elapsedSeconds);
        TrimSeries(seriesTf2, elapsedSeconds);
        TrimSeries(seriesTs, elapsedSeconds);
        TrimSeries(seriesTc, elapsedSeconds);

        // X 轴跟随滚动
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

    // ========== 数据广播事件处理 ==========

    /// <summary>
    /// ⚠️ 跨线程要点：DataBroadcast 事件在后台线程触发，
    /// 所有 UI 更新必须通过 Invoke 切回 UI 线程。
    /// </summary>
    private void OnDataBroadcast(object? sender, DataBroadcastEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => OnDataBroadcast(sender, e)));
            return;
        }

        // 安全操作 UI 控件
        lblTf1Value.Text = $"{e.Tf1:F1} °C";
        lblTf2Value.Text = $"{e.Tf2:F1} °C";
        lblTsValue.Text = $"{e.Ts:F1} °C";
        lblTcValue.Text = $"{e.Tc:F1} °C";
        lblTcalValue.Text = $"{e.Tcal:F1} °C";

        var elapsed = TimeSpan.FromSeconds(e.ElapsedSeconds);
        lblElapsedValue.Text = elapsed.ToString(@"hh\:mm\:ss");

        lblDriftValue.Text = $"TF1: {e.DriftTf1:F2} / TF2: {e.DriftTf2:F2}";

        lblStateValue.Text = _controller.GetStateText();
        lblStateValue.ForeColor = e.State switch
        {
            TestState.Idle => Color.Gray,
            TestState.Preparing => Color.Orange,
            TestState.Ready => Color.LimeGreen,
            TestState.Recording => Color.Red,
            TestState.Complete => Color.DeepSkyBlue,
            _ => Color.White
        };

        if (!string.IsNullOrEmpty(_controller.CurrentProductId))
        {
            lblProductIdValue.Text = _controller.CurrentProductId;
        }

        // 更新曲线图：Idle 状态清空曲线并重置计时；其余状态每次广播累加 0.8 秒
        if (e.State == TestState.Idle)
        {
            ResetChart();
        }
        else
        {
            _chartTimeSeconds += 0.8;
            AppendChartPoint(_chartTimeSeconds, e.Tf1, e.Tf2, e.Ts, e.Tc);
        }

        // 系统消息日志：把本次广播携带的所有消息追加显示
        foreach (var msg in e.Messages)
        {
            AppendLogMessage(msg);
        }
    }

    /// <summary>新一轮升温开始时（Idle）清空曲线数据，重新计时</summary>
    private void ResetChart()
    {
        if (seriesTf1.Points.Count == 0) return; // 已经是空的，不用重复清

        _chartTimeSeconds = 0;
        seriesTf1.Points.Clear();
        seriesTf2.Points.Clear();
        seriesTs.Points.Clear();
        seriesTc.Points.Clear();

        var xAxis = plotModel.Axes[0];
        xAxis.Minimum = 0;
        xAxis.Maximum = PlotWindowSeconds;

        plotModel.InvalidatePlot(true);
    }
}