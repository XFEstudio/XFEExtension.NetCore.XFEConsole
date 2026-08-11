namespace XFEExtension.NetCore.XFEConsole.Terminal;

/// <summary>
/// 全屏或交互终端会话选项。
/// </summary>
public sealed class XFETerminalSessionOptions
{
    /// <summary>是否使用不破坏原屏幕内容的备用屏幕。默认为 true。</summary>
    public bool UseAlternateScreen { get; set; } = true;

    /// <summary>会话期间是否隐藏光标。默认为 true。</summary>
    public bool HideCursor { get; set; } = true;

    /// <summary>进入会话后是否清屏。默认为 true。</summary>
    public bool ClearScreen { get; set; } = true;

    /// <summary>会话期间是否禁用自动换行。默认为 true。</summary>
    public bool DisableAutoWrap { get; set; } = true;
}

/// <summary>
/// 自动管理备用屏幕、光标和样式恢复的终端会话。
/// </summary>
public sealed class XFETerminalSession : IDisposable, IAsyncDisposable
{
    private readonly TextWriter writer;
    private readonly XFETerminalSessionOptions options;
    private readonly XFETerminalCapabilities capabilities;
    private readonly bool? originalCursorVisible;
    private bool disposed;

    /// <summary>
    /// 创建并立即进入终端会话。
    /// </summary>
    /// <param name="options">会话选项。</param>
    /// <param name="writer">输出目标。</param>
    public XFETerminalSession(XFETerminalSessionOptions? options = null, TextWriter? writer = null)
    {
        this.options = options ?? new XFETerminalSessionOptions();
        this.writer = writer ?? XFETerminal.GetLocalWriter();
        capabilities = XFETerminal.Capabilities;

        if (capabilities.SupportsVirtualTerminal)
        {
            var start = string.Concat(
                this.options.UseAlternateScreen ? XFETerminalSequences.EnterAlternateScreen : string.Empty,
                this.options.DisableAutoWrap ? XFETerminalSequences.DisableAutoWrap : string.Empty,
                this.options.HideCursor ? XFETerminalSequences.HideCursor : string.Empty,
                this.options.ClearScreen ? XFETerminalSequences.EraseDisplay(XFETerminalEraseMode.All) + XFETerminalSequences.CursorPosition(1, 1) : string.Empty);
            XFETerminal.WriteRaw(start, this.writer, true);
        }
        else if (capabilities.IsInteractive)
        {
            originalCursorVisible = TryGetCursorVisible();
            if (this.options.HideCursor)
                TrySetCursorVisible(false);
            if (this.options.ClearScreen)
                TryClear();
        }
    }

    /// <summary>当前会话检测到的终端能力。</summary>
    public XFETerminalCapabilities Capabilities => capabilities;

    /// <summary>会话使用的输出流。</summary>
    public TextWriter Writer => writer;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;

        if (capabilities.SupportsVirtualTerminal)
        {
            var end = string.Concat(
                XFETerminalSequences.ResetStyle,
                options.DisableAutoWrap ? XFETerminalSequences.EnableAutoWrap : string.Empty,
                options.HideCursor ? XFETerminalSequences.ShowCursor : string.Empty,
                options.UseAlternateScreen ? XFETerminalSequences.LeaveAlternateScreen : string.Empty);
            XFETerminal.WriteRaw(end, writer, true);
        }
        else if (originalCursorVisible is { } visible)
        {
            TrySetCursorVisible(visible);
        }

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private static bool? TryGetCursorVisible()
    {
        if (!OperatingSystem.IsWindows())
            return null;
        try { return Console.CursorVisible; }
        catch (IOException) { return null; }
        catch (PlatformNotSupportedException) { return null; }
    }

    private static void TrySetCursorVisible(bool visible)
    {
        if (!OperatingSystem.IsWindows())
            return;
        try { Console.CursorVisible = visible; }
        catch (IOException) { }
        catch (PlatformNotSupportedException) { }
    }

    private static void TryClear()
    {
        try { Console.Clear(); }
        catch (IOException) { }
    }
}
