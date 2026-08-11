using System.Globalization;
using System.Text;

namespace XFEExtension.NetCore.XFEConsole.Terminal;

/// <summary>
/// 内置终端标题艺术字样式。
/// </summary>
public enum XFETerminalArtStyle
{
    /// <summary>双倍宽度实心块。</summary>
    Block,
    /// <summary>紧凑单元格。</summary>
    Compact,
    /// <summary>圆点字。</summary>
    Dots,
    /// <summary>轮廓字。</summary>
    Outline,
    /// <summary>带右下阴影。</summary>
    Shadow,
    /// <summary>倾斜字。</summary>
    Slant,
    /// <summary>适用于任意 Unicode 文本的边框标题。</summary>
    Framed
}

/// <summary>
/// 内置艺术字配色。
/// </summary>
public enum XFETerminalArtPalette
{
    /// <summary>不设置颜色。</summary>
    Default,
    /// <summary>青色。</summary>
    Cyan,
    /// <summary>彩虹渐变。</summary>
    Rainbow,
    /// <summary>海洋蓝渐变。</summary>
    Ocean,
    /// <summary>日落渐变。</summary>
    Sunset,
    /// <summary>森林绿渐变。</summary>
    Forest,
    /// <summary>火焰渐变。</summary>
    Fire,
    /// <summary>霓虹紫渐变。</summary>
    Neon
}

/// <summary>
/// 终端标题艺术字生成选项。
/// </summary>
public sealed class XFETerminalTitleArtOptions
{
    /// <summary>艺术字样式。</summary>
    public XFETerminalArtStyle Style { get; set; } = XFETerminalArtStyle.Block;

    /// <summary>内置配色。</summary>
    public XFETerminalArtPalette Palette { get; set; } = XFETerminalArtPalette.Cyan;

    /// <summary>兼容模式。</summary>
    public XFETerminalCompatibility Compatibility { get; set; } = XFETerminalCompatibility.Auto;

    /// <summary>是否输出颜色。传统模式的返回字符串始终不含控制符，直接显示时使用 ConsoleColor。</summary>
    public bool UseColor { get; set; } = true;

    /// <summary>覆盖内置配色的单一颜色；为空时使用 Palette。</summary>
    public XFETerminalColor? Color { get; set; }

    /// <summary>字母间空白列数，默认为 1。</summary>
    public int LetterSpacing { get; set; } = 1;

    /// <summary>直接显示后是否换行。</summary>
    public bool WriteLine { get; set; } = true;
}

/// <summary>
/// 无第三方字体依赖的终端标题艺术字生成器。
/// 点阵样式内置 A-Z、0-9 和常用符号；其他 Unicode 文本会自动使用 Framed 样式以保留原字符。
/// </summary>
public static class XFETerminalTitleArt
{
    private const int GlyphWidth = 5;
    private const int GlyphHeight = 5;

    private static readonly IReadOnlyDictionary<char, string> Glyphs = new Dictionary<char, string>
    {
        ['A'] = "0111010001111111000110001",
        ['B'] = "1111010001111101000111110",
        ['C'] = "0111110000100001000001111",
        ['D'] = "1111010001100011000111110",
        ['E'] = "1111110000111101000011111",
        ['F'] = "1111110000111101000010000",
        ['G'] = "0111110000101111000101111",
        ['H'] = "1000110001111111000110001",
        ['I'] = "1111100100001000010011111",
        ['J'] = "0011100010000101001011100",
        ['K'] = "1000110010111001001010001",
        ['L'] = "1000010000100001000011111",
        ['M'] = "1000111011101011000110001",
        ['N'] = "1000111001101011001110001",
        ['O'] = "0111010001100011000101110",
        ['P'] = "1111010001111101000010000",
        ['Q'] = "0111010001101011001001101",
        ['R'] = "1111010001111101001010001",
        ['S'] = "0111110000011100000111110",
        ['T'] = "1111100100001000010000100",
        ['U'] = "1000110001100011000101110",
        ['V'] = "1000110001100010101000100",
        ['W'] = "1000110001101011101110001",
        ['X'] = "1000101010001000101010001",
        ['Y'] = "1000101010001000010000100",
        ['Z'] = "1111100010001000100011111",
        ['0'] = "0111010011101011100101110",
        ['1'] = "0010001100001000010001110",
        ['2'] = "0111010001000100010011111",
        ['3'] = "1111000001001100000111110",
        ['4'] = "1000110001111110000100001",
        ['5'] = "1111110000111100000111110",
        ['6'] = "0111110000111101000101110",
        ['7'] = "1111100010001000100001000",
        ['8'] = "0111010001011101000101110",
        ['9'] = "0111010001011110000101110",
        [' '] = "0000000000000000000000000",
        ['-'] = "0000000000111110000000000",
        ['_'] = "0000000000000000000011111",
        ['.'] = "0000000000000000011000110",
        [','] = "0000000000000000011000100",
        [':'] = "0000000110000000011000000",
        ['!'] = "0010000100001000000000100",
        ['?'] = "0111010001001100000000100",
        ['+'] = "0000000100011100010000000",
        ['='] = "0000011111000001111100000",
        ['/'] = "0000100010001000100010000",
        ['\\'] = "1000001000001000001000001"
    };

