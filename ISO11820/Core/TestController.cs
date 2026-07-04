using System.Timers;
using MathNet.Numerics;
using Timer = System.Timers.Timer;

namespace ISO11820.Core;

/// <summary>
/// 试验控制器 — 核心状态机。
/// 管理试验5个状态的流转，每800ms执行一次仿真tick。
/// </summary>
public class TestController
{
    private readonly object _lock = new();
    private readonly Timer _timer;
    private readonly Random _rng = new();
    private readonly List<MasterMessage> _pendingMessages = new();

    // ========== 当前状态 ==========

    /// <summary>当前试验状态</summary>
    public TestState State { get; private set; } = TestState.Idle;

    /// <summary>炉温1（TF1）</summary>
    public double Tf1 { get; private set; } = 25.0;

    /// <summary>炉温2（TF2）</summary>
    public double Tf2 { get; private set; } = 24.9;

    /// <summary>表面温度（TS）</summary>
    public double Ts { get; private set; } = 24.5;

    /// <summary>中心温度（TC）</summary>
    public double Tc { get; private set; } = 24.3;

    /// <summary>校准温度（TCal）</summary>
    public double Tcal { get; private set; } = 25.0;

    /// <summary>已记录秒数</summary>
    public int ElapsedSeconds { get; private set; }

    /// <summary>当前样品编号</summary>
    public string CurrentProductId { get; set; } = string.Empty;

    /// <summary>当前试验编号</summary>
    public string CurrentTestId { get; set; } = string.Empty;

    /// <summary>当前试验前质量（g）</summary>
    public double CurrentPreWeight { get; set; }

    /// <summary>当前操作员</summary>
    public string CurrentOperator { get; set; } = string.Empty;

    /// <summary>是否已完成但未保存（flag != "10000000"）</summary>
    public bool HasUnsavedResult { get; set; }

    // ========== 内部状态 ==========

    private int _stableTickCount;           // 稳定计数器
    private int _recordingTickCount;        // 记录已过的tick数
    private double _heatingRate;            // °C/s
    private double _fluctuation;            // 噪声幅度

    // ========== 温漂计算 ==========

    private readonly List<double> _tf1History = new();  // TF1 历史数据（每 tick 一个点）
    private readonly List<double> _tf2History = new();  // TF2 历史数据
    private const int MaxHistoryPoints = 600;           // 最多保留 600 个数据点（约 10 分钟）
    private double _driftTf1;                           // TF1 温漂（°C/10min）
    private double _driftTf2;                           // TF2 温漂（°C/10min）

    // ========== 记录初始温度 ==========
    private double _initialTf1;
    private double _initialTf2;
    private double _initialTs;
    private double _initialTc;

    /// <summary>记录开始时的炉温1</summary>
    public double InitialTf1 => _initialTf1;
    /// <summary>记录开始时的炉温2</summary>
    public double InitialTf2 => _initialTf2;
    /// <summary>记录开始时的表面温度</summary>
    public double InitialTs => _initialTs;
    /// <summary>记录开始时的中心温度</summary>
    public double InitialTc => _initialTc;

    // ========== 记录最终温度（停止记录时锁定） ==========
    private double _finalTf1, _finalTf2, _finalTs, _finalTc;
    /// <summary>停止记录时的炉温1</summary>
    public double FinalTf1 => _finalTf1;
    /// <summary>停止记录时的炉温2</summary>
    public double FinalTf2 => _finalTf2;
    /// <summary>停止记录时的表面温度</summary>
    public double FinalTs => _finalTs;
    /// <summary>停止记录时的中心温度</summary>
    public double FinalTc => _finalTc;

    private TestMode _testMode = TestMode.Standard60Min;  // 试验模式
    private int _targetDurationSeconds = 3600;             // 目标时长（秒），仅 FixedDuration 模式使用
    private int _lastCheckMinute;                          // 上次检查的"分钟"刻度（用于标准模式每5分钟检查）
    private bool _isCoolingDown;                           // 是否正在降温（停止升温后）

    // ========== 事件 ==========

    /// <summary>
    /// 数据广播事件 — 每 800ms 触发一次。
    /// ⚠️ 在后台线程触发，UI 订阅者必须用 Invoke 切回 UI 线程。
    /// </summary>
    public event EventHandler<DataBroadcastEventArgs>? DataBroadcast;

    // ========== 构造 ==========

    public TestController()
    {
        _timer = new Timer(800); // 每 800ms tick 一次
        _timer.Elapsed += OnTimerTick;
        _timer.AutoReset = true;

        UpdateConfig();
    }

    /// <summary>从 AppGlobal 重新读取仿真配置</summary>
    public void UpdateConfig()
    {
        var cfg = AppGlobal.Instance;
        _heatingRate = cfg.HeatingRatePerSecond;
        _fluctuation = cfg.TempFluctuation;
        // 保持当前温度不变（如果已经在升温中）
    }

