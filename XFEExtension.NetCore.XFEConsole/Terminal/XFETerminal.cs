using XFEExtension.NetCore.XFEConsole.Utilities.Helpers;

namespace XFEExtension.NetCore.XFEConsole.Terminal;

/// <summary>
/// 新旧终端功能的统一入口。
/// </summary>
public static class XFETerminal
{
    private static readonly object SyncRoot = new();
    private static XFETerminalCapabilities? capabilities;

    /// <summary>
    /// 当前终端能力。第一次访问时会检测并在 Windows 上尝试启用 VT。
    /// </summary>
    public static XFETerminalCapabilities Capabilities
    {
        get
        {
            lock (SyncRoot)
                return capabilities ??= XFETerminalCapabilities.Detect();
        }
    }

    /// <summary>
    /// 重新检测当前终端能力。
    /// </summary>
    /// <param name="enableVirtualTerminal">是否尝试启用 VT。</param>
    /// <returns>新的能力快照。</returns>
    public static XFETerminalCapabilities RefreshCapabilities(bool enableVirtualTerminal = true)
    {
        lock (SyncRoot)
            return capabilities = XFETerminalCapabilities.Detect(enableVirtualTerminal);
    }

    /// <summary>
    /// 尝试启用 ANSI/VT 控制序列。
    /// </summary>
    /// <returns>可以使用 VT 时为 <see langword="true"/>。</returns>
    public static bool TryEnableVirtualTerminal()
    {
        var enabled = ConsoleAnsi.TryEnable();
        RefreshCapabilities(false);
        return enabled;
    }

    /// <summary>
    /// 向本地终端原始输出流写入文本，不经过 XFE 远程日志包装。
    /// </summary>
    /// <param name="value">文本或控制序列。</param>
    /// <param name="writer">自定义目标；为空时使用当前本地控制台。</param>
    /// <param name="flush">是否立即刷新。</param>
    public static void WriteRaw(string value, TextWriter? writer = null, bool flush = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        writer ??= GetLocalWriter();
        writer.Write(value);
        if (flush)
            writer.Flush();
    }

    /// <summary>
    /// 异步向本地终端原始输出流写入文本。
    /// </summary>
    /// <param name="value">文本或控制序列。</param>
    /// <param name="writer">自定义目标；为空时使用当前本地控制台。</param>
    /// <param name="flush">是否立即刷新。</param>
    /// <param name="cancellationToken">取消标记。</param>
    public static async ValueTask WriteRawAsync(
        string value,
        TextWriter? writer = null,
        bool flush = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        writer ??= GetLocalWriter();
        await writer.WriteAsync(value.AsMemory(), cancellationToken);
        if (flush)
            await writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// 设置控制台窗口或 Windows Terminal 标签页标题。
    /// </summary>
    /// <param name="title">标题，最长 254 个字符。</param>
    public static void SetTitle(string title)
    {
        var sequence = XFETerminalSequences.SetTitle(title);
        if (Capabilities.SupportsVirtualTerminal)
        {
            WriteRaw(sequence, flush: true);
            return;
        }

        if (!OperatingSystem.IsWindows() || Console.IsOutputRedirected)
            return;

        try
        {
            Console.Title = title;
        }
        catch (IOException)
        {
            // 无控制台窗口时安全降级。
        }
    }

    /// <summary>
    /// 设置 Windows Terminal 标签页圆环和 Windows 任务栏进度。
    /// 非 Windows Terminal 中安全地忽略。
    /// </summary>
    /// <param name="state">进度状态。</param>
    /// <param name="progress">0 到 100 的进度。</param>
    /// <returns>是否已将序列发送给 Windows Terminal。</returns>
    public static bool SetTaskbarProgress(XFETerminalProgressState state, int progress = 0)
    {
        var sequence = XFETerminalSequences.SetProgress(state, progress);
        if (!Capabilities.SupportsTaskbarProgress)
            return false;
        WriteRaw(sequence, flush: true);
        return true;
    }

    /// <summary>
    /// 清除 Windows Terminal 标签页及任务栏进度。
    /// </summary>
    /// <returns>是否已发送清除序列。</returns>
    public static bool ClearTaskbarProgress() => SetTaskbarProgress(XFETerminalProgressState.Hidden);

    /// <summary>
    /// 向 Windows Terminal 报告当前工作目录，以便复制标签页时沿用目录。
    /// </summary>
    /// <param name="path">工作目录。</param>
    /// <returns>是否已发送目录序列。</returns>
    public static bool SetWorkingDirectory(string path)
    {
        var sequence = XFETerminalSequences.SetWorkingDirectory(path);
        if (Capabilities.Kind != XFETerminalKind.WindowsTerminal)
            return false;
        WriteRaw(sequence, flush: true);
        return true;
    }

    /// <summary>
    /// 输出可点击超链接；不支持时只输出显示文本。
    /// </summary>
    /// <param name="text">显示文本。</param>
    /// <param name="uri">目标 URI。</param>
    /// <param name="writer">输出目标。</param>
    public static void WriteHyperlink(string text, Uri uri, TextWriter? writer = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(uri);
        WriteRaw(Capabilities.SupportsHyperlinks ? XFETerminalSequences.Hyperlink(text, uri) : text, writer);
    }

    /// <summary>
    /// 发出终端响铃。
    /// </summary>
    public static void Bell() => WriteRaw("\a", flush: true);

    /// <summary>
    /// 开始一个会在释放时自动恢复屏幕、光标与样式的交互会话。
    /// </summary>
    /// <param name="options">会话选项。</param>
    /// <param name="writer">输出目标。</param>
    /// <returns>交互会话。</returns>
    public static XFETerminalSession BeginSession(
        XFETerminalSessionOptions? options = null,
        TextWriter? writer = null) => new(options, writer);

    /// <summary>
    /// 创建行内进度条。
    /// </summary>
    /// <param name="options">显示选项。</param>
    /// <param name="writer">输出目标。</param>
    /// <returns>可报告进度的对象。</returns>
    public static XFETerminalProgressBar CreateProgressBar(
        XFETerminalProgressBarOptions? options = null,
        TextWriter? writer = null) => new(options, writer);

    internal static TextWriter GetLocalWriter() =>
        global::XFEExtension.NetCore.XFEConsole.XFEConsole.CurrentConsoleTextWriter?.OriginalTextWriter ?? Console.Out;
}
