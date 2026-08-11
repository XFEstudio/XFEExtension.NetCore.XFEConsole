using System.Text;

namespace XFEExtension.NetCore.XFEConsole.Terminal;

/// <summary>
/// 构造 Windows Console 与 Windows Terminal 支持的 VT/OSC 控制序列。
/// 所有坐标均从 1 开始。
/// </summary>
public static class XFETerminalSequences
{
    /// <summary>ESC 控制字符。</summary>
    public const string Escape = "\x1b";

    /// <summary>CSI 前缀。</summary>
    public const string Csi = "\x1b[";

    /// <summary>OSC 前缀。</summary>
    public const string Osc = "\x1b]";

    /// <summary>推荐的 OSC 字符串终止符。</summary>
    public const string StringTerminator = "\x1b\\";

    /// <summary>恢复默认文本样式。</summary>
    public const string ResetStyle = "\x1b[0m";

    /// <summary>保存光标位置及状态。</summary>
    public const string SaveCursor = "\x1b7";

    /// <summary>恢复光标位置及状态。</summary>
    public const string RestoreCursor = "\x1b8";

    /// <summary>隐藏光标。</summary>
    public const string HideCursor = "\x1b[?25l";

    /// <summary>显示光标。</summary>
    public const string ShowCursor = "\x1b[?25h";

    /// <summary>进入备用屏幕缓冲区。</summary>
    public const string EnterAlternateScreen = "\x1b[?1049h";

    /// <summary>返回主屏幕缓冲区。</summary>
    public const string LeaveAlternateScreen = "\x1b[?1049l";

    /// <summary>启用自动换行。</summary>
    public const string EnableAutoWrap = "\x1b[?7h";

    /// <summary>禁用自动换行。</summary>
    public const string DisableAutoWrap = "\x1b[?7l";

    /// <summary>启用括号粘贴模式。</summary>
    public const string EnableBracketedPaste = "\x1b[?2004h";

    /// <summary>禁用括号粘贴模式。</summary>
    public const string DisableBracketedPaste = "\x1b[?2004l";

    /// <summary>启用焦点事件报告。</summary>
    public const string EnableFocusReporting = "\x1b[?1004h";

    /// <summary>禁用焦点事件报告。</summary>
    public const string DisableFocusReporting = "\x1b[?1004l";

    /// <summary>请求终端报告光标位置。</summary>
    public const string QueryCursorPosition = "\x1b[6n";

    /// <summary>请求终端报告主要设备属性。</summary>
    public const string QueryDeviceAttributes = "\x1b[0c";

    /// <summary>执行 VT 软重置。</summary>
    public const string SoftReset = "\x1b[!p";

    /// <summary>
    /// 光标上移。
    /// </summary>
    public static string CursorUp(int count = 1) => $"{Csi}{Positive(count, nameof(count))}A";

    /// <summary>光标下移。</summary>
    public static string CursorDown(int count = 1) => $"{Csi}{Positive(count, nameof(count))}B";

    /// <summary>光标右移。</summary>
    public static string CursorForward(int count = 1) => $"{Csi}{Positive(count, nameof(count))}C";

    /// <summary>光标左移。</summary>
    public static string CursorBackward(int count = 1) => $"{Csi}{Positive(count, nameof(count))}D";

    /// <summary>光标移动到后续行开头。</summary>
    public static string CursorNextLine(int count = 1) => $"{Csi}{Positive(count, nameof(count))}E";

    /// <summary>光标移动到之前行开头。</summary>
    public static string CursorPreviousLine(int count = 1) => $"{Csi}{Positive(count, nameof(count))}F";

    /// <summary>设置光标绝对列。</summary>
    public static string CursorColumn(int column) => $"{Csi}{Positive(column, nameof(column))}G";

    /// <summary>设置光标绝对行。</summary>
    public static string CursorRow(int row) => $"{Csi}{Positive(row, nameof(row))}d";

    /// <summary>设置光标绝对位置。</summary>
    public static string CursorPosition(int row, int column) =>
        $"{Csi}{Positive(row, nameof(row))};{Positive(column, nameof(column))}H";

    /// <summary>设置光标形状。</summary>
    public static string CursorShape(XFETerminalCursorShape shape) => Enum.IsDefined(shape)
        ? $"{Csi}{(int)shape} q"
        : throw new ArgumentOutOfRangeException(nameof(shape));

    /// <summary>向上滚动内容。</summary>
    public static string ScrollUp(int count = 1) => $"{Csi}{Positive(count, nameof(count))}S";

    /// <summary>向下滚动内容。</summary>
    public static string ScrollDown(int count = 1) => $"{Csi}{Positive(count, nameof(count))}T";

    /// <summary>插入空白字符。</summary>
    public static string InsertCharacters(int count = 1) => $"{Csi}{Positive(count, nameof(count))}@";

    /// <summary>删除字符。</summary>
    public static string DeleteCharacters(int count = 1) => $"{Csi}{Positive(count, nameof(count))}P";

    /// <summary>擦除字符。</summary>
    public static string EraseCharacters(int count = 1) => $"{Csi}{Positive(count, nameof(count))}X";

    /// <summary>插入行。</summary>
    public static string InsertLines(int count = 1) => $"{Csi}{Positive(count, nameof(count))}L";

    /// <summary>删除行。</summary>
    public static string DeleteLines(int count = 1) => $"{Csi}{Positive(count, nameof(count))}M";

