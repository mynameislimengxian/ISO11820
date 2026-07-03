using ISO11820.Data;
using ISO11820.Services;
using System.Data;
using System.Text;

namespace ISO11820.Forms;

/// <summary>
/// 记录查询 Tab — 提供历史试验记录的查询、查看和导出功能。
/// 作为 UserControl 嵌入到 MainForm 的 TabControl 中。
/// </summary>
public class RecordQueryTab : UserControl
{
    private readonly DbHelper _db;

    // 筛选控件
    private DateTimePicker _dtpFrom;
    private DateTimePicker _dtpTo;
    private TextBox _txtProductId;
    private Button _btnQuery;
    private Label _lblFrom;
    private Label _lblTo;
    private Label _lblProductId;

    // 结果展示
    private DataGridView _dgvResults;
    private Button _btnExportExcel;
    private Button _btnViewDetail;
    private Label _lblStatus;

    // 缓存查询结果
    private List<TestSummary> _currentResults = new();

    public RecordQueryTab(DbHelper db)
    {
        _db = db;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        // ===== 筛选条件区域 =====
        var filterPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 50,
            Padding = new Padding(10, 8, 10, 4),
            ColumnCount = 8,
            RowCount = 1
        };
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _lblFrom = new Label { Text = "开始日期：", TextAlign = ContentAlignment.MiddleRight, AutoSize = true };
        _dtpFrom = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 130 };
        _dtpFrom.Value = DateTime.Today.AddDays(-30);

        _lblTo = new Label { Text = "结束日期：", TextAlign = ContentAlignment.MiddleRight, AutoSize = true };
        _dtpTo = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 130 };
        _dtpTo.Value = DateTime.Today;

        _lblProductId = new Label { Text = "样品编号：", TextAlign = ContentAlignment.MiddleRight, AutoSize = true };
        _txtProductId = new TextBox { Width = 110 };

        _btnQuery = new Button { Text = "查询", Width = 70, Height = 28 };
        _btnQuery.Click += BtnQuery_Click;

        filterPanel.Controls.Add(_lblFrom, 0, 0);
        filterPanel.Controls.Add(_dtpFrom, 1, 0);
        filterPanel.Controls.Add(_lblTo, 2, 0);
        filterPanel.Controls.Add(_dtpTo, 3, 0);
        filterPanel.Controls.Add(_lblProductId, 4, 0);
        filterPanel.Controls.Add(_txtProductId, 5, 0);
        filterPanel.Controls.Add(_btnQuery, 6, 0);

        // ===== DataGridView =====
        _dgvResults = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false
        };
        _dgvResults.DoubleClick += DgvResults_DoubleClick;
        _dgvResults.SelectionChanged += DgvResults_SelectionChanged;

        // ===== 底部按钮栏 =====
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            Padding = new Padding(10, 4, 10, 4)
        };

        _btnViewDetail = new Button { Text = "查看详情", Width = 90, Enabled = false, Left = 0, Top = 4 };
        _btnViewDetail.Click += BtnViewDetail_Click;

        _btnExportExcel = new Button { Text = "导出选中为 Excel", Width = 130, Enabled = false, Left = 100, Top = 4 };
        _btnExportExcel.Click += BtnExportExcel_Click;

        _lblStatus = new Label
        {
            Text = "就绪",
            AutoSize = true,
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _lblStatus.Location = new Point(240, 10);

        bottomPanel.Controls.Add(_btnViewDetail);
        bottomPanel.Controls.Add(_btnExportExcel);
        bottomPanel.Controls.Add(_lblStatus);

        // ===== 组装 =====
        Controls.Add(_dgvResults);
        Controls.Add(filterPanel);
        Controls.Add(bottomPanel);

        ResumeLayout(false);
    }

    // ========================================================================
    // 事件处理
    // ========================================================================

    private void BtnQuery_Click(object? sender, EventArgs e)
    {
        try
        {
            _lblStatus.Text = "查询中...";
            _lblStatus.ForeColor = Color.Gray;
            Cursor = Cursors.WaitCursor;

            var productId = string.IsNullOrWhiteSpace(_txtProductId.Text) ? null : _txtProductId.Text.Trim();
            _currentResults = _db.QueryTests(_dtpFrom.Value, _dtpTo.Value, productId);

            BindResults(_currentResults);

            _lblStatus.Text = $"共 {_currentResults.Count} 条记录";
            _lblStatus.ForeColor = Color.Green;
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"查询失败：{ex.Message}";
            _lblStatus.ForeColor = Color.Red;
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void DgvResults_DoubleClick(object? sender, EventArgs e)
    {
        ShowDetail();
    }

    private void DgvResults_SelectionChanged(object? sender, EventArgs e)
    {
        var hasSelection = _dgvResults.SelectedRows.Count > 0;
        _btnViewDetail.Enabled = hasSelection;
        _btnExportExcel.Enabled = hasSelection;
    }

    private void BtnViewDetail_Click(object? sender, EventArgs e)
    {
        ShowDetail();
    }

    private void BtnExportExcel_Click(object? sender, EventArgs e)
    {
        ExportToExcel();
    }

    // ========================================================================
    // 核心逻辑
    // ========================================================================

    private void BindResults(List<TestSummary> results)
    {
        _dgvResults.DataSource = null;
        _dgvResults.Columns.Clear();

        if (results.Count == 0)
        {
            _dgvResults.DataSource = null;
            return;
        }

        // 手动绑定列以控制中文列名
        var dt = new System.Data.DataTable();
        dt.Columns.Add("ProductId", typeof(string));
        dt.Columns.Add("TestId", typeof(string));
        dt.Columns.Add("ProductName", typeof(string));
        dt.Columns.Add("TestDate", typeof(DateTime));
        dt.Columns.Add("Operator", typeof(string));
        dt.Columns.Add("TotalTestTime", typeof(int));
        dt.Columns.Add("LostWeightPer", typeof(double));
        dt.Columns.Add("DeltaTf", typeof(double));
        dt.Columns.Add("Status", typeof(string));

        foreach (var r in results)
        {
            dt.Rows.Add(
                r.ProductId,
                r.TestId,
                r.ProductName,
                r.TestDate,
                r.Operator,
                r.TotalTestTime,
                r.LostWeightPer,
                r.DeltaTf,
                r.Flag == "10000000" ? "已保存" : "未保存"
            );
        }

        _dgvResults.DataSource = dt;

        // 设置列名
        var colNames = new[] { "样品编号", "试验ID", "样品名称", "试验日期", "操作员", "时长(秒)", "失重率(%)", "温升(°C)", "状态" };
        for (int i = 0; i < colNames.Length && i < _dgvResults.Columns.Count; i++)
        {
            _dgvResults.Columns[i].HeaderText = colNames[i];
        }

        // 日期列格式化
        if (_dgvResults.Columns.Count > 3)
            _dgvResults.Columns[3].DefaultCellStyle.Format = "yyyy-MM-dd";
    }

    private void ShowDetail()
    {
        if (_dgvResults.SelectedRows.Count == 0) return;

        var row = _dgvResults.SelectedRows[0];
        var productId = row.Cells[0].Value?.ToString() ?? "";
        var testId = row.Cells[1].Value?.ToString() ?? "";

        try
        {
            var test = _db.GetTest(productId, testId);
            if (test == null)
            {
                MessageBox.Show("未找到该试验记录。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var product = _db.GetProduct(productId);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("========== 试验详细记录 ==========");
            sb.AppendLine();
            sb.AppendLine($"样品编号：{test.ProductId}");
            sb.AppendLine($"样品名称：{product?.ProductName ?? "—"}");
            sb.AppendLine($"试验 ID：{test.TestId}");
            sb.AppendLine($"试验日期：{test.TestDate:yyyy-MM-dd}");
            sb.AppendLine($"操作员：{test.Operator}");
            sb.AppendLine($"设备名称：{test.ApparatusName}");
            sb.AppendLine($"环境温度：{test.AmbTemp:F1} °C");
            sb.AppendLine($"环境湿度：{test.AmbHumi:F1} %");
            sb.AppendLine($"试验依据：{test.According}");
            sb.AppendLine($"试验时长：{test.TotalTestTime} 秒");
            sb.AppendLine($"恒功率值：{test.ConstPower}");
            sb.AppendLine("----------------------------------");
            sb.AppendLine($"试验前质量：{test.PreWeight:F2} g");
            sb.AppendLine($"试验后质量：{test.PostWeight:F2} g");
            sb.AppendLine($"失重量：{test.LostWeight:F2} g");
            sb.AppendLine($"失重率：{test.LostWeightPer:F2} %");
            sb.AppendLine("----------------------------------");
            sb.AppendLine($"炉温1 最大值：{test.MaxTf1:F1} °C (@ {test.MaxTf1Time}s)");
            sb.AppendLine($"炉温2 最大值：{test.MaxTf2:F1} °C (@ {test.MaxTf2Time}s)");
            sb.AppendLine($"表面温 最大值：{test.MaxTs:F1} °C (@ {test.MaxTsTime}s)");
            sb.AppendLine($"中心温 最大值：{test.MaxTc:F1} °C (@ {test.MaxTcTime}s)");
            sb.AppendLine("----------------------------------");
            sb.AppendLine($"炉温1 温升：{test.DeltaTf1:F2} °C");
            sb.AppendLine($"炉温2 温升：{test.DeltaTf2:F2} °C");
            sb.AppendLine($"样品温升：{test.DeltaTf:F2} °C");
            sb.AppendLine($"表面温 温升：{test.DeltaTs:F2} °C");
            sb.AppendLine($"中心温 温升：{test.DeltaTc:F2} °C");
            sb.AppendLine("----------------------------------");
            if (test.FlameDuration > 0)
            {
                sb.AppendLine($"火焰发生时刻：{test.FlameTime} 秒");
                sb.AppendLine($"火焰持续时间：{test.FlameDuration} 秒");
            }
            else
            {
                sb.AppendLine("持续火焰：未出现");
            }
            if (!string.IsNullOrEmpty(test.Memo))
                sb.AppendLine($"备注：{test.Memo}");
            sb.AppendLine($"状态：{(test.Flag == "10000000" ? "已保存" : "未保存")}");

            MessageBox.Show(sb.ToString(), "试验详情", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"获取详情失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportToExcel()
    {
        if (_dgvResults.SelectedRows.Count == 0) return;

        try
        {
            var row = _dgvResults.SelectedRows[0];
            var productId = row.Cells[0].Value?.ToString() ?? "";
            var testId = row.Cells[1].Value?.ToString() ?? "";

            var excelPath = Services.FileStorageManager.GetExcelPath(testId);
            var csvPath = Services.FileStorageManager.GetCsvPath(productId, testId);

            // 使用兰竣雯的 ExcelExportService
            var excelService = new ExcelExportService(_db);
            excelService.Export(productId, testId, excelPath, csvPath);

            MessageBox.Show($"Excel 已导出到：\n{excelPath}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}