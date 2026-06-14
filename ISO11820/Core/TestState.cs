namespace ISO11820.Core;

/// <summary>
/// 试验状态枚举
/// Idle(空闲) → Preparing(升温中) → Ready(就绪) → Recording(记录中) → Complete(完成)
/// </summary>
public enum TestState
{
    /// <summary>空闲 — 初始状态，可新建试验、开始升温</summary>
    Idle,

    /// <summary>升温中 — 炉温上升，等待达到 745~755°C 且稳定</summary>
    Preparing,

    /// <summary>就绪 — 温度已稳定，等待用户点击开始记录</summary>
    Ready,

    /// <summary>记录中 — 每秒记录温度数据，等待试验结束</summary>
    Recording,

    /// <summary>完成 — 试验结束，等待用户保存现象记录</summary>
    Complete
}