    // ========== 公开方法：状态切换 ==========

    /// <summary>开始升温：Idle → Preparing</summary>
    public bool StartHeating()
    {
        lock (_lock)
        {
            if (State != TestState.Idle) return false;
            State = TestState.Preparing;
            _stableTickCount = 0;
            _tf1History.Clear();
            _tf2History.Clear();
            _driftTf1 = 0;
            _driftTf2 = 0;

            // 仿真模式下从室温起步
            if (AppGlobal.Instance.EnableSimulation)
            {
                Tf1 = 25.0;
                Tf2 = 24.9;
                Ts = Tf1 * 0.3;
                Tc = Tf1 * 0.25;
                Tcal = Tf1;
            }

            _timer.Start();
            AddMessage("开始升温，系统升温中");
            return true;
        }
    }

    /// <summary>停止升温：Preparing/Ready/Complete → 降温后回到 Idle</summary>
    public bool StopHeating()
    {
        lock (_lock)
        {
            if (State != TestState.Preparing && State != TestState.Ready && State != TestState.Complete)
                return false;
            // 进入降温阶段，不停止定时器，温度逐渐下降
            State = TestState.Complete;
            _isCoolingDown = true;
            AddMessage("停止升温，系统降温中");
            return true;
        }
    }

    /// <summary>开始记录：Ready → Recording</summary>
    public bool StartRecording()
    {
        lock (_lock)
        {
            if (State != TestState.Ready) return false;
            if (HasUnsavedResult) return false;

            State = TestState.Recording;
            ElapsedSeconds = 0;
            _recordingTickCount = 0;
            // 记录初始温度（用于计算温升）
            _initialTf1 = Tf1;
            _initialTf2 = Tf2;
            _initialTs = Ts;
            _initialTc = Tc;
            AddMessage("开始记录，计时开始");
            return true;
        }
    }

    /// <summary>停止记录：Recording → Complete</summary>
    public bool StopRecording()
    {
        lock (_lock)
        {
            if (State != TestState.Recording) return false;
            State = TestState.Complete;
            // 锁定最终温度（停止记录时的温度，而不是降温后的温度）
            _finalTf1 = Tf1;
            _finalTf2 = Tf2;
            _finalTs = Ts;
            _finalTc = Tc;
            AddMessage("用户手动停止记录");
            return true;
        }
    }

    /// <summary>标记已保存：Complete → Preparing（保持炉温）</summary>
    public void MarkSaved()
    {
        lock (_lock)
        {
            State = TestState.Preparing;
            HasUnsavedResult = false;
            AddMessage("试验记录已保存，可进行下一次试验");
        }
    }

    /// <summary>
    /// 设置试验模式。
    /// Standard60Min：标准 60 分钟模式，每 5 分钟检查提前终止条件，60 分钟无条件终止。
    /// FixedDuration：固定时长模式，到达 targetSeconds 后自动终止。
    /// </summary>
    public void SetTestMode(TestMode mode, int targetSeconds = 3600)
    {
        lock (_lock)
        {
            _testMode = mode;
            _targetDurationSeconds = targetSeconds > 0 ? targetSeconds : 3600;
            _lastCheckMinute = 0;
        }
    }

    /// <summary>获取当前试验模式</summary>
    public TestMode CurrentTestMode => _testMode;

    /// <summary>获取目标时长（秒）</summary>
    public int TargetDurationSeconds => _targetDurationSeconds;

    // ========== 核心 tick：每 800ms 执行 ==========

