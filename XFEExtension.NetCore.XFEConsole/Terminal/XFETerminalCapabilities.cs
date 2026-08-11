using System.Text;
using XFEExtension.NetCore.XFEConsole.Utilities.Helpers;

namespace XFEExtension.NetCore.XFEConsole.Terminal;

/// <summary>
/// 描述当前终端可以安全使用的功能。
/// </summary>
public sealed record XFETerminalCapabilities
{
    /// <summary>终端宿主类型。</summary>
    public required XFETerminalKind Kind { get; init; }

    /// <summary>输入、输出是否连接到交互控制台。</summary>
    public required bool IsInteractive { get; init; }

    /// <summary>是否可以使用 VT 输出序列。</summary>
    public required bool SupportsVirtualTerminal { get; init; }

    /// <summary>是否支持 256 色。</summary>
    public required bool Supports256Colors { get; init; }

    /// <summary>是否支持 24 位真彩色。</summary>
    public required bool SupportsTrueColor { get; init; }

    /// <summary>是否支持 VT 备用屏幕。</summary>
    public required bool SupportsAlternateScreen { get; init; }

    /// <summary>是否支持 OSC 8 可点击超链接。</summary>
    public required bool SupportsHyperlinks { get; init; }

    /// <summary>是否支持 Windows Terminal 的标签页/任务栏进度。</summary>
    public required bool SupportsTaskbarProgress { get; init; }

    /// <summary>是否可以读取低级鼠标输入。</summary>
    public required bool SupportsMouseInput { get; init; }

    /// <summary>是否适合输出 Unicode 绘图字符。</summary>
    public required bool SupportsUnicode { get; init; }

    /// <summary>
    /// 检测当前进程连接的终端。
    /// </summary>
    /// <param name="enableVirtualTerminal">在 Windows 上检测时是否尝试启用 VT 输出。</param>
    /// <returns>终端能力快照。</returns>
    public static XFETerminalCapabilities Detect(bool enableVirtualTerminal = true)
    {
        var outputRedirected = Console.IsOutputRedirected;
        var inputRedirected = Console.IsInputRedirected;
        var interactive = !outputRedirected && !inputRedirected;
        var isWindowsTerminal = OperatingSystem.IsWindows() &&
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WT_SESSION"));

        bool supportsVirtualTerminal;
        XFETerminalKind kind;

        if (outputRedirected)
        {
            kind = XFETerminalKind.Redirected;
            supportsVirtualTerminal = false;
        }
        else if (OperatingSystem.IsWindows())
        {
            kind = isWindowsTerminal ? XFETerminalKind.WindowsTerminal : XFETerminalKind.LegacyWindowsConsole;
            supportsVirtualTerminal = enableVirtualTerminal ? ConsoleAnsi.TryEnable() : ConsoleAnsi.IsEnabled();
        }
        else
        {
            var term = Environment.GetEnvironmentVariable("TERM");
            supportsVirtualTerminal = !string.Equals(term, "dumb", StringComparison.OrdinalIgnoreCase);
            kind = supportsVirtualTerminal ? XFETerminalKind.VirtualTerminal : XFETerminalKind.Unknown;
        }

        var colorTerm = Environment.GetEnvironmentVariable("COLORTERM");
        var hasTrueColorHint = string.Equals(colorTerm, "truecolor", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(colorTerm, "24bit", StringComparison.OrdinalIgnoreCase);
        var termName = Environment.GetEnvironmentVariable("TERM") ?? string.Empty;
        var modern = supportsVirtualTerminal && (isWindowsTerminal || !OperatingSystem.IsWindows());

        return new XFETerminalCapabilities
        {
            Kind = kind,
            IsInteractive = interactive,
            SupportsVirtualTerminal = supportsVirtualTerminal,
            Supports256Colors = modern && (isWindowsTerminal || termName.Contains("256color", StringComparison.OrdinalIgnoreCase) || hasTrueColorHint),
            SupportsTrueColor = modern && (isWindowsTerminal || hasTrueColorHint || termName.Contains("direct", StringComparison.OrdinalIgnoreCase)),
            SupportsAlternateScreen = supportsVirtualTerminal,
            SupportsHyperlinks = modern,
            SupportsTaskbarProgress = isWindowsTerminal && !outputRedirected,
            SupportsMouseInput = OperatingSystem.IsWindows() && interactive,
            SupportsUnicode = isWindowsTerminal || Encoding.UTF8.Equals(Console.OutputEncoding) || !OperatingSystem.IsWindows()
        };
    }
}
