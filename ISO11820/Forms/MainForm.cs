using System;
using System.Drawing;
using System.Windows.Forms;
using ISO11820.Core;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace ISO11820.Forms
{
    /// <summary>
    /// 主窗体 — 包含提交 6（温度面板）、7（曲线）、8（日志）、9（按钮互锁）
    /// 角色 C 负责。版本：2026-07-03
    /// </summary>
    public partial class MainForm : Form
    {
        // ========== 由 A 提供的核心状态机（直接在 MainForm 中创建实例） ==========
        private readonly TestController Controller = new TestController();

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
        private DateTime _chartStartTime;          // 用于计算实际耗时

        // ========== 系统消息日志 ==========
        private RichTextBox rtbLog = null!;

        // ========== 6 个按钮 ==========
        private Button btnNewTest = null!;
        private Button btnStartHeat = null!;
        private Button btnStopHeat = null!;
        private Button btnStartRecord = null!;
        private Button btnStopRecord = null!;
        private Button btnSettings = null!;

        // ========== 构造函数 ==========
        public MainForm()
        {
            Text = "ISO 11820 建筑材料不燃性试验系统";
            Size = new Size(1280, 850);
            StartPosition = FormStartPosition.CenterScreen;

            BuildTemperaturePanel();
            BuildTemperatureChart();
            BuildMessageLog();
            BuildButtons();

            // 订阅数据广播事件
            Controller.DataBroadcast += OnDataBroadcast;

            // 初始按钮状态
            UpdateButtonStates(Controller.State);
        }

        // ==================== 构建温度面板（提交6） ====================
        private void BuildTemperaturePanel()
        {
            var panel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(1230, 150),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            // 5 个温度通道
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

            // 右侧信息区
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

        // ==================== 构建消息日志（提交8） ====================
        private void BuildMessageLog()
        {
            var lblTitle = new Label
            {
                Text = "系统消息日志",
                Font = new Font("微软雅黑", 10, FontStyle.Bold),
                Location = new Point(20, 570),
                Size = new Size(200, 25)
            };

            rtbLog = new RichTextBox
            {
                Location = new Point(20, 595),
                Size = new Size(1230, 150),
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Consolas", 10),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            Controls.Add(lblTitle);
            Controls.Add(rtbLog);
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

        // ==================== 构建温度曲线（提交7） ====================
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
                Location = new Point(20, 180),
                Size = new Size(1230, 380),
                BackColor = Color.FromArgb(20, 20, 20)
            };

            Controls.Add(plotView);
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

        // ==================== 构建按钮 + 互锁逻辑（提交9） ====================
        private void BuildButtons()
        {
            // 重新调整曲线图和日志的位置，为按钮腾出空间
            plotView.Location = new Point(20, 200);
            plotView.Size = new Size(1230, 350);

            rtbLog.Location = new Point(20, 585);
            rtbLog.Size = new Size(1230, 130);

            // 6 个按钮
            btnNewTest = new Button { Text = "新建试验", Location = new Point(20, 560), Size = new Size(100, 30) };
            btnStartHeat = new Button { Text = "开始升温", Location = new Point(130, 560), Size = new Size(100, 30) };
            btnStopHeat = new Button { Text = "停止升温", Location = new Point(240, 560), Size = new Size(100, 30) };
            btnStartRecord = new Button { Text = "开始记录", Location = new Point(350, 560), Size = new Size(100, 30) };
            btnStopRecord = new Button { Text = "停止记录", Location = new Point(460, 560), Size = new Size(100, 30) };
            btnSettings = new Button { Text = "参数设置", Location = new Point(570, 560), Size = new Size(100, 30) };

            btnNewTest.Click += BtnNewTest_Click;
            btnStartHeat.Click += BtnStartHeat_Click;
            btnStopHeat.Click += BtnStopHeat_Click;
            btnStartRecord.Click += BtnStartRecord_Click;
            btnStopRecord.Click += BtnStopRecord_Click;
            btnSettings.Click += BtnSettings_Click;

            Controls.Add(btnNewTest);
            Controls.Add(btnStartHeat);
            Controls.Add(btnStopHeat);
            Controls.Add(btnStartRecord);
            Controls.Add(btnStopRecord);
            Controls.Add(btnSettings);

            UpdateButtonStates(Controller.State);
        }

        /// <summary>
        /// 按钮状态控制矩阵（6按钮 × 5状态）
        /// </summary>
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
                    break;

                default:
                    break;
            }
        }

        // ==================== 按钮点击事件（暂为占位） ====================
        private void BtnNewTest_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("新建试验功能待实现（提交10）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnStartHeat_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("开始升温功能待实现", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnStopHeat_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("停止升温功能待实现", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnStartRecord_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("开始记录功能待实现", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnStopRecord_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("停止记录功能待实现", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnSettings_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("参数设置功能待实现", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ==================== 数据广播事件处理 ====================
        private void OnDataBroadcast(object? sender, DataBroadcastEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnDataBroadcast(sender, e)));
                return;
            }

            if (lblTf1Value == null || plotView == null) return;

            // ---- 更新温度面板 ----
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

            // ---- 更新按钮互锁 ----
            UpdateButtonStates(e.State);

            // ---- 更新曲线 ----
            if (e.State == TestState.Idle)
            {
                ResetChart();
            }
            else
            {
                double realElapsed = (DateTime.Now - _chartStartTime).TotalSeconds;
                AppendChartPoint(realElapsed, e.Tf1, e.Tf2, e.Ts, e.Tc);
            }

            // ---- 更新日志 ----
            foreach (var msg in e.Messages)
            {
                AppendLogMessage(msg);
            }
        }

        // ==================== 窗体关闭时取消订阅 ====================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Controller.DataBroadcast -= OnDataBroadcast;
            base.OnFormClosing(e);
        }
    }
}