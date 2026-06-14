using System.Timers;
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

    /// <summary>当前操作员</summary>
    public string CurrentOperator { get; set; } = string.Empty;

    /// <summary>是否已完成但未保存（flag != "10000000"）</summary>
    public bool HasUnsavedResult { get; set; }

    // ========== 内部状态 ==========

    private int _stableTickCount;           // 稳定计数器
    private int _recordingTickCount;        // 记录已过的tick数
    private double _heatingRate;            // °C/s
    private double _fluctuation;            // 噪声幅度

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

    /// <summary>停止升温：Preparing/Ready → Idle</summary>
    public bool StopHeating()
    {
        lock (_lock)
        {
            if (State != TestState.Preparing && State != TestState.Ready && State != TestState.Complete)
                return false;
            _timer.Stop();
            State = TestState.Idle;
            AddMessage("用户手动停止升温");
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

    // ========== 核心 tick：每 800ms 执行 ==========

    private void OnTimerTick(object? sender, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            if (State == TestState.Idle) return;

            // 1. 更新温度（仿真）
            UpdateTemperatures();

            // 2. 检查状态转移
            CheckStateTransition();

            // 3. 计时更新
            if (State == TestState.Recording)
            {
                _recordingTickCount++;
                ElapsedSeconds++;
            }

            // 4. 广播数据到 UI
            var args = new DataBroadcastEventArgs
            {
                Tf1 = Math.Round(Tf1, 1),
                Tf2 = Math.Round(Tf2, 1),
                Ts = Math.Round(Ts, 1),
                Tc = Math.Round(Tc, 1),
                Tcal = Math.Round(Tcal, 1),
                ElapsedSeconds = ElapsedSeconds,
                State = State,
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
                // 升温阶段：TF1/TF2 向目标温度前进
                Tf1 += _heatingRate * 0.8 + RandNoise();
                Tf2 += _heatingRate * 0.8 + RandNoise();
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
        TestState.Complete => "完成",
        _ => "未知"
    };
}
