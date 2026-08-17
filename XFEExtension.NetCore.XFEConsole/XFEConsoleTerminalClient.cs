namespace XFEExtension.NetCore.XFEConsole;

using XFEExtension.NetCore.CyberComm;
using XFEExtension.NetCore.DelegateExtension;
using XFEExtension.NetCore.FormatExtension;

/// <summary>
/// XFE控制台终端客户端
/// </summary>
public sealed class XFEConsoleTerminalClient : IAsyncDisposable
{
    private TaskCompletionSource<bool>? authenticationSignal;
    private int authenticated;

    /// <summary>创建主动连接调试程序服务器的终端客户端。</summary>
    public XFEConsoleTerminalClient(string serverAddress, string password = "", string? terminalName = null, string? terminalId = null)
    {
        if (!Uri.TryCreate(serverAddress, UriKind.Absolute, out var serverUri) || serverUri.Scheme is not ("ws" or "wss"))
            throw new ArgumentException("调试服务器地址必须是有效的 ws 或 wss 地址。", nameof(serverAddress));

        ServerAddress = serverUri.ToString();
        Password = password ?? string.Empty;
        TerminalName = string.IsNullOrWhiteSpace(terminalName) ? Environment.MachineName : terminalName;
        TerminalId = string.IsNullOrWhiteSpace(terminalId) ? Guid.NewGuid().ToString("N") : terminalId;
        Client = new CyberCommClient(new CyberCommClientOptions
        {
            ServerUri = serverUri,
            Reconnect = new CyberCommReconnectOptions { Enabled = false, MaxAttempts = 0 },
            RequestHeaders = new Dictionary<string, string>
            {
                [XFEConsoleProtocol.TerminalNameHeader] = TerminalName,
                [XFEConsoleProtocol.TerminalIdHeader] = TerminalId,
                [XFEConsoleProtocol.PasswordHeader] = Password
            }
        })
        {
            MessageHandler = HandleMessageAsync,
            ConnectionClosedHandler = HandleConnectionClosedAsync,
            ErrorHandler = HandleError
        };
    }

    /// <summary>底层通信客户端。</summary>
    public CyberCommClient Client { get; }

    /// <summary>调试程序服务器地址。</summary>
    public string ServerAddress { get; }

    /// <summary>连接密码。</summary>
    public string Password { get; }

    /// <summary>终端名称。</summary>
    public string TerminalName { get; }

    /// <summary>终端唯一标识。</summary>
    public string TerminalId { get; }

    /// <summary>服务器返回的调试程序名称。</summary>
    public string RemoteProgramName { get; private set; } = string.Empty;

    /// <summary>服务器返回的调试程序唯一标识。</summary>
    public string RemoteProgramId { get; private set; } = string.Empty;

    /// <summary>最近一次鉴权失败原因。</summary>
    public string? AuthenticationFailureReason { get; private set; }

    /// <summary>是否已完成密码鉴权。</summary>
    public bool IsAuthenticated => Volatile.Read(ref authenticated) == 1;

    /// <summary>鉴权成功并建立调试连接时触发。</summary>
    public event XFEEventHandler<XFEConsoleTerminalClient>? Connected;

    /// <summary>已鉴权的调试连接断开时触发。</summary>
    public event XFEEventHandler<XFEConsoleTerminalClient>? Disconnected;

    /// <summary>接收到调试程序的控制台输出时触发。</summary>
    public event XFEEventHandler<XFEConsoleTerminalClient, string>? MessageReceived;

    /// <summary>底层通信发生错误时触发。</summary>
    public event XFEEventHandler<XFEConsoleTerminalClient, Exception>? ErrorOccurred;

    /// <summary>连接服务器并等待密码鉴权结果。</summary>
    /// <returns>密码正确时返回 true；密码被拒绝时返回 false。</returns>
    public async Task<bool> ConnectAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        if (IsAuthenticated && Client.IsConnected)
            return true;

        AuthenticationFailureReason = null;
        RemoteProgramName = string.Empty;
        RemoteProgramId = string.Empty;
        Interlocked.Exchange(ref authenticated, 0);
        authenticationSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout ?? TimeSpan.FromSeconds(10));
        try
        {
            await Client.ConnectAsync(timeoutSource.Token).ConfigureAwait(false);
            var accepted = await authenticationSignal.Task.WaitAsync(timeoutSource.Token).ConfigureAwait(false);
            if (!accepted)
            {
                await DisconnectIgnoringErrorsAsync().ConfigureAwait(false);
                return false;
            }

            Interlocked.Exchange(ref authenticated, 1);
            Connected?.Invoke(this);
            return true;
        }
        catch
        {
            await DisconnectIgnoringErrorsAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            authenticationSignal = null;
        }
    }

    /// <summary>主动断开调试连接。</summary>
    public Task DisconnectAsync(CancellationToken cancellationToken = default) => Client.DisconnectAsync(cancellationToken);

    private ValueTask HandleMessageAsync(CyberCommClientEventArgs eventArgs, CancellationToken cancellationToken)
    {
        if (eventArgs.MessageType == BackMessageType.Error)
        {
            if (eventArgs.Exception is not null)
                HandleError(eventArgs.Exception);
            return ValueTask.CompletedTask;
        }

        if (eventArgs.MessageType != BackMessageType.Text || eventArgs.TextMessage is null)
            return ValueTask.CompletedTask;

        if (!IsAuthenticated && TryHandleAuthentication(eventArgs.TextMessage))
            return ValueTask.CompletedTask;

        if (IsAuthenticated)
            MessageReceived?.Invoke(this, eventArgs.TextMessage);
        return ValueTask.CompletedTask;
    }

    private bool TryHandleAuthentication(string message)
    {
        try
        {
            var dictionary = new XFEDictionary(message);
            if (dictionary[XFEConsoleProtocol.MessageTypeKey] != XFEConsoleProtocol.AuthenticationMessage)
                return false;

            RemoteProgramName = dictionary[XFEConsoleProtocol.ProgramNameKey] ?? string.Empty;
            RemoteProgramId = dictionary[XFEConsoleProtocol.ProgramIdKey] ?? string.Empty;
            var accepted = dictionary[XFEConsoleProtocol.AuthenticatedKey] == "true";
            AuthenticationFailureReason = accepted ? null : dictionary[XFEConsoleProtocol.ReasonKey] ?? "密码错误";
            authenticationSignal?.TrySetResult(accepted);
            return true;
        }
        catch (Exception exception)
        {
            authenticationSignal?.TrySetException(new InvalidDataException("调试服务器返回了无效的鉴权消息。", exception));
            return true;
        }
    }

    private ValueTask HandleConnectionClosedAsync(CancellationToken cancellationToken)
    {
        var wasAuthenticated = Interlocked.Exchange(ref authenticated, 0) == 1;
        authenticationSignal?.TrySetException(new IOException("调试服务器在完成鉴权前关闭了连接。"));
        if (wasAuthenticated)
            Disconnected?.Invoke(this);
        return ValueTask.CompletedTask;
    }

    private void HandleError(Exception exception)
    {
        authenticationSignal?.TrySetException(exception);
        ErrorOccurred?.Invoke(this, exception);
    }

    private async Task DisconnectIgnoringErrorsAsync()
    {
        try
        {
            await Client.DisconnectAsync().ConfigureAwait(false);
        }
        catch
        {
        }
        finally
        {
            Interlocked.Exchange(ref authenticated, 0);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisconnectIgnoringErrorsAsync().ConfigureAwait(false);
        await Client.DisposeAsync().ConfigureAwait(false);
    }
}
