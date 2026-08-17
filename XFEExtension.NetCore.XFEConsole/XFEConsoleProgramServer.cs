namespace XFEExtension.NetCore.XFEConsole;

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using XFEExtension.NetCore.CyberComm;
using XFEExtension.NetCore.DelegateExtension;

/// <summary>
/// XFE控制台进程服务器
/// </summary>
public sealed class XFEConsoleProgramServer : IAsyncDisposable
{
    private readonly ConcurrentDictionary<WebSocket, XFEConsoleTerminalInfo> terminals = new();

    /// <summary>创建供远程终端连接的调试程序服务器。</summary>
    public XFEConsoleProgramServer(int port = 3280, bool localOnly = true, string password = "", string? programName = null, string? programId = null)
        : this(localOnly ? [$"http://localhost:{port}/"] : [$"http://*:{port}/"], password, programName, programId)
    {
    }

    /// <summary>使用指定监听地址创建调试程序服务器。</summary>
    public XFEConsoleProgramServer(string[] listenAddresses, string password = "", string? programName = null, string? programId = null)
    {
        ArgumentNullException.ThrowIfNull(listenAddresses);
        if (listenAddresses.Length == 0)
            throw new ArgumentException("至少需要一个监听地址。", nameof(listenAddresses));

        Password = password ?? string.Empty;
        ProgramName = string.IsNullOrWhiteSpace(programName) ? AppDomain.CurrentDomain.FriendlyName : programName;
        ProgramId = string.IsNullOrWhiteSpace(programId) ? Guid.NewGuid().ToString("N") : programId;
        Server = new CyberCommServer(listenAddresses)
        {
            StartedHandler = HandleStartedAsync,
            WebSocketConnectedHandler = HandleConnectedAsync,
            WebSocketClosedHandler = HandleClosedAsync,
            WebSocketMessageHandler = HandleMessageAsync,
            ErrorHandler = HandleServerError
        };
    }

    /// <summary>底层通信服务器。</summary>
    public CyberCommServer Server { get; }

    /// <summary>终端连接密码。</summary>
    public string Password { get; }

    /// <summary>调试程序名称。</summary>
    public string ProgramName { get; }

    /// <summary>调试程序唯一标识。</summary>
    public string ProgramId { get; }

    /// <summary>当前已通过鉴权的终端。</summary>
    public IReadOnlyCollection<XFEConsoleTerminalInfo> ConnectedTerminals => terminals.Values.ToArray();

    /// <summary>服务器启动时触发。</summary>
    public event XFEEventHandler<XFEConsoleProgramServer>? ServerStarted;

    /// <summary>终端通过鉴权并连接时触发。</summary>
    public event XFEEventHandler<XFEConsoleProgramServer, XFEConsoleTerminalInfo>? Connected;

    /// <summary>已鉴权终端断开时触发。</summary>
    public event XFEEventHandler<XFEConsoleProgramServer, XFEConsoleTerminalInfo>? Disconnected;

    /// <summary>通信发生错误时触发。</summary>
    public event XFEEventHandler<XFEConsoleProgramServer, Exception>? ErrorOccurred;

    /// <summary>开始监听；监听就绪后返回。</summary>
    public Task StartAsync(CancellationToken cancellationToken = default) => Server.StartAsync(cancellationToken);

    /// <summary>等待服务器停止。</summary>
    public Task RunAsync(CancellationToken cancellationToken = default) => Server.RunAsync(cancellationToken);

