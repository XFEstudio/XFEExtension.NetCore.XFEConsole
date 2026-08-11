using System.Runtime.InteropServices;

namespace XFEExtension.NetCore.XFEConsole.Utilities.Helpers;

/// <summary>
/// 控制台ANSI转义序列启用器
/// </summary>
public static partial class ConsoleAnsi
{
    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    /// <summary>
    /// 启用控制台ANSI转义序列支持，使得控制台能够正确解析和显示ANSI转义序列（如颜色、光标控制等）。
    /// </summary>
    public static void Enable() => TryEnable();

    /// <summary>
    /// 尝试启用 ANSI/VT 输出，并返回当前终端是否可以使用 VT 序列。
    /// </summary>
    /// <returns>启用成功，或当前非 Windows 终端原生支持 VT 时为 <see langword="true"/>。</returns>
    public static bool TryEnable()
    {
        if (Console.IsOutputRedirected)
            return false;

        if (!OperatingSystem.IsWindows())
            return !string.Equals(Environment.GetEnvironmentVariable("TERM"), "dumb", StringComparison.OrdinalIgnoreCase);

        var handle = GetStdHandle(StdOutputHandle);
        if (handle == IntPtr.Zero || handle == new IntPtr(-1) || !GetConsoleMode(handle, out var mode))
            return false;

        if ((mode & EnableVirtualTerminalProcessing) != 0)
            return true;

        return SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
    }

    /// <summary>
    /// 判断 Windows 控制台输出句柄是否已经启用 ANSI/VT 处理。
    /// </summary>
    /// <returns>已启用时为 <see langword="true"/>。</returns>
    public static bool IsEnabled()
    {
        if (Console.IsOutputRedirected)
            return false;

        if (!OperatingSystem.IsWindows())
            return !string.Equals(Environment.GetEnvironmentVariable("TERM"), "dumb", StringComparison.OrdinalIgnoreCase);

        var handle = GetStdHandle(StdOutputHandle);
        if (handle != IntPtr.Zero && handle != new IntPtr(-1) && GetConsoleMode(handle, out var mode))
        {
            return (mode & EnableVirtualTerminalProcessing) != 0;
        }

        return false;
    }
}
