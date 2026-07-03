namespace ISO11820.Core;

/// <summary>
/// 试验模式枚举
/// Standard60Min：标准 60 分钟，每 5 分钟检查提前终止条件，60 分钟无条件终止
/// FixedDuration：固定时长，到达设定秒数后自动终止
/// </summary>
public enum TestMode
{
    /// <summary>标准 60 分钟模式（ISO 11820 标准）</summary>
    Standard60Min,

    /// <summary>固定时长模式（用户自定义时长）</summary>
    FixedDuration
}