    /// <summary>停止监听并断开所有终端。</summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await Server.StopAsync(cancellationToken).ConfigureAwait(false);
        terminals.Clear();
    }

    /// <summary>向所有已鉴权终端发送控制台输出。</summary>
    public async Task OutputMessage(string message, bool isLine, CancellationToken cancellationToken = default)
    {
        var payload = XFEConsoleProtocol.CreateOutputMessage(message, isLine);
        var snapshot = terminals.ToArray();
        if (snapshot.Length == 0)
            return;

        await Task.WhenAll(snapshot.Select(pair => SendOutputAsync(pair.Key, pair.Value, payload, cancellationToken))).ConfigureAwait(false);
    }

    private ValueTask HandleStartedAsync(CancellationToken cancellationToken)
    {
        terminals.Clear();
        ServerStarted?.Invoke(this);
        return ValueTask.CompletedTask;
    }

    private async ValueTask HandleConnectedAsync(CyberCommServerEventArgs eventArgs, CancellationToken cancellationToken)
    {
        var terminalName = eventArgs.WSHeader[XFEConsoleProtocol.TerminalNameHeader];
        var terminalId = eventArgs.WSHeader[XFEConsoleProtocol.TerminalIdHeader];
        var suppliedPassword = eventArgs.WSHeader[XFEConsoleProtocol.PasswordHeader];
        var accepted = !string.IsNullOrWhiteSpace(terminalName)
            && !string.IsNullOrWhiteSpace(terminalId)
            && suppliedPassword is not null
            && PasswordMatches(Password, suppliedPassword);

        if (!accepted)
        {
            var reason = suppliedPassword is null ? "连接请求缺少密码。" : "密码错误。";
            await RejectAndCloseAsync(eventArgs, reason).ConfigureAwait(false);
            return;
        }

        var terminal = new XFEConsoleTerminalInfo(terminalName!, terminalId!, eventArgs);
        if (!terminals.TryAdd(eventArgs.CurrentWebSocket, terminal))
        {
            await RejectAndCloseAsync(eventArgs, "终端连接已存在。").ConfigureAwait(false);
            return;
        }

        try
        {
            await eventArgs.ReplyMessage(XFEConsoleProtocol.CreateAuthenticationMessage(true, string.Empty, ProgramName, ProgramId)).ConfigureAwait(false);
            Connected?.Invoke(this, terminal);
        }
        catch (Exception exception)
        {
            terminals.TryRemove(eventArgs.CurrentWebSocket, out _);
            ErrorOccurred?.Invoke(this, exception);
            await CloseIgnoringErrorsAsync(eventArgs).ConfigureAwait(false);
        }
    }

    private ValueTask HandleClosedAsync(CyberCommServerEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (terminals.TryRemove(eventArgs.CurrentWebSocket, out var terminal))
            Disconnected?.Invoke(this, terminal);
        return ValueTask.CompletedTask;
    }

    private ValueTask HandleMessageAsync(CyberCommServerEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (eventArgs.MessageType == BackMessageType.Error && eventArgs.Exception is not null)
            ErrorOccurred?.Invoke(this, eventArgs.Exception);
        return ValueTask.CompletedTask;
    }

    private void HandleServerError(Exception exception) => ErrorOccurred?.Invoke(this, exception);

    private async Task SendOutputAsync(WebSocket webSocket, XFEConsoleTerminalInfo terminal, string payload, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await terminal.EventArgs.ReplyMessage(payload).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            terminals.TryRemove(webSocket, out _);
            ErrorOccurred?.Invoke(this, exception);
        }
    }

    private async Task RejectAndCloseAsync(CyberCommServerEventArgs eventArgs, string reason)
    {
        try
        {
            await eventArgs.ReplyMessage(XFEConsoleProtocol.CreateAuthenticationMessage(false, reason, ProgramName, ProgramId)).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            ErrorOccurred?.Invoke(this, exception);
        }
        await CloseIgnoringErrorsAsync(eventArgs).ConfigureAwait(false);
    }

    private static async Task CloseIgnoringErrorsAsync(CyberCommServerEventArgs eventArgs)
    {
        try
        {
            await eventArgs.Close().ConfigureAwait(false);
        }
        catch
        {
            try
            {
                eventArgs.ForceClose();
            }
            catch
            {
            }
        }
    }

    private static bool PasswordMatches(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        await Server.DisposeAsync().ConfigureAwait(false);
    }
}