    private static readonly IReadOnlyDictionary<XFETerminalArtPalette, XFETerminalColor[]> Palettes =
        new Dictionary<XFETerminalArtPalette, XFETerminalColor[]>
        {
            [XFETerminalArtPalette.Cyan] = [XFETerminalColor.FromRgb(0x20, 0xd9, 0xff)],
            [XFETerminalArtPalette.Rainbow] =
            [
                XFETerminalColor.FromRgb(0xff, 0x4d, 0x6d),
                XFETerminalColor.FromRgb(0xff, 0xa9, 0x4d),
                XFETerminalColor.FromRgb(0xff, 0xe6, 0x6d),
                XFETerminalColor.FromRgb(0x54, 0xe3, 0x8e),
                XFETerminalColor.FromRgb(0x4d, 0xa6, 0xff),
                XFETerminalColor.FromRgb(0xb8, 0x69, 0xff)
            ],
            [XFETerminalArtPalette.Ocean] =
            [
                XFETerminalColor.FromRgb(0x00, 0xf5, 0xd4),
                XFETerminalColor.FromRgb(0x00, 0xbb, 0xf9),
                XFETerminalColor.FromRgb(0x43, 0x67, 0xff),
                XFETerminalColor.FromRgb(0x72, 0x56, 0xff)
            ],
            [XFETerminalArtPalette.Sunset] =
            [
                XFETerminalColor.FromRgb(0xff, 0xd1, 0x66),
                XFETerminalColor.FromRgb(0xff, 0x8c, 0x42),
                XFETerminalColor.FromRgb(0xf4, 0x43, 0x69),
                XFETerminalColor.FromRgb(0x8f, 0x3f, 0x71)
            ],
            [XFETerminalArtPalette.Forest] =
            [
                XFETerminalColor.FromRgb(0xb7, 0xef, 0xc5),
                XFETerminalColor.FromRgb(0x57, 0xcc, 0x99),
                XFETerminalColor.FromRgb(0x38, 0xa3, 0xa5),
                XFETerminalColor.FromRgb(0x22, 0x57, 0x7a)
            ],
            [XFETerminalArtPalette.Fire] =
            [
                XFETerminalColor.FromRgb(0xff, 0xf3, 0x75),
                XFETerminalColor.FromRgb(0xff, 0xb7, 0x03),
                XFETerminalColor.FromRgb(0xfb, 0x85, 0x00),
                XFETerminalColor.FromRgb(0xd0, 0x00, 0x00)
            ],
            [XFETerminalArtPalette.Neon] =
            [
                XFETerminalColor.FromRgb(0x00, 0xff, 0xea),
                XFETerminalColor.FromRgb(0x7b, 0x2c, 0xff),
                XFETerminalColor.FromRgb(0xff, 0x2b, 0xd6),
                XFETerminalColor.FromRgb(0x00, 0xff, 0xea)
            ]
        };

    /// <summary>
    /// 生成适合当前终端直接输出的艺术字。现代模式可能包含 ANSI 颜色序列。
    /// </summary>
    /// <param name="text">标题文本。</param>
    /// <param name="options">生成选项。</param>
    /// <returns>终端就绪字符串。</returns>
    public static string Generate(string text, XFETerminalTitleArtOptions? options = null)
    {
        ValidateText(text);
        options ??= new XFETerminalTitleArtOptions();
        ValidateOptions(options);
        var modern = IsModern(options.Compatibility);
        var rows = RenderRows(text, options, modern);
        return HasColor(options) && modern
            ? ApplyAnsiColors(rows, options)
            : string.Join(Environment.NewLine, rows);
    }

    /// <summary>
    /// 生成不包含任何 ANSI 控制符的纯艺术字字符串。
    /// </summary>
    /// <param name="text">标题文本。</param>
    /// <param name="style">艺术字样式。</param>
    /// <param name="compatibility">现代或传统字符集。</param>
    /// <param name="letterSpacing">字母间距。</param>
    /// <returns>纯文本艺术字。</returns>
    public static string GeneratePlain(
        string text,
        XFETerminalArtStyle style = XFETerminalArtStyle.Block,
        XFETerminalCompatibility compatibility = XFETerminalCompatibility.Auto,
        int letterSpacing = 1)
    {
        ValidateText(text);
        var options = new XFETerminalTitleArtOptions
        {
            Style = style,
            Compatibility = compatibility,
            UseColor = false,
            LetterSpacing = letterSpacing
        };
        ValidateOptions(options);
        return string.Join(Environment.NewLine, RenderRows(text, options, IsModern(compatibility)));
    }

