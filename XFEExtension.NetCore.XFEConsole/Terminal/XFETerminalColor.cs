namespace XFEExtension.NetCore.XFEConsole.Terminal;

/// <summary>
/// 终端颜色的编码方式。
/// </summary>
public enum XFETerminalColorKind
{
    /// <summary>终端默认颜色。</summary>
    Default,
    /// <summary>ANSI 16 色。</summary>
    Ansi16,
    /// <summary>ANSI 256 色索引。</summary>
    Indexed256,
    /// <summary>24 位 RGB 真彩色。</summary>
    Rgb
}

/// <summary>
/// 可表示默认色、16 色、256 色或 24 位真彩色的终端颜色。
/// </summary>
public readonly record struct XFETerminalColor
{
    private static readonly (ConsoleColor Color, byte R, byte G, byte B)[] ConsolePalette =
    [
        (ConsoleColor.Black, 0x0c, 0x0c, 0x0c),
        (ConsoleColor.DarkBlue, 0x00, 0x37, 0xda),
        (ConsoleColor.DarkGreen, 0x13, 0xa1, 0x0e),
        (ConsoleColor.DarkCyan, 0x3a, 0x96, 0xdd),
        (ConsoleColor.DarkRed, 0xc5, 0x0f, 0x1f),
        (ConsoleColor.DarkMagenta, 0x88, 0x17, 0x98),
        (ConsoleColor.DarkYellow, 0xc1, 0x9c, 0x00),
        (ConsoleColor.Gray, 0xcc, 0xcc, 0xcc),
        (ConsoleColor.DarkGray, 0x76, 0x76, 0x76),
        (ConsoleColor.Blue, 0x3b, 0x78, 0xff),
        (ConsoleColor.Green, 0x16, 0xc6, 0x0c),
        (ConsoleColor.Cyan, 0x61, 0xd6, 0xd6),
        (ConsoleColor.Red, 0xe7, 0x48, 0x56),
        (ConsoleColor.Magenta, 0xb4, 0x00, 0x9e),
        (ConsoleColor.Yellow, 0xf9, 0xf1, 0xa5),
        (ConsoleColor.White, 0xf2, 0xf2, 0xf2)
    ];

    private XFETerminalColor(XFETerminalColorKind kind, byte value1, byte value2, byte value3)
    {
        Kind = kind;
        Value1 = value1;
        Value2 = value2;
        Value3 = value3;
    }

    /// <summary>颜色编码方式。</summary>
    public XFETerminalColorKind Kind { get; }

    /// <summary>索引值或红色分量。</summary>
    public byte Value1 { get; }

    /// <summary>绿色分量。</summary>
    public byte Value2 { get; }

    /// <summary>蓝色分量。</summary>
    public byte Value3 { get; }

    /// <summary>终端默认颜色。</summary>
    public static XFETerminalColor Default => new(XFETerminalColorKind.Default, 0, 0, 0);

    /// <summary>黑色。</summary>
    public static XFETerminalColor Black => FromConsoleColor(ConsoleColor.Black);

    /// <summary>白色。</summary>
    public static XFETerminalColor White => FromConsoleColor(ConsoleColor.White);

    /// <summary>红色。</summary>
    public static XFETerminalColor Red => FromConsoleColor(ConsoleColor.Red);

    /// <summary>绿色。</summary>
    public static XFETerminalColor Green => FromConsoleColor(ConsoleColor.Green);

    /// <summary>蓝色。</summary>
    public static XFETerminalColor Blue => FromConsoleColor(ConsoleColor.Blue);

    /// <summary>青色。</summary>
    public static XFETerminalColor Cyan => FromConsoleColor(ConsoleColor.Cyan);

    /// <summary>品红色。</summary>
    public static XFETerminalColor Magenta => FromConsoleColor(ConsoleColor.Magenta);

    /// <summary>黄色。</summary>
    public static XFETerminalColor Yellow => FromConsoleColor(ConsoleColor.Yellow);

    /// <summary>
    /// 创建 24 位 RGB 颜色。
    /// </summary>
    /// <param name="red">红色分量。</param>
    /// <param name="green">绿色分量。</param>
    /// <param name="blue">蓝色分量。</param>
    /// <returns>终端颜色。</returns>
    public static XFETerminalColor FromRgb(byte red, byte green, byte blue) =>
        new(XFETerminalColorKind.Rgb, red, green, blue);

    /// <summary>
    /// 创建 ANSI 256 色。
    /// </summary>
    /// <param name="index">0 到 255 的颜色索引。</param>
    /// <returns>终端颜色。</returns>
    public static XFETerminalColor FromIndex(byte index) =>
        new(XFETerminalColorKind.Indexed256, index, 0, 0);

    /// <summary>
    /// 从 <see cref="ConsoleColor"/> 创建 ANSI 16 色。
    /// </summary>
    /// <param name="color">传统控制台颜色。</param>
    /// <returns>终端颜色。</returns>
    public static XFETerminalColor FromConsoleColor(ConsoleColor color) =>
        Enum.IsDefined(color)
            ? new(XFETerminalColorKind.Ansi16, checked((byte)color), 0, 0)
            : throw new ArgumentOutOfRangeException(nameof(color));

    /// <summary>
    /// 取得最接近当前颜色的传统控制台颜色。
    /// </summary>
    /// <returns>近似的 <see cref="ConsoleColor"/>。</returns>
    public ConsoleColor ToConsoleColor()
    {
        if (Kind == XFETerminalColorKind.Ansi16)
            return (ConsoleColor)Value1;

        if (Kind == XFETerminalColorKind.Default)
            return ConsoleColor.Gray;

        var (r, g, b) = ToRgb();
        var best = ConsoleColor.Gray;
        var bestDistance = double.MaxValue;
        foreach (var candidate in ConsolePalette)
        {
            var red = r - candidate.R;
            var green = g - candidate.G;
            var blue = b - candidate.B;
            var distance = red * red + green * green + blue * blue;
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = candidate.Color;
        }

        return best;
    }

    internal string ToSgrParameters(bool foreground)
    {
        var colorBase = foreground ? 30 : 40;
        return Kind switch
        {
            XFETerminalColorKind.Default => (foreground ? 39 : 49).ToString(),
            XFETerminalColorKind.Ansi16 => ToAnsi16Parameter((ConsoleColor)Value1, colorBase),
            XFETerminalColorKind.Indexed256 => $"{(foreground ? 38 : 48)};5;{Value1}",
            XFETerminalColorKind.Rgb => $"{(foreground ? 38 : 48)};2;{Value1};{Value2};{Value3}",
            _ => (foreground ? 39 : 49).ToString()
        };
    }

    private static string ToAnsi16Parameter(ConsoleColor color, int colorBase)
    {
        var value = (int)color;
        var bright = value >= 8;
        var ansiIndex = color switch
        {
            ConsoleColor.Black or ConsoleColor.DarkGray => 0,
            ConsoleColor.DarkRed or ConsoleColor.Red => 1,
            ConsoleColor.DarkGreen or ConsoleColor.Green => 2,
            ConsoleColor.DarkYellow or ConsoleColor.Yellow => 3,
            ConsoleColor.DarkBlue or ConsoleColor.Blue => 4,
            ConsoleColor.DarkMagenta or ConsoleColor.Magenta => 5,
            ConsoleColor.DarkCyan or ConsoleColor.Cyan => 6,
            _ => 7
        };
        return (colorBase + ansiIndex + (bright ? 60 : 0)).ToString();
    }

    private (byte R, byte G, byte B) ToRgb()
    {
        if (Kind == XFETerminalColorKind.Rgb)
            return (Value1, Value2, Value3);

        if (Kind == XFETerminalColorKind.Ansi16)
        {
            var consoleColor = (ConsoleColor)Value1;
            var entry = ConsolePalette.FirstOrDefault(item => item.Color == consoleColor);
            return (entry.R, entry.G, entry.B);
        }

        if (Kind == XFETerminalColorKind.Indexed256)
        {
            if (Value1 < 16)
            {
                var entry = ConsolePalette[Value1];
                return (entry.R, entry.G, entry.B);
            }

            if (Value1 >= 232)
            {
                var gray = (byte)(8 + (Value1 - 232) * 10);
                return (gray, gray, gray);
            }

            var index = Value1 - 16;
            var red = index / 36;
            var green = index % 36 / 6;
            var blue = index % 6;
            return (Cube(red), Cube(green), Cube(blue));
        }

        return (0xcc, 0xcc, 0xcc);
    }

    private static byte Cube(int value) => (byte)(value == 0 ? 0 : 55 + value * 40);
}
