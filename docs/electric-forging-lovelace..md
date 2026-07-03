# ISO 11820 建筑材料不燃性试验仿真系统 — 4人团队分工方案

> **工作方式**：每人使用 AI 大模型辅助完成各自的文档和代码模块。
> **分工原则**：一个人负责一整份文档（主笔），不拆散；一个人负责一整类代码模块，各自用 AI 指导生成。
> **Git 提交**：按代码依赖顺序依次提交，每次提交标注负责人姓名，记录谁指导 AI 完成了什么。

---

## 一、考核标准与交付物

| 序号 | 交付物 | 占比 | 交付格式 |
|:---:|--------|:----:|---------|
| 1 | **需求分析报告** | 平时 40% × 40% = 16% | .docx |
| 2 | **概要设计报告** | 平时 40% × 30% = 12% | .docx |
| 3 | **详细设计报告** | 平时 40% × 30% = 12% | .docx |
| 4 | **软件作品**（源码 + 可运行程序） | 期末 60% × 40% = 24% | 源码 + .exe |
| 5 | **软件系统研究报告** | 期末 60% × 30% = 18% | .docx |
| 6 | **答辩**（PPT + 演示视频） | 期末 60% × 30% = 18% | .pptx + .mp4 |

---

## 二、4人分工总览（文档 + 代码）

| 角色 | 文档任务 | 代码任务 |
|:---:|---------|---------|
| **A（组长）** | ① 需求分析报告（主笔）✅<br>② 软件系统研究报告（主笔）<br>③ 答辩 PPT（主笔） | Core 层补充 + 集成联调 + 打包发布 |
| **B** | ④ 概要设计报告（主笔）✅<br>⑤ 详细设计报告（主笔）✅ | Data 层（数据库操作）+ CSV/Excel 导出服务 |
| **C** | ⑥ 各文档界面相关插图/截图 | Forms 层（全部 UI 界面） |
| **D** | ⑦ 演示视频录制<br>⑧ 研究报告"测试"章节提供素材 | Services 层（PDF 导出 + 文件管理）+ 测试<br>+ 记录查询 Tab + 设备校准 Tab |

> **核心原则**：文档不拆散——一份文档一个人从头写到尾，避免多人合写一章造成的风格不一致和沟通成本。
> 代码也是一个人负责一整层或一类功能，分模块独立完成。

---

## 三、代码开发 — 按依赖顺序推进 + Git 提交计划

> 代码模块按依赖关系排序，依次完成。每个成员在自己的模块中指导 AI 编写代码，完成后提交 Git。
> 已有基础代码：Core 层（AppGlobal / TestController 状态机+仿真 / TestState / 事件参数）已完成，Forms 层为空壳骨架，Data / Services 层待建。

### 提交序列（按依赖顺序，标注负责人）

| 序号 | 提交信息 | 负责人 | 所属模块 |
|:---:|---------|:---:|------|
| 1 | `feat: 数据库建表与初始数据` | B | Data |
| 2 | `feat: DbHelper 核心 CRUD（登录/新建/更新/查询）` | B | Data |
| 3 | `feat: 温漂计算（MathNet 线性回归）` | A | Core |
| 4 | `feat: 终止条件判定（标准模式+固定时长+手动）` | A | Core |
| 5 | `feat: 登录窗体（角色选择+密码验证）` | C | Forms |
| 6 | `feat: 主界面温度面板（5通道LED显示+计时器+温漂+状态）` | C | Forms |
| 7 | `feat: OxyPlot 实时温度曲线（4线+滚动窗口）` | C | Forms |
| 8 | `feat: 系统消息日志（彩色文字+时间戳）` | C | Forms |
| 9 | `feat: 按钮互锁逻辑（6按钮×5状态控制矩阵）` | C | Forms |
| 10 | `feat: 新建试验窗体（环境+样品+参数+设备）` | C | Forms |
| 11 | `feat: 试验现象记录窗体（火焰+质量+自动计算）` | C | Forms |
| 12 | `feat: 记录查询 Tab（DataGridView+筛选+详情）` | D | Forms |
| 13 | `feat: 设备校准 Tab（校准温显示+记录保存+历史）` | D | Forms |
| 14 | `feat: CSV 导出服务（试验完成自动生成）` | B | Services |
| 15 | `feat: Excel 导出服务（三Sheet+嵌入图表）` | B | Services |
| 16 | `feat: PDF 报告生成（试验概要+曲线图+判定结论）` | D | Services |
| 17 | `feat: 文件存储管理（目录创建+路径管理）` | D | Services |
| 18 | `feat: 完整流程联调通过 + 修 Bug` | A | 联调 |
| 19 | `test: 核心流程测试用例` | D | 测试 |
| 20 | `feat: 打包发布（Release + win-x64 self-contained）` | A | 发布 |