    /// <summary>擦除显示区域。</summary>
    public static string EraseDisplay(XFETerminalEraseMode mode = XFETerminalEraseMode.ToEnd) =>
        $"{Csi}{ValidateEraseMode(mode, true)}J";

    /// <summary>擦除当前行。</summary>
    public static string EraseLine(XFETerminalEraseMode mode = XFETerminalEraseMode.ToEnd) =>
        $"{Csi}{ValidateEraseMode(mode, false)}K";

    /// <summary>设置上下滚动边界。</summary>
    public static string ScrollingRegion(int top, int bottom)
    {
        top = Positive(top, nameof(top));
        bottom = Positive(bottom, nameof(bottom));
        if (bottom < top)
            throw new ArgumentException("底部边界不能小于顶部边界。", nameof(bottom));
        return $"{Csi}{top};{bottom}r";
    }

    /// <summary>恢复默认滚动边界。</summary>
    public static string ResetScrollingRegion() => $"{Csi}r";

    /// <summary>为文本应用指定样式。</summary>
    public static string Styled(string text, XFETerminalStyle style, bool reset = true) => style.Apply(text, reset);

    /// <summary>设置窗口或标签页标题。</summary>
    public static string SetTitle(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        if (title.Length > 254)
            throw new ArgumentOutOfRangeException(nameof(title), "终端标题不能超过 254 个字符。");
        return $"{Osc}2;{SanitizeOsc(title)}{StringTerminator}";
    }

    /// <summary>生成 OSC 8 可点击超链接。</summary>
    public static string Hyperlink(string text, Uri uri, string? id = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(uri);
        var parameters = string.IsNullOrEmpty(id) ? string.Empty : $"id={SanitizeOsc(id)}";
        return $"{Osc}8;{parameters};{SanitizeOsc(uri.ToString())}{StringTerminator}{text}{Osc}8;;{StringTerminator}";
    }

    /// <summary>设置 Windows Terminal 标签页及任务栏进度。</summary>
    public static string SetProgress(XFETerminalProgressState state, int progress = 0)
    {
        if (!Enum.IsDefined(state))
            throw new ArgumentOutOfRangeException(nameof(state));
        if (progress is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(progress), "进度必须在 0 到 100 之间。");
        return $"{Osc}9;4;{(int)state};{progress}\x07";
    }

    /// <summary>向 Windows Terminal 报告当前工作目录。</summary>
    public static string SetWorkingDirectory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return $"{Osc}9;9;{SanitizeOsc(Path.GetFullPath(path))}{StringTerminator}";
    }

    /// <summary>生成 Windows Terminal shell integration 命令标记。</summary>
    public static string ShellMark(XFETerminalShellMark mark, int? exitCode = null) => mark switch
    {
        XFETerminalShellMark.Prompt => $"{Osc}133;A{StringTerminator}",
        XFETerminalShellMark.CommandStart => $"{Osc}133;B{StringTerminator}",
        XFETerminalShellMark.CommandExecuted => $"{Osc}133;C{StringTerminator}",
        XFETerminalShellMark.CommandFinished => $"{Osc}133;D{(exitCode is null ? string.Empty : $";{exitCode.Value}")}{StringTerminator}",
        _ => throw new ArgumentOutOfRangeException(nameof(mark))
    };

    /// <summary>修改 ANSI 调色板中的一个颜色。</summary>
    public static string SetPaletteColor(byte index, byte red, byte green, byte blue) =>
        $"{Osc}4;{index};rgb:{red:x2}/{green:x2}/{blue:x2}{StringTerminator}";

    /// <summary>
    /// 生成 OSC 52 写入剪贴板序列。终端可能禁用或询问此功能；只应传入可信文本。
    /// </summary>
    public static string CopyToClipboard(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        return $"{Osc}52;c;{base64}{StringTerminator}";
    }

    /// <summary>启用或关闭 SGR 鼠标跟踪。</summary>
    public static string MouseTracking(XFETerminalMouseTrackingMode mode)
    {
        const string disable = "\x1b[?1000l\x1b[?1002l\x1b[?1003l\x1b[?1006l";
        return mode switch
        {
            XFETerminalMouseTrackingMode.Disabled => disable,
            XFETerminalMouseTrackingMode.Click => $"{disable}\x1b[?1000h\x1b[?1006h",
            XFETerminalMouseTrackingMode.ButtonEvent => $"{disable}\x1b[?1002h\x1b[?1006h",
            XFETerminalMouseTrackingMode.AnyEvent => $"{disable}\x1b[?1003h\x1b[?1006h",
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    private static int Positive(int value, string parameterName)
    {
        if (value is < 1 or > short.MaxValue)
            throw new ArgumentOutOfRangeException(parameterName, "值必须在 1 到 32767 之间。");
        return value;
    }

    private static int ValidateEraseMode(XFETerminalEraseMode mode, bool allowScrollback)
    {
        if (!Enum.IsDefined(mode) || (!allowScrollback && mode == XFETerminalEraseMode.AllWithScrollback))
            throw new ArgumentOutOfRangeException(nameof(mode));
        return (int)mode;
    }

    private static string SanitizeOsc(string value) => value
        .Replace("\x1b", string.Empty, StringComparison.Ordinal)
        .Replace("\x07", string.Empty, StringComparison.Ordinal)
        .Replace("\x9c", string.Empty, StringComparison.Ordinal);
}
