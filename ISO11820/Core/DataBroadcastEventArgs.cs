namespace ISO11820.Core;

/// <summary>
/// 数据广播事件参数 — 从后台线程推送温度数据到 UI
/// </summary>
public class DataBroadcastEventArgs : EventArgs
{
    /// <summary>炉温1（TF1），°C</summary>
    public double Tf1 { get; set; }

    /// <summary>炉温2（TF2），°C</summary>
    public double Tf2 { get; set; }

    /// <summary>表面温度（TS），°C</summary>
    public double Ts { get; set; }

    /// <summary>中心温度（TC），°C</summary>
    public double Tc { get; set; }

    /// <summary>校准温度（TCal），°C</summary>
    public double Tcal { get; set; }

    /// <summary>已记录秒数（仅在 Recording 状态下计时）</summary>
    public int ElapsedSeconds { get; set; }

    /// <summary>当前试验状态</summary>
    public TestState State { get; set; }

    /// <summary>炉温1 温漂（°C/10min），基于最近600个数据点的线性回归斜率</summary>
    public double DriftTf1 { get; set; }

    /// <summary>炉温2 温漂（°C/10min）</summary>
    public double DriftTf2 { get; set; }

    /// <summary>本次广播携带的系统消息列表</summary>
    public List<MasterMessage> Messages { get; set; } = new();
}
