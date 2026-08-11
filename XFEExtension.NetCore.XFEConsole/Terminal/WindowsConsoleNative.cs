using System.Runtime.InteropServices;

namespace XFEExtension.NetCore.XFEConsole.Terminal;

internal static class WindowsConsoleNative
{
    internal const int StdInputHandle = -10;
    internal const uint EnableProcessedInput = 0x0001;
    internal const uint EnableLineInput = 0x0002;
    internal const uint EnableEchoInput = 0x0004;
    internal const uint EnableWindowInput = 0x0008;
    internal const uint EnableMouseInput = 0x0010;
    internal const uint EnableQuickEditMode = 0x0040;
    internal const uint EnableExtendedFlags = 0x0080;

    internal const ushort KeyEvent = 0x0001;
    internal const ushort MouseEvent = 0x0002;
    internal const ushort WindowBufferSizeEvent = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GetStdHandle(int standardHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetConsoleMode(IntPtr consoleHandle, out uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetConsoleMode(IntPtr consoleHandle, uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetNumberOfConsoleInputEvents(IntPtr inputHandle, out uint count);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "ReadConsoleInputW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ReadConsoleInput(
        IntPtr inputHandle,
        [Out] InputRecord[] buffer,
        uint length,
        out uint eventsRead);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Coord
    {
        internal short X;
        internal short Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct KeyEventRecord
    {
        internal int KeyDown;
        internal ushort RepeatCount;
        internal ushort VirtualKeyCode;
        internal ushort VirtualScanCode;
        internal char UnicodeChar;
        internal uint ControlKeyState;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MouseEventRecord
    {
        internal Coord MousePosition;
        internal uint ButtonState;
        internal uint ControlKeyState;
        internal uint EventFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowBufferSizeRecord
    {
        internal Coord Size;
    }

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
    internal struct InputRecord
    {
        [FieldOffset(0)] internal ushort EventType;
        [FieldOffset(4)] internal KeyEventRecord KeyEvent;
        [FieldOffset(4)] internal MouseEventRecord MouseEvent;
        [FieldOffset(4)] internal WindowBufferSizeRecord WindowBufferSizeEvent;
    }
}