    /// <summary>
    /// 直接向终端显示艺术字。传统模式会用 ConsoleColor 逐行着色。
    /// </summary>
    /// <param name="text">标题文本。</param>
    /// <param name="options">显示选项。</param>
    /// <param name="writer">输出目标。</param>
    public static void Write(string text, XFETerminalTitleArtOptions? options = null, TextWriter? writer = null)
    {
        ValidateText(text);
        options ??= new XFETerminalTitleArtOptions();
        ValidateOptions(options);
        var useConsoleColor = writer is null;
        writer ??= XFETerminal.GetLocalWriter();
        var modern = IsModern(options.Compatibility);
        var rows = RenderRows(text, options, modern);
        var hasColor = HasColor(options);

        if (modern || !hasColor)
        {
            writer.Write(modern && hasColor ? ApplyAnsiColors(rows, options) : string.Join(Environment.NewLine, rows));
            if (options.WriteLine)
                writer.WriteLine();
            writer.Flush();
            return;
        }

        ConsoleColor? original = null;
        try { if (useConsoleColor) original = Console.ForegroundColor; }
        catch (IOException) { }
        for (var i = 0; i < rows.Count; i++)
        {
            try { if (useConsoleColor) Console.ForegroundColor = ColorForRow(i, rows.Count, options).ToConsoleColor(); }
            catch (IOException) { }
            writer.Write(rows[i]);
            if (i < rows.Count - 1 || options.WriteLine)
                writer.WriteLine();
        }
        if (original is { } color)
        {
            try { Console.ForegroundColor = color; }
            catch (IOException) { }
        }
        writer.Flush();
    }

    private static IReadOnlyList<string> RenderRows(
        string text,
        XFETerminalTitleArtOptions options,
        bool modern)
    {
        var normalized = text.ToUpperInvariant();
        if (options.Style == XFETerminalArtStyle.Framed || normalized.Any(character => !Glyphs.ContainsKey(character)))
            return RenderFramed(text, modern);

        var scaleX = options.Style is XFETerminalArtStyle.Block or XFETerminalArtStyle.Outline or XFETerminalArtStyle.Shadow ? 2 : 1;
        var scaleY = options.Style == XFETerminalArtStyle.Outline ? 2 : 1;
        var glyphWidth = GlyphWidth * scaleX;
        var width = normalized.Length * glyphWidth + Math.Max(0, normalized.Length - 1) * options.LetterSpacing;
        var height = GlyphHeight * scaleY;
        var matrix = new bool[height, width];

        var offsetX = 0;
        foreach (var character in normalized)
        {
            var glyph = Glyphs[character];
            for (var glyphY = 0; glyphY < GlyphHeight; glyphY++)
                for (var glyphX = 0; glyphX < GlyphWidth; glyphX++)
                {
                    if (glyph[glyphY * GlyphWidth + glyphX] != '1')
                        continue;
                    for (var yScale = 0; yScale < scaleY; yScale++)
                        for (var xScale = 0; xScale < scaleX; xScale++)
                            matrix[glyphY * scaleY + yScale, offsetX + glyphX * scaleX + xScale] = true;
                }
            offsetX += glyphWidth + options.LetterSpacing;
        }

        if (options.Style == XFETerminalArtStyle.Outline)
            matrix = CreateOutline(matrix);

        return MatrixToRows(matrix, options.Style, modern);
    }

    private static IReadOnlyList<string> MatrixToRows(bool[,] matrix, XFETerminalArtStyle style, bool modern)
    {
        var height = matrix.GetLength(0);
        var width = matrix.GetLength(1);
        var shadow = style == XFETerminalArtStyle.Shadow;
        var primary = modern
            ? style == XFETerminalArtStyle.Dots ? '●' : '█'
            : style == XFETerminalArtStyle.Dots ? '*' : '#';
        var shadowCharacter = modern ? '░' : '.';
        var outputHeight = height + (shadow ? 1 : 0);
        var outputWidth = width + (shadow ? 1 : 0);
        var rows = new List<string>(outputHeight);

        for (var y = 0; y < outputHeight; y++)
        {
            var builder = new StringBuilder(outputWidth + height);
            if (style == XFETerminalArtStyle.Slant)
                builder.Append(' ', height - Math.Min(y, height - 1) - 1);
            for (var x = 0; x < outputWidth; x++)
            {
                var filled = y < height && x < width && matrix[y, x];
                var isShadow = shadow && !filled && y > 0 && x > 0 && matrix[y - 1, x - 1];
                builder.Append(filled ? primary : isShadow ? shadowCharacter : ' ');
            }
            rows.Add(builder.ToString().TrimEnd());
        }
        return rows;
    }