**统计**：A 5 次 / B 4 次 / C 7 次 / D 5 次（共 21 次，远超考核要求的 4×4=16 次最低线）

---

## 四、各模块详细说明

### 4-1 B — Data 层（数据库操作）

> 数据库是所有功能的基础，B 率先完成，为 A 和 C 提供数据支撑。

**产出文件**：
- `DatabaseInitializer.cs`：首次运行自动建库建表（6 张表） + 插入初始数据（admin/experimenter、FURNACE-01 设备、17 个传感器通道）
- `DbHelper.cs`：封装所有数据库操作（Microsoft.Data.Sqlite 原生 SQL，无 ORM）

**DbHelper 提供的 API**（参照 `接口约定.txt`）：
- `Login(username, pwd)` → 登录验证，返回 (是否通过, 用户ID, 角色)
- `InsertTest(...)` → 新建试验记录，所有统计字段初始填 0
- `UpdateTestResult(...)` → 试验完成后回填失重率/温升/时长/现象编码，flag 置 "10000000"
- `QueryTests(from, to, productId)` → 历史试验查询，按日期倒序
- 设备读写、校准记录读写

**参考文档**：`DB-数据库设计.md` 中有完整的 SQL 和 C# 代码示例，直接指导 AI 生成即可。

**检查点**：程序启动自动建库成功，admin/123456 能登录，新建试验能写入数据库。

---

### 4-2 A — Core 层补充（温漂 + 终止条件）

> TestController 状态机+仿真引擎已基本完成，A 负责补充两个关键算法。

**产出内容**：

| 功能 | 实现方式 |
|------|---------|
| **温漂计算** | MathNet.Numerics `SimpleRegression.Fit(xArr, yArr)` → `regression.Item2` 即斜率（°C/tick），乘以 `(600/ticksPer10Min)` 得到 °C/10min 温漂值。在主界面实时显示，绝对值 < 阈值自动判定稳定 |
| **终止条件判定** | 标准 60 分钟模式：每 5 分钟检查（t=1800/2100/2400/2700/3000/3300s），或 t=3600s 无条件终止；固定时长模式：到达设定秒数终止；手动终止：StopRecording() |
| **判定结论** | `deltatf ≤ 50 && lostweight_per ≤ 50 && flameduration < 5` → "通过"，否则"不通过" |

**检查点**：试验中温漂值实时更新，到达终止条件后自动进入 Complete 状态。

---

### 4-3 C — Forms 层（全部 UI 界面）

> C 负责人机交互，所有窗体依次开发，每次完成后编译验证。

