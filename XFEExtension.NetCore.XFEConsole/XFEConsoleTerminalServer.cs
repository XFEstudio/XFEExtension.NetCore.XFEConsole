using System.Net.WebSockets;
using XFEExtension.NetCore.CyberComm;
using XFEExtension.NetCore.DelegateExtension;

namespace XFEExtension.NetCore.XFEConsole;

/// <summary>
/// XFE控制台终端服务器
/// </summary>
public class XFEConsoleTerminalServer
{
    /// <summary>
    /// 服务器
    /// </summary>
    public CyberCommServer Server { get; set; }
    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; }
    /// <summary>
    /// Socket客户端-客户端信息字典
    /// </summary>
    public Dictionary<WebSocket, XFEConsoleClientInfo> ClientInfoDictionary { get; set; } = [];
    /// <summary>
    /// 客户端连接事件
    /// </summary>
    public event XFEEventHandler<XFEConsoleTerminalServer, XFEConsoleClientInfo>? Connected;
    /// <summary>
    /// 客户端断开连接事件
    /// </summary>
    public event XFEEventHandler<XFEConsoleTerminalServer, XFEConsoleClientInfo>? Disconnected;
    /// <summary>
    /// 接收到客户端消息触发
    /// </summary>
    public event XFEEventHandler<XFEConsoleClientInfo, string>? MessageReceived;
    /// <summary>
    /// 发生错误
    /// </summary>
    public event XFEEventHandler<XFEConsoleClientInfo, Exception>? ErrorOccurred;
    /// <summary>
    /// 服务器启动事件
    /// </summary>
    public event XFEEventHandler<XFEConsoleTerminalServer>? ServerStarted;
    /// <summary>
    /// XFE控制台终端服务器
    /// </summary>
    /// <param name="port">端口号</param>
    /// <param name="localOnly">是否只在本地开启</param>
    /// <param name="password">密码（默认为空）</param>
    public XFEConsoleTerminalServer(int port, bool localOnly = true, string password = "")
    {
        Password = password;
        Server = localOnly ? new CyberCommServer($"http://localhost:{port}/") : new CyberCommServer(port);
        ConfigureServer();
    }
    /// <summary>
    /// XFE控制台终端服务器
    /// </summary>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="password">密码（默认为空）</param>
    public XFEConsoleTerminalServer(string[] ipAddress, string password = "")
    {
        Password = password;
        Server = new CyberCommServer(ipAddress);
        ConfigureServer();
    }

    private void ConfigureServer()
    {
        Server.StartedHandler = cancellationToken =>
        {
            Server_ServerStarted();
            return ValueTask.CompletedTask;
        };
        Server.WebSocketConnectedHandler = async (eventArgs, cancellationToken) =>
            await Server_ClientConnected(eventArgs).ConfigureAwait(false);
        Server.WebSocketClosedHandler = (eventArgs, cancellationToken) =>
        {
            Server_ConnectionClosed(eventArgs);
            return ValueTask.CompletedTask;
        };
        Server.WebSocketMessageHandler = (eventArgs, cancellationToken) =>
        {
            Server_MessageReceived(eventArgs);
            return ValueTask.CompletedTask;
        };
    }

    private void Server_MessageReceived(CyberCommServerEventArgs e)
    {
        XFEConsoleClientInfo? clientInfo;
        lock (ClientInfoDictionary)
            ClientInfoDictionary.TryGetValue(e.CurrentWebSocket, out clientInfo);
        if (clientInfo is null)
            return;

        switch (e.MessageType)
        {
            case BackMessageType.Text:
                MessageReceived?.Invoke(clientInfo, e.TextMessage!);
                break;
            case BackMessageType.Binary:
                break;
            case BackMessageType.Error:
                ErrorOccurred?.Invoke(clientInfo, e.Exception!);
                break;
        }
    }

    private void Server_ConnectionClosed(CyberCommServerEventArgs e)
    {
        XFEConsoleClientInfo? clientInfo;
        lock (ClientInfoDictionary)
        {
            if (!ClientInfoDictionary.Remove(e.CurrentWebSocket, out clientInfo)) return;
        }
        Disconnected?.Invoke(this, clientInfo);
    }

    private async Task Server_ClientConnected(CyberCommServerEventArgs e)
    {
        try
        {
            if (e.WSHeader["ClientName"] is not null && e.WSHeader["ClientID"] is not null && e.WSHeader["Password"] is not null)
            {
                var password = e.WSHeader["Password"]!;
                var clientName = e.WSHeader["ClientName"]!;
                var clientUuid = e.WSHeader["ClientID"]!;
                if (password == Password)
                {
                    var clientInfo = new XFEConsoleClientInfo(clientName, clientUuid, password, e);
                    lock (ClientInfoDictionary)
                        ClientInfoDictionary.Add(e.CurrentWebSocket, clientInfo);
                    Connected?.Invoke(this, clientInfo);
                    return;
                }
            }
        }
        catch { }
        try
        {
            await e.Close();
        }
        catch
        {
            try
            {
                e.ForceClose();
            }
            catch { }
        }
    }

    private void Server_ServerStarted()
    {
        ClientInfoDictionary = [];
        ServerStarted?.Invoke(this);
    }
    /// <summary>
    /// 启动服务器
    /// </summary>
    /// <returns></returns>
    public async Task StartServer()
    {
        await Server.StartAsync().ConfigureAwait(false);
        await Server.RunAsync().ConfigureAwait(false);
    }
    /// <summary>
    /// 启动服务器并在监听就绪后返回。
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default) => Server.StartAsync(cancellationToken);
    /// <summary>
    /// 停止服务器并关闭当前连接。
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken = default) => Server.StopAsync(cancellationToken);
}
