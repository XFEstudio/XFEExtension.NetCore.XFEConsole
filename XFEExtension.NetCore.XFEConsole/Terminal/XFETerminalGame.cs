using System.Diagnostics;

namespace XFEExtension.NetCore.XFEConsole.Terminal;

/// <summary>
/// 终端小游戏运行选项。
/// </summary>
public sealed class XFETerminalGameOptions
{
    /// <summary>固定画布宽度；为空时使用窗口宽度。</summary>
    public int? Width { get; set; }

    /// <summary>固定画布高度；为空时使用窗口高度。</summary>
    public int? Height { get; set; }

    /// <summary>每秒目标帧数，默认为 30。</summary>
    public int FramesPerSecond { get; set; } = 30;

    /// <summary>是否使用备用屏幕。</summary>
    public bool UseAlternateScreen { get; set; } = true;

    /// <summary>是否捕获鼠标。</summary>
    public bool CaptureMouse { get; set; } = true;

    /// <summary>当画布尺寸未固定时，是否随窗口改变尺寸。</summary>
    public bool AutoResize { get; set; } = true;

    /// <summary>自动退出键；为空时由游戏代码决定。</summary>
    public ConsoleKey? ExitKey { get; set; } = ConsoleKey.Escape;
}

/// <summary>
/// 每一帧传给游戏代码的上下文。
/// </summary>
public sealed class XFETerminalGameContext
{
    private IReadOnlyList<XFETerminalInputEvent> inputEvents = [];

    internal XFETerminalGameContext(XFETerminalCanvas canvas)
    {
        Canvas = canvas;
    }

    /// <summary>当前字符画布。</summary>
    public XFETerminalCanvas Canvas { get; }

    /// <summary>当前帧收到的输入事件。</summary>
    public IReadOnlyList<XFETerminalInputEvent> InputEvents => inputEvents;

    /// <summary>上一帧到当前帧的时间。</summary>
    public TimeSpan DeltaTime { get; internal set; }

    /// <summary>游戏启动后的总时间。</summary>
    public TimeSpan Elapsed { get; internal set; }

    /// <summary>当前帧编号，从 0 开始。</summary>
    public long FrameNumber { get; internal set; }

    /// <summary>是否已请求退出。</summary>
    public bool IsExitRequested { get; private set; }

    /// <summary>
    /// 判断当前帧是否收到指定按键的按下事件。
    /// </summary>
    public bool IsKeyPressed(ConsoleKey key) => inputEvents
        .OfType<XFETerminalKeyEvent>()
        .Any(input => input.IsKeyDown && input.Key == key);

    /// <summary>请求在当前帧结束后退出游戏循环。</summary>
    public void RequestExit() => IsExitRequested = true;

    internal void SetInputEvents(IReadOnlyList<XFETerminalInputEvent> value) => inputEvents = value;
}

/// <summary>
/// 终端游戏每帧回调。
/// </summary>
/// <param name="context">游戏上下文。</param>
/// <param name="cancellationToken">取消标记。</param>
public delegate ValueTask XFETerminalGameFrame(
    XFETerminalGameContext context,
    CancellationToken cancellationToken);

/// <summary>
/// 固定帧率终端小游戏/TUI 循环。
/// </summary>
public static class XFETerminalGame
{
    /// <summary>
    /// 运行终端游戏循环，并在退出或异常时自动恢复终端状态。
    /// </summary>
    /// <param name="frame">每帧更新和绘制回调。</param>
    /// <param name="options">运行选项。</param>
    /// <param name="cancellationToken">取消标记。</param>
    public static async Task RunAsync(
        XFETerminalGameFrame frame,
        XFETerminalGameOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(frame);
        options ??= new XFETerminalGameOptions();
        if (options.FramesPerSecond is < 1 or > 240)
            throw new ArgumentOutOfRangeException(nameof(options.FramesPerSecond), "帧率必须在 1 到 240 之间。");
        if (!XFETerminal.Capabilities.IsInteractive)
            throw new InvalidOperationException("终端游戏需要未重定向的交互输入和输出。");

        var (windowWidth, windowHeight) = GetConsoleSize();
        var canvas = new XFETerminalCanvas(options.Width ?? windowWidth, options.Height ?? windowHeight);
        var context = new XFETerminalGameContext(canvas);
        using var session = XFETerminal.BeginSession(new XFETerminalSessionOptions
        {
            UseAlternateScreen = options.UseAlternateScreen,
            HideCursor = true,
            ClearScreen = true,
            DisableAutoWrap = true
        });
        using var input = new XFETerminalInputReader(new XFETerminalInputOptions
        {
            CaptureMouse = options.CaptureMouse,
            CaptureWindowResize = options.AutoResize,
            DisableLineInputAndEcho = true
        });

        var stopwatch = Stopwatch.StartNew();
        var previousFrameTime = TimeSpan.Zero;
        var targetFrameTime = TimeSpan.FromSeconds(1d / options.FramesPerSecond);
        while (!context.IsExitRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var frameStarted = stopwatch.Elapsed;
            var events = input.ReadAvailable();
            context.SetInputEvents(events);
            context.Elapsed = frameStarted;
            context.DeltaTime = frameStarted - previousFrameTime;
            previousFrameTime = frameStarted;

            if (options.AutoResize && (options.Width is null || options.Height is null))
            {
                var resize = events.OfType<XFETerminalResizeEvent>().LastOrDefault();
                if (resize is not null)
                    canvas.Resize(options.Width ?? Math.Max(1, resize.Width), options.Height ?? Math.Max(1, resize.Height));
            }

            if (options.ExitKey is { } exitKey && context.IsKeyPressed(exitKey))
                context.RequestExit();

            if (!context.IsExitRequested)
            {
                await frame(context, cancellationToken);
                canvas.Present(session.Writer);
                context.FrameNumber++;
            }

            var remaining = targetFrameTime - (stopwatch.Elapsed - frameStarted);
            if (remaining > TimeSpan.Zero)
                await Task.Delay(remaining, cancellationToken);
        }
    }

    private static (int Width, int Height) GetConsoleSize()
    {
        try
        {
            return (Math.Max(1, Console.WindowWidth), Math.Max(1, Console.WindowHeight));
        }
        catch (IOException)
        {
            return (80, 25);
        }
    }
}