| 顺序 | 窗体/组件 | 功能要点 |
|:---:|---------|---------|
| ① | `LoginForm` | 窗体 450×350 固定对话框；角色 RadioButton（管理员/试验员）+ 密码 TextBox（PasswordChar='*'）+ 登录 Button；调用 DbHelper.Login()，失败提示"密码错误，请重新输入" |
| ② | `MainForm` 温度面板 | 5 个 Label 显示 TF1/TF2/TS/TC/Tcal 当前值（LED 风格字体+颜色），计时器显示记录时长，温漂值，当前状态（中文），样品编号。订阅 DataBroadcast 事件，**所有更新用 Invoke 回到 UI 线程** |
| ③ | `MainForm` OxyPlot 曲线 | 4 条折线（TF1 红/TF2 橙/TS 蓝/TC 绿），X 轴时间（秒）滚动 10 分钟窗口，Y 轴固定 0~800°C；在 Invoke 回调里更新 PlotModel + `InvalidatePlot(true)` |
| ④ | `MainForm` 消息日志 | RichTextBox，每条消息格式 "HH:mm:ss 内容"，普通消息黑色，警告消息橙红色，每次追加后 `ScrollToCaret()` |
| ⑤ | `MainForm` 按钮互锁 | 6 个按钮（新建试验/开始升温/停止升温/开始记录/停止记录/参数设置），根据 5 个状态启用/禁用，直接按状态机矩阵控制 `.Enabled` |
| ⑥ | `NewTestForm` | 弹出对话框，环境信息（温度/湿度自动读取）、样品信息（编号/名称/规格/直径/高度/质量）、试验参数（模式/时长）、设备信息自动带出；必填验证通过后调用 DbHelper.InsertTest() + TestController.GoToPreparing() |
| ⑦ | 试验现象记录窗体 | 持续火焰 CheckBox + 火焰时刻/持续时间（火焰时才启用）、试验后质量（必填）、备注；保存时自动计算失重率= (pre-post)/pre×100%、温升=finalTS - 初始温度，调用 DbHelper.UpdateTestResult()，flag→"10000000" |

**⚠️ 跨线程要点**：DataBroadcast 事件在后台线程触发，C 的所有 UI 更新必须 `Invoke`：

```csharp
private void OnDataBroadcast(object sender, DataBroadcastEventArgs e)
{
    if (this.InvokeRequired)
    {
        this.Invoke(new Action(() => OnDataBroadcast(sender, e)));
        return;
    }
    // 安全操作 UI 控件
    lblTF1.Text = $"{e.Tf1:F1} °C";
    // ...
}
```

**检查点**：每完成一个窗体即编译运行，确保 UI 正常、不崩溃。

---

### 4-4 D — Services 层 + 查询/校准 Tab（PDF 导出 + 文件管理 + 两个 Tab）

> D 负责文件级别的导出和管理，也是最后一批代码模块。

**产出文件**：
- `PdfExportService.cs`：使用 PDFsharp-MigraDoc 6.x，生成 PDF 报告。内容包括：试验概要信息（样品/日期/操作员/判定结论）、温度曲线截图（从 OxyPlot 导出 PNG 嵌入 PDF）、统计汇总（温升/失重率/最长火焰时间）。**注意中文字体配置**（需指定微软雅黑或 SimSun 路径）
- `FileStorageManager.cs`：自动创建目录 `{BaseDir}\TestData\{ProductId}\{TestId}\` 和 `{BaseDir}\Reports\`；提供统一路径生成方法 `GetCsvPath()`、`GetReportPath()`
- 测试：编写测试检查清单，执行全流程回归测试，记录 Bug 并修复
此外，D 还负责 Forms 层的两个 Tab 页面：
- 记录查询 Tab（提交序号 12）：DataGridView 绑定查询结果，顶部筛选区（日期范围 DateTimePicker + 样品编号 TextBox + 查询按钮），双击行查看详情，选中行可导出 Excel
- 设备校准 Tab（提交序号 13）：实时显示校准温度（Tcal 通道），记录校准数据（9 个热电偶测温点），计算均值+偏差，保存 CalibrationRecords，下方 DataGridView 展示历史校准记录

**检查点**：PDF 能正常生成、中文不显示为方块、目录自动创建、文件路径正确。

---

### 4-5 A — 集成联调

> A 作为组长，串联 B/C/D 的模块，确保完整流程可走通。

**联调流程**：

```
登录 → 新建试验 → 开始升温 → 温度稳定自动就绪
→ 开始记录（计时+实时曲线+消息日志）