    private void OnTimerTick(object? sender, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            // 降温阶段：持续降温直到接近室温
            if (_isCoolingDown && State == TestState.Complete)
            {
                UpdateTemperatures();
                if (Tf1 <= 30.0)
                {
                    // 降温完成，回到空闲
                    Tf1 = 25.0; Tf2 = 24.9; Ts = 24.5; Tc = 24.3; Tcal = 25.0;
                    _timer.Stop();
                    _isCoolingDown = false;
                    State = TestState.Idle;
                    AddMessage("降温完成，系统已回到室温");
                }
                // 广播降温数据
                var coolArgs = new DataBroadcastEventArgs
                {
                    Tf1 = Math.Round(Tf1, 1), Tf2 = Math.Round(Tf2, 1),
                    Ts = Math.Round(Ts, 1), Tc = Math.Round(Tc, 1),
                    Tcal = Math.Round(Tcal, 1), ElapsedSeconds = 0,
                    State = State, Messages = new List<MasterMessage>(_pendingMessages)
                };
                _pendingMessages.Clear();
                DataBroadcast?.Invoke(this, coolArgs);
                return;
            }

            if (State == TestState.Idle) return;

            // 1. 更新温度（仿真）
            UpdateTemperatures();

            // 2. 记录温度历史（用于温漂计算）
            RecordTemperatureHistory();

            // 3. 计算温漂
            CalculateDrift();

            // 4. 检查状态转移
            CheckStateTransition();

            // 5. 检查终止条件（仅 Recording 状态）
            if (State == TestState.Recording)
            {
                CheckTerminationConditions();
            }

            // 6. 计时更新
            if (State == TestState.Recording)
            {
                _recordingTickCount++;
                ElapsedSeconds++;
            }

            // 7. 广播数据到 UI
            var args = new DataBroadcastEventArgs
            {
                Tf1 = Math.Round(Tf1, 1),
                Tf2 = Math.Round(Tf2, 1),
                Ts = Math.Round(Ts, 1),
                Tc = Math.Round(Tc, 1),
                Tcal = Math.Round(Tcal, 1),
                ElapsedSeconds = ElapsedSeconds,
                State = State,
                DriftTf1 = Math.Round(_driftTf1, 3),
                DriftTf2 = Math.Round(_driftTf2, 3),
                Messages = new List<MasterMessage>(_pendingMessages)
            };
            _pendingMessages.Clear();

            // 在锁外触发事件（避免死锁）
            DataBroadcast?.Invoke(this, args);
        }
    }

    // ========== 温度仿真算法 ==========

    private void UpdateTemperatures()
    {
        var targetTemp = AppGlobal.Instance.TargetFurnaceTemp;

        switch (State)
        {
            case TestState.Preparing:
                // 升温阶段：指数趋近目标温度，避免冲过
                Tf1 += (targetTemp - Tf1) * 0.08 + RandNoise();
                Tf2 += (targetTemp - Tf2) * 0.08 + RandNoise();
                Ts = Tf1 * 0.3 + RandNoise();
                Tc = Tf1 * 0.25 + RandNoise();
                Tcal = Tf1 + RandNoise() * 2;
                break;

            case TestState.Ready:
                // 稳定阶段：钳位到 750°C ± 噪声
                Tf1 = targetTemp + RandNoise();
                Tf2 = targetTemp + RandNoise();
                Ts = Tf1 * 0.3 + RandNoise();
                Tc = Tf1 * 0.25 + RandNoise();
                Tcal = Tf1 + RandNoise() * 2;
                break;

            case TestState.Recording:
                // 保持炉温稳定 + 表面/中心温缓慢上升
                Tf1 = targetTemp + RandNoise();
                Tf2 = targetTemp + RandNoise();
                // 表面温向 炉温×0.95 指数接近
                Ts += (Math.Min(Tf1 * 0.95, 800) - Ts) * 0.02 + RandNoise();
                // 中心温向 炉温×0.85 指数接近（更慢）
                Tc += (Math.Min(Tf1 * 0.85, 750) - Tc) * 0.01 + RandNoise();
                Tcal = Tf1 + RandNoise() * 2;
                break;

            case TestState.Complete:
                // 降温
                Tf1 -= 0.5 + Math.Abs(RandNoise()) * 0.1;
                Tf2 -= 0.5 + Math.Abs(RandNoise()) * 0.1;
                Ts = Tf1 * 0.5;
                Tc = Tf1 * 0.4;
                Tcal = Tf1 + RandNoise() * 2;
                break;
        }
    }

    // ========== 状态转移检查 ==========

    private void CheckStateTransition()
    {
        var targetTemp = AppGlobal.Instance.TargetFurnaceTemp;

        if (State == TestState.Preparing)
        {
            // 检查是否达到稳定条件：745~755°C 且稳定3个tick以上
            bool isInRange = Tf1 >= targetTemp - 5 && Tf1 <= targetTemp + 5;

            if (isInRange)
            {
                _stableTickCount++;
                if (_stableTickCount > 3)
                {
                    State = TestState.Ready;
                    AddMessage("温度已稳定，可以开始记录");
                }
            }
            else
            {
                _stableTickCount = 0;
            }
        }
        else if (State == TestState.Ready)
        {
            // 温度跌出稳定范围 → 回退到 Preparing
            bool isInRange = Tf1 >= targetTemp - 5 && Tf1 <= targetTemp + 5;
            if (!isInRange)
            {
                State = TestState.Preparing;
                _stableTickCount = 0;
            }
        }
    }

    // ========== 温漂计算 ==========

    /// <summary>记录温度历史数据（每 tick 一个点，最多保留 600 个点）</summary>
    private void RecordTemperatureHistory()
    {
        _tf1History.Add(Tf1);
        _tf2History.Add(Tf2);

        // 保持历史数据不超过上限
        while (_tf1History.Count > MaxHistoryPoints)
            _tf1History.RemoveAt(0);
        while (_tf2History.Count > MaxHistoryPoints)
            _tf2History.RemoveAt(0);
    }

    /// <summary>
    /// 使用 MathNet.Numerics 线性回归计算温漂（°C/10min）。
    /// 对最近的历史数据点做最小二乘拟合，斜率 × 每10分钟tick数 = 温漂值。
    /// 数据点不足 10 个时，温漂设为 0。
    /// </summary>
    private void CalculateDrift()
    {
        int count = _tf1History.Count;
        if (count < 10)
        {
            _driftTf1 = 0;
            _driftTf2 = 0;
            return;
        }

        // 构建 x 数组（tick 序号，0 ~ count-1）
        double[] xArr = new double[count];
        for (int i = 0; i < count; i++)
            xArr[i] = i;

        double[] yArr1 = _tf1History.ToArray();
        double[] yArr2 = _tf2History.ToArray();

        // 线性回归：Fit.Line 返回 (intercept, slope)
        var (_, slope1) = Fit.Line(xArr, yArr1);
        var (_, slope2) = Fit.Line(xArr, yArr2);

        // 斜率单位：°C/tick，转换为 °C/10min
        // 10 分钟 = 600 秒，每 tick = 800ms = 0.8s，10 分钟 = 750 tick
        const double ticksPer10Min = 600.0 / 0.8; // 750

        _driftTf1 = slope1 * ticksPer10Min;
        _driftTf2 = slope2 * ticksPer10Min;
    }

    // ========== 终止条件判定 ==========

    /// <summary>
    /// 检查试验终止条件。根据不同模式执行不同策略：
    ///
    /// 标准 60 分钟模式：
    ///   - 每 5 分钟检查一次（t=1800/2100/2400/2700/3000/3300s）
    ///   - 条件：TF1 和 TF2 的 10 分钟温漂均不超过 MaxTemperatureDriftPerTenMinutes
    ///   - 满足条件则提前终止
    ///   - t=3600s 无条件终止
    ///
    /// 固定时长模式：
    ///   - 忽略提前终止检查，到达 _targetDurationSeconds 后终止
    /// </summary>
    private void CheckTerminationConditions()
    {
        int currentMinute = ElapsedSeconds / 60; // 当前分钟数
        int currentSecond = ElapsedSeconds;

        switch (_testMode)
        {
            case TestMode.Standard60Min:
                // 无条件终止：到达 3600 秒
                if (currentSecond >= 3600)
                {
                    State = TestState.Complete;
                    AddMessage("记录时间到达 3600 秒，试验自动结束");
                    return;
                }

                // 每 5 分钟检查一次提前终止条件（t=30,35,40,45,50,55 分钟）
                if (currentMinute >= 30 && currentMinute % 5 == 0 && currentMinute != _lastCheckMinute)
                {
                    _lastCheckMinute = currentMinute;

                    var driftThreshold = AppGlobal.Instance.MaxTemperatureDriftPerTenMinutes;
                    bool tf1Stable = Math.Abs(_driftTf1) <= driftThreshold;
                    bool tf2Stable = Math.Abs(_driftTf2) <= driftThreshold;
                    bool hasEnoughData = _tf1History.Count >= 100; // 至少 100 个数据点（约 80 秒）

                    if (hasEnoughData && tf1Stable && tf2Stable)
                    {
                        State = TestState.Complete;
                        AddMessage(
                            $"满足终止条件，试验结束（温漂 TF1={_driftTf1:F2}°C/10min, " +
                            $"TF2={_driftTf2:F2}°C/10min，均 ≤ {driftThreshold}°C/10min）",
                            "warning");
                    }
                }
                break;

            case TestMode.FixedDuration:
                // 固定时长模式：到达目标秒数
                if (currentSecond >= _targetDurationSeconds)
                {
                    State = TestState.Complete;
                    AddMessage($"记录时间到达 {_targetDurationSeconds} 秒，试验自动结束");
                }
                break;
        }
    }

    // ========== 工具方法 ==========

    private double RandNoise()
    {
        // 返回 [-fluctuation, +fluctuation] 范围的随机噪声
        return (_rng.NextDouble() * 2 - 1) * _fluctuation;
    }

    private void AddMessage(string content, string type = "normal")
    {
        var msg = new MasterMessage
        {
            Time = DateTime.Now.ToString("HH:mm:ss"),
            Content = content,
            Type = type
        };
        _pendingMessages.Add(msg);
    }

    /// <summary>获取当前状态的文字描述</summary>
    public string GetStateText() => State switch
    {
        TestState.Idle => "空闲",
        TestState.Preparing => "升温中",
        TestState.Ready => "就绪",
        TestState.Recording => "记录中",
        TestState.Complete => _isCoolingDown ? "降温中" : "完成",
        _ => "未知"
    };

    /// <summary>是否正在降温</summary>
    public bool IsCoolingDown => _isCoolingDown;
}
