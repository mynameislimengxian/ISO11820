namespace ISO11820.Core;

/// <summary>
/// 系统消息数据结构
/// </summary>
public class MasterMessage
{
    /// <summary>消息时间，格式 HH:mm:ss</summary>
    public string Time { get; set; } = string.Empty;

    /// <summary>消息内容</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>消息颜色：normal / warning</summary>
    public string Type { get; set; } = "normal";
}
