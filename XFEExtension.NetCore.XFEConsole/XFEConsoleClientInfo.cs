using XFEExtension.NetCore.CyberComm;

namespace XFEExtension.NetCore.XFEConsole;

/// <summary>
/// XFE控制台客户端信息
/// </summary>
/// <param name="clientName">客户端名称</param>
/// <param name="clientUuid">客户端唯一标识符</param>
/// <param name="password">客户端连接时密码</param>
/// <param name="eventArgs">事件参数</param>
public class XFEConsoleClientInfo(string clientName, string clientUuid, string password, CyberCommServerEventArgs eventArgs)
{
    /// <summary>
    /// 客户端名称
    /// </summary>
    public string ClientName { get; set; } = clientName;
    /// <summary>
    /// 客户端唯一标识符
    /// </summary>
    public string ClientUuid { get; set; } = clientUuid;
    /// <summary>
    /// 客户端密码
    /// </summary>
    public string Password { get; set; } = password;
    /// <summary>
    /// 事件参数
    /// </summary>
    public CyberCommServerEventArgs EventArgs { get; set; } = eventArgs;
}
