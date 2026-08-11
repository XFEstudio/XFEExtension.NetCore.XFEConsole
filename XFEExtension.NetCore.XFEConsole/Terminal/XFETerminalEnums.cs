namespace XFEExtension.NetCore.XFEConsole.Terminal;

/// <summary>
/// 当前控制台宿主类型。
/// </summary>
public enum XFETerminalKind
{
    /// <summary>无法可靠识别的宿主。</summary>
    Unknown,
    /// <summary>标准输出已重定向，不应发送交互控制序列。</summary>
    Redirected,
    /// <summary>Windows 传统控制台宿主。</summary>
    LegacyWindowsConsole,
    /// <summary>Windows Terminal。</summary>
    WindowsTerminal,
    /// <summary>Windows 之外支持 VT 的终端。</summary>
    VirtualTerminal
}

/// <summary>
/// 擦除屏幕或行时采用的范围。
/// </summary>
public enum XFETerminalEraseMode
{
    /// <summary>从光标（含）擦除到末尾。</summary>
    ToEnd = 0,
    /// <summary>从开头擦除到光标（含）。</summary>
    ToBeginning = 1,
    /// <summary>擦除全部。</summary>
    All = 2,
    /// <summary>擦除全部及回滚缓冲区；仅用于擦除显示。</summary>
    AllWithScrollback = 3
}

/// <summary>
/// VT 光标形状。
/// </summary>
public enum XFETerminalCursorShape
{
    /// <summary>使用用户配置的默认形状。</summary>
    Default = 0,
    /// <summary>闪烁方块。</summary>
    BlinkingBlock = 1,
    /// <summary>常亮方块。</summary>
    SteadyBlock = 2,
    /// <summary>闪烁下划线。</summary>
    BlinkingUnderline = 3,
    /// <summary>常亮下划线。</summary>
    SteadyUnderline = 4,
    /// <summary>闪烁竖线。</summary>
    BlinkingBar = 5,
    /// <summary>常亮竖线。</summary>
    SteadyBar = 6
}

/// <summary>
/// Windows Terminal 标签页及任务栏进度状态。
/// </summary>
public enum XFETerminalProgressState
{
    /// <summary>隐藏进度。</summary>
    Hidden = 0,
    /// <summary>普通进度。</summary>
    Normal = 1,
    /// <summary>错误进度。</summary>
    Error = 2,
    /// <summary>无法确定百分比的进度。</summary>
    Indeterminate = 3,
    /// <summary>警告进度。</summary>
    Warning = 4
}

/// <summary>
/// Windows Terminal 命令标记类型。
/// </summary>
public enum XFETerminalShellMark
{
    /// <summary>提示符开始。</summary>
    Prompt,
    /// <summary>命令行开始。</summary>
    CommandStart,
    /// <summary>命令已执行、输出开始。</summary>
    CommandExecuted,
    /// <summary>命令结束。</summary>
    CommandFinished
}

/// <summary>
/// VT 鼠标跟踪详细程度。
/// </summary>
public enum XFETerminalMouseTrackingMode
{
    /// <summary>关闭鼠标跟踪。</summary>
    Disabled,
    /// <summary>只报告按下和释放。</summary>
    Click,
    /// <summary>按键按下时同时报告移动。</summary>
    ButtonEvent,
    /// <summary>报告所有移动。</summary>
    AnyEvent
}

/// <summary>
/// 艺术字和其他输出的终端兼容模式。
/// </summary>
public enum XFETerminalCompatibility
{
    /// <summary>自动检测。</summary>
    Auto,
    /// <summary>使用 ANSI/VT、Unicode 与真彩色。</summary>
    Modern,
    /// <summary>使用传统控制台安全的字符和颜色。</summary>
    Legacy
}