    private static bool[,] CreateOutline(bool[,] source)
    {
        var height = source.GetLength(0);
        var width = source.GetLength(1);
        var result = new bool[height, width];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                if (!source[y, x])
                    continue;
                result[y, x] = x == 0 || x == width - 1 || y == 0 || y == height - 1 ||
                    !source[y, x - 1] || !source[y, x + 1] || !source[y - 1, x] || !source[y + 1, x];
            }
        return result;
    }

    private static IReadOnlyList<string> RenderFramed(string text, bool modern)
    {
        var displayWidth = GetDisplayWidth(text);
        var horizontal = modern ? '─' : '-';
        var (topLeft, topRight, bottomLeft, bottomRight, vertical) = modern
            ? ('╭', '╮', '╰', '╯', '│')
            : ('+', '+', '+', '+', '|');
        return
        [
            $"{topLeft}{new string(horizontal, displayWidth + 2)}{topRight}",
            $"{vertical} {text} {vertical}",
            $"{bottomLeft}{new string(horizontal, displayWidth + 2)}{bottomRight}"
        ];
    }

    private static int GetDisplayWidth(string text)
    {
        var width = 0;
        var textElements = StringInfo.GetTextElementEnumerator(text);
        while (textElements.MoveNext())
        {
            var element = textElements.GetTextElement();
            width += element.EnumerateRunes().Any(IsWideRune) ? 2 : 1;
        }
        return width;
    }

    private static bool IsWideRune(Rune rune) => rune.Value is
        >= 0x1100 and <= 0x115f or
        >= 0x2e80 and <= 0xa4cf or
        >= 0xac00 and <= 0xd7a3 or
        >= 0xf900 and <= 0xfaff or
        >= 0xfe10 and <= 0xfe6f or
        >= 0xff00 and <= 0xff60 or
        >= 0x1f300 and <= 0x1faff;

    private static string ApplyAnsiColors(IReadOnlyList<string> rows, XFETerminalTitleArtOptions options)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < rows.Count; i++)
        {
            var style = new XFETerminalStyle
            {
                Foreground = ColorForRow(i, rows.Count, options),
                Bold = true
            };
            builder.Append(style.ToSequence());
            builder.Append(rows[i]);
            builder.Append(XFETerminalSequences.ResetStyle);
            if (i < rows.Count - 1)
                builder.AppendLine();
        }
        return builder.ToString();
    }

    private static XFETerminalColor ColorForRow(int row, int rowCount, XFETerminalTitleArtOptions options)
    {
        if (options.Color is { } color)
            return color;
        if (options.Palette == XFETerminalArtPalette.Default || !Palettes.TryGetValue(options.Palette, out var palette))
            return XFETerminalColor.Default;
        if (palette.Length == 1 || rowCount <= 1)
            return palette[0];
        var index = (int)Math.Round(row * (palette.Length - 1d) / (rowCount - 1d), MidpointRounding.AwayFromZero);
        return palette[Math.Clamp(index, 0, palette.Length - 1)];
    }

    private static bool IsModern(XFETerminalCompatibility compatibility) => compatibility switch
    {
        XFETerminalCompatibility.Modern => true,
        XFETerminalCompatibility.Legacy => false,
        XFETerminalCompatibility.Auto => XFETerminal.Capabilities.SupportsVirtualTerminal && XFETerminal.Capabilities.SupportsUnicode,
        _ => throw new ArgumentOutOfRangeException(nameof(compatibility))
    };

    private static void ValidateOptions(XFETerminalTitleArtOptions options)
    {
        if (!Enum.IsDefined(options.Style)) throw new ArgumentOutOfRangeException(nameof(options.Style));
        if (!Enum.IsDefined(options.Palette)) throw new ArgumentOutOfRangeException(nameof(options.Palette));
        if (!Enum.IsDefined(options.Compatibility)) throw new ArgumentOutOfRangeException(nameof(options.Compatibility));
        if (options.LetterSpacing is < 0 or > 16)
            throw new ArgumentOutOfRangeException(nameof(options.LetterSpacing), "字母间距必须在 0 到 16 之间。");
    }

    private static void ValidateText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (text.Length > 256)
            throw new ArgumentOutOfRangeException(nameof(text), "艺术字标题不能超过 256 个 UTF-16 字符。");
        if (text.Any(char.IsControl))
            throw new ArgumentException("艺术字标题不能包含换行或其他控制字符。", nameof(text));
    }

    private static bool HasColor(XFETerminalTitleArtOptions options) =>
        options.UseColor && (options.Color is not null || options.Palette != XFETerminalArtPalette.Default);
}