→ 标准模式第60分钟自动终止 / 手动停止记录
→ 试验现象记录 → 保存（生成 CSV+PDF）
→ 记录查询 Tab 可查到 → 导出 Excel
```

**重点检查项**：
- CSV 文件内容正确（路径 + 格式）
- Excel 三 Sheet 正确、图表正常
- PDF 中文正常、曲线图嵌入正常
- 记录查询能展示完整字段
- 按钮状态在 5 个状态下全部正确
- 温度曲线流畅刷新、无卡顿

---

### 4-6 D — 测试

> D 负责全流程回归测试，确保软件稳定。

| 测试类型 | 内容 |
|---------|------|
| 核心流程 | 完整走一遍（登录→新建→升温→就绪→记录→完成→保存→导出→查询） |
| 边界值 | 密码错误提示、必填字段未填、未保存时禁止新建试验、Ready 状态温度回落自动回到 Preparing |
| 按钮状态 | 遍历 5 个状态，逐一验证 6 个按钮的启用/禁用 |
| 导出验证 | CSV/Excel/PDF 三种文件打开检查，Excel 图表正确，PDF 中文正常 |
| 稳定性 | 完整 60 分钟（加速）流程不崩溃，内存无泄漏 |

**验收标准**：上述全部通过，输出一份测试结果清单。

---

### 4-7 A — 打包发布

```bash
dotnet publish ISO11820/ISO11820/ISO11820.csproj -c Release -r win-x64 --self-contained
```

将 `publish` 目录打包为 zip，拷贝到任意 Windows 10/11 机器解压即可运行。

---

## 五、文档分工（简洁版）

| 文档 | 负责人 | 方式 | 状态 |
|------|:---:|------|:---:|
| 需求分析报告 | **A 主笔** | AI 辅助生成全文档，A 审核修改 | ✅ 已完成 |
| 概要设计报告 | **B 主笔** | AI 辅助生成全文档，B 审核修改 | ✅ 已完成 |
| 详细设计报告 | **B 主笔** | AI 辅助生成全文档，B 审核修改 | ✅ 已完成 |
| 软件系统研究报告 | **A 主笔** | AI 辅助生成全文档（6 章完整），D 提供测试章节素材 | ⬜ 待做 |
| 答辩 PPT | **A 主笔** | AI 生成 PPT 框架+内容，A 调整排版 | ⬜ 待做 |
| 演示视频 | **D 录制** | D 操作软件录制 5 分钟演示视频 | ⬜ 待做 |

> **不拆散文档**：每份文档由一个人独立完成，统一风格、避免碎片化沟通。

### 各文档内容概要

**概要设计报告**（B 主笔）：
- 系统架构设计（分层架构图：Forms → Core → Data/Services/Simulation）
- 模块划分（6 大模块清单及职责）
- 技术选型（.NET 8 / WinForms / SQLite / OxyPlot / EPPlus / PDFsharp / MathNet，逐项说明理由）
- 数据库设计（6 张表 ER 关系图）
- UI 设计（主界面布局草图、各窗体功能说明）
- 接口设计（AppGlobal 单例、TestController API、DataBroadcast 事件、DbHelper API）

**详细设计报告**（B 主笔）：
- 核心模块详细设计（状态机流转图 + 仿真算法公式 + 温漂计算公式）
- 数据库物理设计（完整建表 SQL + 索引 + 初始数据）
- 界面详细设计（控件清单表 + 事件流程：按钮点击 → TestController → DataBroadcast → UI 更新）
- 按钮状态控制矩阵（6 按钮 × 5 状态）
- 导出格式规范（CSV 列定义 / Excel 三 Sheet 结构 / PDF 内容结构）
- 测试计划（测试策略 + 用例清单）

**软件系统研究报告**（A 主笔）：
- 第 1 章 引言（项目背景、研究意义）
- 第 2 章 ISO 11820 标准解读（750°C / 60 分钟 / 判定标准）
- 第 3 章 系统核心实现（状态机 + 仿真引擎 + 温漂 + 数据库）
- 第 4 章 系统界面与交互设计（截图 + 说明）
- 第 5 章 系统测试与分析（D 提供测试素材）
- 第 6 章 总结（工作收获 + 不足与展望）

---

## 六、技术避坑指南（全员共享）

| 坑 | 避免方法 |
|---|---------|
| AI 生成代码无法编译 | 每次生成后立即 `dotnet build`，不要攒到最后 |
| WinForms 跨线程 UI 崩溃 | **所有 UI 更新必须用 `Invoke`**；DataBroadcast 事件在后台线程触发 |
| OxyPlot 曲线不刷新 | Invoke 里更新 PlotModel + `InvalidatePlot(true)` |
| SQLite 路径错误 | 用 `Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ISO11820.db")` |
| NuGet 版本冲突 | 已锁定：OxyPlot 2.1.2 / EPPlus 7.4.1 / PDFsharp 6.1.1 / Sqlite 8.0.0 / MathNet 5.0.0 |
| 登录字段错误 | operators 表按 `username + pwd` 查询，**不用 userid** |
| 试验主键遗忘 | testmaster 联合主键 `(productid, testid)`，查询必须两个值 |
| 表名大小写 | `CalibrationRecords` PascalCase，其余小写 |
| 未保存试验阻塞 | `flag != "10000000"` 时禁止新建试验和开始记录 |
| Complete → Preparing | 保存后状态回 Preparing（保持炉温），不是 Idle |
| PDF 中文乱码 | PDFsharp 必须指定中文字体路径 |
| EPPlus 许可 | 加 `ExcelPackage.LicenseContext = LicenseContext.NonCommercial` |
| 温漂计算数据点 | 取最近 600 个数据点做线性回归，刚启动时不足 600 个则用已有数据 |
| CSV 目录不存在 | ExportCsv 前先 `Directory.CreateDirectory()` |

