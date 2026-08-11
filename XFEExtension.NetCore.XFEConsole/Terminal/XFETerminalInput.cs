namespace XFEExtension.NetCore.XFEConsole.Terminal;

/// <summary>
/// 键盘修饰键。
/// </summary>
[Flags]
public enum XFEKeyModifiers
{
    /// <summary>无修饰键。</summary>
    None = 0,
    /// <summary>Shift。</summary>
    Shift = 1,
    /// <summary>Alt。</summary>
    Alt = 2,
    /// <summary>Ctrl。</summary>
    Control = 4
}

/// <summary>
/// 鼠标按键状态。
/// </summary>
[Flags]
public enum XFEMouseButtons
{
    /// <summary>未按下。</summary>
    None = 0,
    /// <summary>左键。</summary>
    Left = 1,
    /// <summary>右键。</summary>
    Right = 2,
    /// <summary>第二扩展键。</summary>
    Second = 4,
    /// <summary>第三扩展键。</summary>
    Third = 8,
    /// <summary>第四扩展键。</summary>
    Fourth = 16
}

/// <summary>
/// 鼠标动作类型。
/// </summary>
public enum XFEMouseAction
{
    /// <summary>按键按下。</summary>
    ButtonPressed,
    /// <summary>按键释放。</summary>
    ButtonReleased,
    /// <summary>鼠标移动。</summary>
    Moved,
    /// <summary>双击。</summary>
    DoubleClick,
    /// <summary>垂直滚轮。</summary>
    Wheel,
    /// <summary>水平滚轮。</summary>
    HorizontalWheel
}

/// <summary>
/// 终端输入事件基类。
/// </summary>
public abstract record XFETerminalInputEvent;

/// <summary>
/// 键盘按下或释放事件。
/// </summary>
/// <param name="Key">控制台按键。</param>
/// <param name="Character">输入字符；无对应字符时为 \0。</param>
/// <param name="IsKeyDown">true 表示按下，false 表示释放。</param>
/// <param name="RepeatCount">自动重复次数。</param>
/// <param name="Modifiers">修饰键。</param>
public sealed record XFETerminalKeyEvent(
    ConsoleKey Key,
    char Character,
    bool IsKeyDown,
    int RepeatCount,
    XFEKeyModifiers Modifiers) : XFETerminalInputEvent;

/// <summary>
/// 鼠标输入事件。坐标相对于当前可见窗口且从 0 开始。
/// </summary>
/// <param name="X">列坐标。</param>
/// <param name="Y">行坐标。</param>
/// <param name="Buttons">当前按键状态。</param>
/// <param name="Action">动作类型。</param>
/// <param name="Modifiers">修饰键。</param>
/// <param name="WheelDelta">滚轮增量，非滚轮事件为 0。</param>
public sealed record XFETerminalMouseEvent(
    int X,
    int Y,
    XFEMouseButtons Buttons,
    XFEMouseAction Action,
    XFEKeyModifiers Modifiers,
    int WheelDelta = 0) : XFETerminalInputEvent;

/// <summary>
/// 终端可见区域尺寸变化事件。
/// </summary>
/// <param name="Width">新宽度。</param>
/// <param name="Height">新高度。</param>
public sealed record XFETerminalResizeEvent(int Width, int Height) : XFETerminalInputEvent;

/// <summary>
/// 低级终端输入选项。
/// </summary>
public sealed class XFETerminalInputOptions
{
    /// <summary>是否接收鼠标事件。默认为 true。</summary>
    public bool CaptureMouse { get; set; } = true;

    /// <summary>是否接收窗口大小变化事件。默认为 true。</summary>
    public bool CaptureWindowResize { get; set; } = true;

    /// <summary>是否将 Ctrl+C 当作普通输入，而非取消信号。</summary>
    public bool CaptureControlC { get; set; }

    /// <summary>是否关闭行输入与键盘回显。交互应用通常应保持为 true。</summary>
    public bool DisableLineInputAndEcho { get; set; } = true;
}

/// <summary>
/// 可读取键盘、鼠标和窗口大小变化的低级终端输入读取器。
/// Windows 使用 ReadConsoleInput；其他平台安全降级到 Console.ReadKey。
/// </summary>
public sealed class XFETerminalInputReader : IDisposable
{
    private const uint MouseMoved = 0x0001;
    private const uint DoubleClick = 0x0002;
    private const uint MouseWheeled = 0x0004;
    private const uint MouseHorizontalWheeled = 0x0008;
    private const uint LeftAltPressed = 0x0002;
    private const uint RightAltPressed = 0x0001;
    private const uint LeftControlPressed = 0x0008;
    private const uint RightControlPressed = 0x0004;
    private const uint ShiftPressed = 0x0010;

