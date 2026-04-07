using System.Runtime.InteropServices;

namespace XFEExtension.NetCore.XFEConsole.Utilities.Helpers;

/// <summary>
/// 控制台ANSI转义序列启用器
/// </summary>
public static partial class ConsoleAnsi
{
    const int STD_OUTPUT_HANDLE = -11;
    const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

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
    public static void Enable()
    {
        var handle = GetStdHandle(STD_OUTPUT_HANDLE);
        if (GetConsoleMode(handle, out uint mode))
        {
            SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        }
    }
}