---

## 七、Git 提交规范

| 规范项 | 要求 |
|--------|------|
| 仓库 | 同一 GitHub 仓库，`main` 分支 |
| 提交格式 | `feat: 中文描述` / `fix: 中文描述` / `docs: 中文描述` / `test: 中文描述` |
| 提交者 | 必须用自己的 GitHub 账号提交，**禁止代提交**（证明每个人参与了开发） |
| 最低提交数 | A≥5 次 / B≥4 次 / C≥7 次 / D≥5 次 |
| 提交信息模板 | `feat: <功能描述>` — 每次提交对应上面提交序列表中的一条 |

---

## 八、最终交付物清单

| # | 交付物 | 格式 | 负责人 |
|:---:|--------|------|:---:|
| 1 | 需求分析报告 | .docx | A ✅ |
| 2 | 概要设计报告 | .docx | B ✅ |
| 3 | 详细设计报告 | .docx | B ✅ |
| 4 | 完整源码 + Git 历史 | GitHub 仓库 | 全员 |
| 5 | 可运行程序 | 发布版 .exe | A |
| 6 | 软件系统研究报告 | .docx | A |
| 7 | 答辩 PPT | .pptx | A |
| 8 | 演示视频 | .mp4 | D |

---

## 九、现有代码资产（阶段基线）

| 层 | 文件 | 状态 | 负责人 |
|----|------|:---:|:---:|
| Core | `AppGlobal.cs` / `TestController.cs` / `TestState.cs` / `DataBroadcastEventArgs.cs` / `MasterMessage.cs` | ✅ 已完成 | A |
| Forms | `LoginForm.cs` / `MainForm.cs` | ⬜ 空壳骨架 | C |
| Data | `DbHelper.cs` / `DatabaseInitializer.cs` | ⬜ 待建 | B |
| Services | CSV/Excel/PDF/FileStorage | ⬜ 待建 | B+D |
| 配置 | `appsettings.json` | ✅ 完成 | A |
| 文档 | `接口约定.txt` / `DB-数据库设计.md` | ✅ 完成 | A+B |