    private readonly XFETerminalInputOptions options;
    private readonly IntPtr inputHandle;
    private readonly uint originalMode;
    private readonly bool nativeMode;
    private int lastWidth;
    private int lastHeight;
    private uint previousMouseButtons;
    private bool disposed;

    /// <summary>
    /// 创建并配置输入读取器。
    /// </summary>
    /// <param name="options">输入选项。</param>
    public XFETerminalInputReader(XFETerminalInputOptions? options = null)
    {
        this.options = options ?? new XFETerminalInputOptions();
        (lastWidth, lastHeight) = GetConsoleSize();

        if (!OperatingSystem.IsWindows() || Console.IsInputRedirected)
            return;

        inputHandle = WindowsConsoleNative.GetStdHandle(WindowsConsoleNative.StdInputHandle);
        if (inputHandle == IntPtr.Zero || inputHandle == new IntPtr(-1) ||
            !WindowsConsoleNative.GetConsoleMode(inputHandle, out originalMode))
            return;

        var mode = originalMode | WindowsConsoleNative.EnableExtendedFlags;
        mode &= ~WindowsConsoleNative.EnableQuickEditMode;
        mode = this.options.CaptureMouse
            ? mode | WindowsConsoleNative.EnableMouseInput
            : mode & ~WindowsConsoleNative.EnableMouseInput;
        mode = this.options.CaptureWindowResize
            ? mode | WindowsConsoleNative.EnableWindowInput
            : mode & ~WindowsConsoleNative.EnableWindowInput;

        if (this.options.DisableLineInputAndEcho)
            mode &= ~(WindowsConsoleNative.EnableLineInput | WindowsConsoleNative.EnableEchoInput);
        if (this.options.CaptureControlC)
            mode &= ~WindowsConsoleNative.EnableProcessedInput;

        nativeMode = WindowsConsoleNative.SetConsoleMode(inputHandle, mode);
    }

    /// <summary>是否正在使用 Windows 低级输入事件模式。</summary>
    public bool IsNativeEventMode => nativeMode;

    /// <summary>
    /// 尝试读取一个输入事件，不会阻塞。
    /// </summary>
    /// <param name="inputEvent">读取到的事件。</param>
    /// <returns>读取到事件时为 true。</returns>
    public bool TryRead(out XFETerminalInputEvent? inputEvent)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        inputEvent = null;

        if (nativeMode)
            return TryReadNative(out inputEvent);

        if (options.CaptureWindowResize)
        {
            var (width, height) = GetConsoleSize();
            if (width != lastWidth || height != lastHeight)
            {
                lastWidth = width;
                lastHeight = height;
                inputEvent = new XFETerminalResizeEvent(width, height);
                return true;
            }
        }

        try
        {
            if (!Console.KeyAvailable)
                return false;
            var key = Console.ReadKey(true);
            inputEvent = new XFETerminalKeyEvent(
                key.Key,
                key.KeyChar,
                true,
                1,
                ConvertModifiers(key.Modifiers));
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// 读取当前已经排队的输入事件。
    /// </summary>
    /// <param name="maximumCount">最多读取数量。</param>
    /// <returns>输入事件只读列表。</returns>
    public IReadOnlyList<XFETerminalInputEvent> ReadAvailable(int maximumCount = 256)
    {
        if (maximumCount < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumCount));

        var events = new List<XFETerminalInputEvent>(Math.Min(maximumCount, 32));
        while (events.Count < maximumCount && TryRead(out var inputEvent))
        {
            if (inputEvent is not null)
                events.Add(inputEvent);
        }
        return events;
    }

