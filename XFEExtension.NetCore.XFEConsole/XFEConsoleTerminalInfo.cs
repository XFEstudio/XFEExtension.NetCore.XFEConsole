using XFEExtension.NetCore.CyberComm;

namespace XFEExtension.NetCore.XFEConsole;

/// <summary>
/// 连接到调试程序服务器的终端信息。
/// </summary>
public sealed class XFEConsoleTerminalInfo
{
    internal XFEConsoleTerminalInfo(string terminalName, string terminalId, CyberCommServerEventArgs eventArgs)
    {
        TerminalName = terminalName;
        TerminalId = terminalId;
        EventArgs = eventArgs;
    }

    /// <summary>终端名称。</summary>
    public string TerminalName { get; }

    /// <summary>终端唯一标识。</summary>
    public string TerminalId { get; }

    /// <summary>终端的远程 IP 地址。</summary>
    public string IPAddress => EventArgs.IPAddress;

    internal CyberCommServerEventArgs EventArgs { get; }
}
