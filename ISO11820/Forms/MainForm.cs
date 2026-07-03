using ISO11820.Core;

namespace ISO11820.Forms;

/// <summary>
/// 主窗体 — 试验控制 + 实时数据显示 + 曲线图 + 记录查询。
/// 角色 C 负责。当前阶段：温度面板（②）。
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

    public MainForm()
    {
        Text = "ISO 11820 建筑材料不燃性试验系统";
        Size = new Size(1280, 800);
        StartPosition = FormStartPosition.CenterScreen;

        BuildTemperaturePanel();

        // 订阅数据广播事件（在后台线程触发，必须用 Invoke 切回 UI 线程）
        _controller.DataBroadcast += OnDataBroadcast;
    }

    private void BuildTemperaturePanel()
    {
        var panel = new Panel
        {
            Location = new Point(20, 20),
            Size = new Size(1230, 150),
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

        Controls.Add(panel);
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

    // ========== 数据广播事件处理 

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

        // TODO: e.Messages 将在「④ 系统消息日志」阶段接入 RichTextBox
        // TODO: 曲线图更新将在「③ OxyPlot 实时温度曲线」阶段接入
    }
}