    /// <summary>
    /// 异步等待并读取下一个输入事件。
    /// </summary>
    /// <param name="cancellationToken">取消标记。</param>
    /// <returns>下一个输入事件。</returns>
    public async ValueTask<XFETerminalInputEvent> ReadAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TryRead(out var inputEvent) && inputEvent is not null)
                return inputEvent;
            await Task.Delay(8, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        if (nativeMode)
            WindowsConsoleNative.SetConsoleMode(inputHandle, originalMode);
        GC.SuppressFinalize(this);
    }

    private bool TryReadNative(out XFETerminalInputEvent? inputEvent)
    {
        inputEvent = null;
        while (WindowsConsoleNative.GetNumberOfConsoleInputEvents(inputHandle, out var count) && count > 0)
        {
            var buffer = new WindowsConsoleNative.InputRecord[1];
            if (!WindowsConsoleNative.ReadConsoleInput(inputHandle, buffer, 1, out var read) || read == 0)
                return false;

            var record = buffer[0];
            switch (record.EventType)
            {
                case WindowsConsoleNative.KeyEvent:
                    inputEvent = new XFETerminalKeyEvent(
                        (ConsoleKey)record.KeyEvent.VirtualKeyCode,
                        record.KeyEvent.UnicodeChar,
                        record.KeyEvent.KeyDown != 0,
                        record.KeyEvent.RepeatCount,
                        ConvertModifiers(record.KeyEvent.ControlKeyState));
                    return true;

                case WindowsConsoleNative.MouseEvent when options.CaptureMouse:
                    var mouse = record.MouseEvent;
                    var currentButtons = mouse.ButtonState & 0xffff;
                    var action = mouse.EventFlags switch
                    {
                        MouseMoved => XFEMouseAction.Moved,
                        DoubleClick => XFEMouseAction.DoubleClick,
                        MouseWheeled => XFEMouseAction.Wheel,
                        MouseHorizontalWheeled => XFEMouseAction.HorizontalWheel,
                        _ when (currentButtons & ~previousMouseButtons) != 0 => XFEMouseAction.ButtonPressed,
                        _ => XFEMouseAction.ButtonReleased
                    };
                    var wheelDelta = action is XFEMouseAction.Wheel or XFEMouseAction.HorizontalWheel
                        ? (short)(mouse.ButtonState >> 16)
                        : 0;
                    previousMouseButtons = currentButtons;
                    inputEvent = new XFETerminalMouseEvent(
                        Math.Max(0, mouse.MousePosition.X - SafeWindowLeft()),
                        Math.Max(0, mouse.MousePosition.Y - SafeWindowTop()),
                        ConvertButtons(currentButtons),
                        action,
                        ConvertModifiers(mouse.ControlKeyState),
                        wheelDelta);
                    return true;

                case WindowsConsoleNative.WindowBufferSizeEvent when options.CaptureWindowResize:
                    (lastWidth, lastHeight) = GetConsoleSize();
                    inputEvent = new XFETerminalResizeEvent(lastWidth, lastHeight);
                    return true;
            }
        }
        return false;
    }

    private static XFEKeyModifiers ConvertModifiers(uint state)
    {
        var modifiers = XFEKeyModifiers.None;
        if ((state & ShiftPressed) != 0) modifiers |= XFEKeyModifiers.Shift;
        if ((state & (LeftAltPressed | RightAltPressed)) != 0) modifiers |= XFEKeyModifiers.Alt;
        if ((state & (LeftControlPressed | RightControlPressed)) != 0) modifiers |= XFEKeyModifiers.Control;
        return modifiers;
    }

    private static XFEKeyModifiers ConvertModifiers(ConsoleModifiers modifiers)
    {
        var result = XFEKeyModifiers.None;
        if (modifiers.HasFlag(ConsoleModifiers.Shift)) result |= XFEKeyModifiers.Shift;
        if (modifiers.HasFlag(ConsoleModifiers.Alt)) result |= XFEKeyModifiers.Alt;
        if (modifiers.HasFlag(ConsoleModifiers.Control)) result |= XFEKeyModifiers.Control;
        return result;
    }

    private static XFEMouseButtons ConvertButtons(uint state)
    {
        var buttons = XFEMouseButtons.None;
        if ((state & 0x0001) != 0) buttons |= XFEMouseButtons.Left;
        if ((state & 0x0002) != 0) buttons |= XFEMouseButtons.Right;
        if ((state & 0x0004) != 0) buttons |= XFEMouseButtons.Second;
        if ((state & 0x0008) != 0) buttons |= XFEMouseButtons.Third;
        if ((state & 0x0010) != 0) buttons |= XFEMouseButtons.Fourth;
        return buttons;
    }

    private static (int Width, int Height) GetConsoleSize()
    {
        try { return (Math.Max(1, Console.WindowWidth), Math.Max(1, Console.WindowHeight)); }
        catch (IOException) { return (80, 25); }
        catch (PlatformNotSupportedException) { return (80, 25); }
    }

    private static int SafeWindowLeft()
    {
        try { return Console.WindowLeft; }
        catch (IOException) { return 0; }
    }

    private static int SafeWindowTop()
    {
        try { return Console.WindowTop; }
        catch (IOException) { return 0; }
    }
}
