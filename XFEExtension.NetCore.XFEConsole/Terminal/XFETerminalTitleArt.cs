using Figgle;
using Figgle.Fonts;
using System.Globalization;
using System.Reflection;
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
    Framed,
    /// <summary>带可配置深度的立体挤出字。</summary>
    ThreeDimensional
}

/// <summary>
/// 常用的终端艺术字体。除 <see cref="Pixel"/> 外均由 FIGlet 字体提供。
/// </summary>
public enum XFETerminalArtFont
{
    /// <summary>XFEConsole 内置的 5x5 点阵字体。</summary>
    Pixel,
    /// <summary>FIGlet Standard。</summary>
    Standard,
    /// <summary>FIGlet Big。</summary>
    Big,
    /// <summary>FIGlet Small。</summary>
    Small,
    /// <summary>FIGlet Slant。</summary>
    Slant,
    /// <summary>ANSI Shadow 风格；映射至 FIGlet Shadow。</summary>
    AnsiShadow,
    /// <summary>FIGlet Shadow Small。</summary>
    SmallShadow,
    /// <summary>FIGlet Doom。</summary>
    Doom,
    /// <summary>FIGlet Epic。</summary>
    Epic,
    /// <summary>FIGlet Gothic。</summary>
    Gothic,
    /// <summary>FIGlet Ivrit。</summary>
    Ivrit,
    /// <summary>FIGlet Modular。</summary>
    Modular,
    /// <summary>FIGlet Ogre。</summary>
    Ogre,
    /// <summary>FIGlet Rectangles。</summary>
    Rectangles,
    /// <summary>FIGlet Relief。</summary>
    Relief,
    /// <summary>FIGlet Isometric1。</summary>
    Isometric,
    /// <summary>FIGlet Larry3d。</summary>
    Larry3D
}

/// <summary>
/// 立体艺术字的挤出方向。
/// </summary>
public enum XFETerminalArtExtrudeDirection
{
    /// <summary>向右。</summary>
    Right,
    /// <summary>向左。</summary>
    Left,
    /// <summary>向下。</summary>
    Down,
    /// <summary>向上。</summary>
    Up,
    /// <summary>向右下。</summary>
    DownRight,
    /// <summary>向左下。</summary>
    DownLeft,
    /// <summary>向右上。</summary>
    UpRight,
    /// <summary>向左上。</summary>
    UpLeft
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

    /// <summary>艺术字体；默认使用兼容原有版本的内置点阵字体。</summary>
    public XFETerminalArtFont Font { get; set; } = XFETerminalArtFont.Pixel;

    /// <summary>
    /// 任意 FIGlet 字体名。设置后优先于 <see cref="Font"/>；可从
    /// <see cref="XFETerminalTitleArt.AvailableFigletFonts"/> 获取全部名称。
    /// </summary>
    public string? FigletFontName { get; set; }

    /// <summary>内置配色。</summary>
    public XFETerminalArtPalette Palette { get; set; } = XFETerminalArtPalette.Cyan;

    /// <summary>兼容模式。</summary>
    public XFETerminalCompatibility Compatibility { get; set; } = XFETerminalCompatibility.Auto;

    /// <summary>是否输出颜色。传统模式的返回字符串始终不含控制符，直接显示时使用 ConsoleColor。</summary>
    public bool UseColor { get; set; } = true;

    /// <summary>覆盖内置配色的单一颜色；为空时使用 Palette。</summary>
    public XFETerminalColor? Color { get; set; }

    /// <summary>外描边宽度，0 表示不描边；范围 0 到 8。</summary>
    public int OutlineWidth { get; set; }

    /// <summary>描边颜色；为空时自动使用高对比度颜色。</summary>
    public XFETerminalColor? OutlineColor { get; set; }

    /// <summary>描边字符；为空时根据新旧终端选择 Unicode 或 ASCII 字符。</summary>
    public char? OutlineCharacter { get; set; }

    /// <summary>立体挤出深度，0 表示不挤出；范围 0 到 32。</summary>
    public int ExtrudeDepth { get; set; }

    /// <summary>立体挤出方向。</summary>
    public XFETerminalArtExtrudeDirection ExtrudeDirection { get; set; } = XFETerminalArtExtrudeDirection.DownRight;

    /// <summary>立体挤出层颜色；为空时使用内置暗色。</summary>
    public XFETerminalColor? ExtrudeColor { get; set; }

    /// <summary>立体挤出字符；为空时根据新旧终端选择 Unicode 或 ASCII 字符。</summary>
    public char? ExtrudeCharacter { get; set; }

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

    private enum ArtLayer : byte
    {
        None,
        Extrude,
        Outline,
        Fill
    }

    private readonly record struct ArtCell(char Character, ArtLayer Layer);

    private sealed class RenderedArt(ArtCell[][] rows)
    {
        public ArtCell[][] Rows { get; } = rows;

        public int Height => Rows.Length;
    }

    private static readonly PropertyInfo[] FigletFontProperties = typeof(FiggleFonts)
        .GetProperties(BindingFlags.Public | BindingFlags.Static)
        .Where(property => property.PropertyType == typeof(FiggleFont) && property.GetMethod is not null)
        .OrderBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static readonly Dictionary<string, FiggleFont> FigletFontCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly object FigletFontCacheLock = new();

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

    /// <summary>取得全部可通过 <see cref="XFETerminalTitleArtOptions.FigletFontName"/> 使用的 FIGlet 字体名。</summary>
    public static IReadOnlyList<string> AvailableFigletFonts { get; } =
        Array.AsReadOnly(FigletFontProperties.Select(property => property.Name).ToArray());

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
        var art = Render(text, options, modern);
        return HasColor(options) && modern ? ApplyAnsiColors(art, options) : ToPlainText(art);
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
        => GeneratePlain(text, new XFETerminalTitleArtOptions
        {
            Style = style,
            Compatibility = compatibility,
            UseColor = false,
            LetterSpacing = letterSpacing
        });

    /// <summary>
    /// 使用完整选项生成不包含 ANSI 控制符的纯艺术字字符串。
    /// </summary>
    /// <param name="text">标题文本。</param>
    /// <param name="options">字体、描边与立体效果选项；颜色选项会被忽略。</param>
    /// <returns>纯文本艺术字。</returns>
    public static string GeneratePlain(string text, XFETerminalTitleArtOptions options)
    {
        ValidateText(text);
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);
        return ToPlainText(Render(text, options, IsModern(options.Compatibility)));
    }

    /// <summary>
    /// 检查指定 FIGlet 字体名是否可用。比较时忽略大小写、空格、连字符和下划线。
    /// </summary>
    public static bool IsFigletFontAvailable(string? fontName) => FindFigletFontProperty(fontName) is not null;

    /// <summary>
    /// 直接向终端显示艺术字。传统模式会通过 ConsoleColor 为正面、描边和挤出层着色。
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
        var art = Render(text, options, modern);
        var hasColor = HasColor(options);

        if (modern || !hasColor || !useConsoleColor)
        {
            writer.Write(modern && hasColor ? ApplyAnsiColors(art, options) : ToPlainText(art));
            if (options.WriteLine)
                writer.WriteLine();
            writer.Flush();
            return;
        }

        WriteLegacyColored(art, options, writer);
    }

    private static RenderedArt Render(string text, XFETerminalTitleArtOptions options, bool modern)
    {
        IReadOnlyList<string> rows;
        if (options.Style == XFETerminalArtStyle.Framed)
        {
            rows = RenderFramed(text, modern);
        }
        else if (options.Font != XFETerminalArtFont.Pixel || !string.IsNullOrWhiteSpace(options.FigletFontName))
        {
            var font = ResolveFigletFont(options);
            rows = text.Any(character => !char.IsWhiteSpace(character) && !font.Contains(character))
                ? RenderFramed(text, modern)
                : NormalizeRows(font.Render(text));
        }
        else
        {
            rows = RenderRows(text, options, modern);
        }

        var outlineWidth = options.OutlineWidth;
        if (outlineWidth == 0 && options.Style == XFETerminalArtStyle.Outline)
            outlineWidth = 1;

        var extrudeDepth = options.ExtrudeDepth;
        if (extrudeDepth == 0 && options.Style == XFETerminalArtStyle.Shadow)
            extrudeDepth = 1;
        else if (extrudeDepth == 0 && options.Style == XFETerminalArtStyle.ThreeDimensional)
            extrudeDepth = 3;

        return ApplySpatialEffects(rows, outlineWidth, extrudeDepth, options, modern);
    }

    private static RenderedArt ApplySpatialEffects(
        IReadOnlyList<string> sourceRows,
        int outlineWidth,
        int extrudeDepth,
        XFETerminalTitleArtOptions options,
        bool modern)
    {
        var rows = TrimEmptyRows(sourceRows);
        var sourceHeight = rows.Count;
        var sourceWidth = rows.Max(row => row.Length);
        var (directionX, directionY) = DirectionOffset(options.ExtrudeDirection);
        var totalExtrudeDepth = extrudeDepth == 0 ? 0 : extrudeDepth + outlineWidth;
        var minX = Math.Min(-outlineWidth, directionX * totalExtrudeDepth);
        var minY = Math.Min(-outlineWidth, directionY * totalExtrudeDepth);
        var maxX = Math.Max(sourceWidth - 1 + outlineWidth, sourceWidth - 1 + directionX * totalExtrudeDepth);
        var maxY = Math.Max(sourceHeight - 1 + outlineWidth, sourceHeight - 1 + directionY * totalExtrudeDepth);
        var outputWidth = maxX - minX + 1;
        var outputHeight = maxY - minY + 1;
        var cells = new ArtCell[outputHeight][];
        for (var y = 0; y < outputHeight; y++)
            cells[y] = new ArtCell[outputWidth];

        var originX = -minX;
        var originY = -minY;
        var outlineCharacter = options.OutlineCharacter ?? (modern ? '█' : '+');
        var extrudeCharacter = options.ExtrudeCharacter ?? (modern ? '▓' : '#');

        for (var depth = totalExtrudeDepth; depth >= 1; depth--)
            for (var y = 0; y < sourceHeight; y++)
                for (var x = 0; x < rows[y].Length; x++)
                {
                    if (rows[y][x] == ' ')
                        continue;
                    var targetX = originX + x + directionX * depth;
                    var targetY = originY + y + directionY * depth;
                    if (cells[targetY][targetX].Layer == ArtLayer.None)
                        cells[targetY][targetX] = new ArtCell(extrudeCharacter, ArtLayer.Extrude);
                }

        if (outlineWidth > 0)
            for (var y = 0; y < sourceHeight; y++)
                for (var x = 0; x < rows[y].Length; x++)
                {
                    if (rows[y][x] == ' ')
                        continue;
                    for (var offsetY = -outlineWidth; offsetY <= outlineWidth; offsetY++)
                        for (var offsetX = -outlineWidth; offsetX <= outlineWidth; offsetX++)
                        {
                            if (offsetX == 0 && offsetY == 0)
                                continue;
                            var targetX = originX + x + offsetX;
                            var targetY = originY + y + offsetY;
                            cells[targetY][targetX] = new ArtCell(outlineCharacter, ArtLayer.Outline);
                        }
                }

        for (var y = 0; y < sourceHeight; y++)
            for (var x = 0; x < rows[y].Length; x++)
            {
                var character = rows[y][x];
                if (character != ' ')
                    cells[originY + y][originX + x] = new ArtCell(character, ArtLayer.Fill);
            }

        return new RenderedArt(cells);
    }

    private static (int X, int Y) DirectionOffset(XFETerminalArtExtrudeDirection direction) => direction switch
    {
        XFETerminalArtExtrudeDirection.Right => (1, 0),
        XFETerminalArtExtrudeDirection.Left => (-1, 0),
        XFETerminalArtExtrudeDirection.Down => (0, 1),
        XFETerminalArtExtrudeDirection.Up => (0, -1),
        XFETerminalArtExtrudeDirection.DownRight => (1, 1),
        XFETerminalArtExtrudeDirection.DownLeft => (-1, 1),
        XFETerminalArtExtrudeDirection.UpRight => (1, -1),
        XFETerminalArtExtrudeDirection.UpLeft => (-1, -1),
        _ => throw new ArgumentOutOfRangeException(nameof(direction))
    };

    private static FiggleFont ResolveFigletFont(XFETerminalTitleArtOptions options)
    {
        var requestedName = string.IsNullOrWhiteSpace(options.FigletFontName)
            ? FontName(options.Font)
            : options.FigletFontName;
        var property = FindFigletFontProperty(requestedName) ??
            throw new ArgumentException($"找不到 FIGlet 字体“{requestedName}”。请从 AvailableFigletFonts 选择字体。", nameof(options));

        lock (FigletFontCacheLock)
        {
            if (FigletFontCache.TryGetValue(property.Name, out var cached))
                return cached;
            var font = (FiggleFont?)property.GetValue(null) ??
                throw new InvalidOperationException($"无法加载 FIGlet 字体“{property.Name}”。");
            FigletFontCache[property.Name] = font;
            return font;
        }
    }

    private static PropertyInfo? FindFigletFontProperty(string? fontName)
    {
        if (string.IsNullOrWhiteSpace(fontName))
            return null;
        var normalized = NormalizeFontName(fontName);
        return FigletFontProperties.FirstOrDefault(property => NormalizeFontName(property.Name) == normalized);
    }

    private static string NormalizeFontName(string fontName) =>
        new(fontName.Where(character => character is not (' ' or '-' or '_')).Select(char.ToUpperInvariant).ToArray());

    private static string FontName(XFETerminalArtFont font) => font switch
    {
        XFETerminalArtFont.Pixel => throw new ArgumentException("Pixel 不是 FIGlet 字体。", nameof(font)),
        XFETerminalArtFont.AnsiShadow => "Shadow",
        XFETerminalArtFont.SmallShadow => "ShadowSmall",
        XFETerminalArtFont.Isometric => "Isometric1",
        XFETerminalArtFont.Larry3D => "Larry3d",
        _ => font.ToString()
    };

    private static IReadOnlyList<string> NormalizeRows(string rendered) =>
        TrimEmptyRows(rendered.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(row => row.TrimEnd()).ToArray());

    private static IReadOnlyList<string> TrimEmptyRows(IReadOnlyList<string> sourceRows)
    {
        var first = 0;
        var last = sourceRows.Count - 1;
        while (first <= last && string.IsNullOrWhiteSpace(sourceRows[first])) first++;
        while (last >= first && string.IsNullOrWhiteSpace(sourceRows[last])) last--;
        return first > last ? [string.Empty] : sourceRows.Skip(first).Take(last - first + 1).ToArray();
    }

    private static IReadOnlyList<string> RenderRows(
        string text,
        XFETerminalTitleArtOptions options,
        bool modern)
    {
        var normalized = text.ToUpperInvariant();
        if (options.Style == XFETerminalArtStyle.Framed || normalized.Any(character => !Glyphs.ContainsKey(character)))
            return RenderFramed(text, modern);

        var scaleX = options.Style is XFETerminalArtStyle.Block or XFETerminalArtStyle.Outline or
            XFETerminalArtStyle.Shadow or XFETerminalArtStyle.ThreeDimensional ? 2 : 1;
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

        return MatrixToRows(matrix, options.Style, modern);
    }

    private static IReadOnlyList<string> MatrixToRows(bool[,] matrix, XFETerminalArtStyle style, bool modern)
    {
        var height = matrix.GetLength(0);
        var width = matrix.GetLength(1);
        var primary = modern
            ? style == XFETerminalArtStyle.Dots ? '●' : '█'
            : style == XFETerminalArtStyle.Dots ? '*' : '#';
        var rows = new List<string>(height);

        for (var y = 0; y < height; y++)
        {
            var builder = new StringBuilder(width + height);
            if (style == XFETerminalArtStyle.Slant)
                builder.Append(' ', height - y - 1);
            for (var x = 0; x < width; x++)
                builder.Append(matrix[y, x] ? primary : ' ');
            rows.Add(builder.ToString().TrimEnd());
        }
        return rows;
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

    private static string ToPlainText(RenderedArt art)
    {
        var lines = new string[art.Height];
        for (var y = 0; y < art.Height; y++)
        {
            var builder = new StringBuilder(art.Rows[y].Length);
            foreach (var cell in art.Rows[y])
                builder.Append(cell.Layer == ArtLayer.None ? ' ' : cell.Character);
            lines[y] = builder.ToString().TrimEnd();
        }
        return string.Join(Environment.NewLine, lines);
    }

    private static string ApplyAnsiColors(RenderedArt art, XFETerminalTitleArtOptions options)
    {
        var builder = new StringBuilder();
        for (var row = 0; row < art.Height; row++)
        {
            XFETerminalStyle? activeStyle = null;
            var visibleLength = GetVisibleLength(art.Rows[row]);
            for (var column = 0; column < visibleLength; column++)
            {
                var cell = art.Rows[row][column];
                if (cell.Layer == ArtLayer.None)
                {
                    builder.Append(' ');
                    continue;
                }

                var style = StyleForLayer(cell.Layer, row, art.Height, options);
                if (style != activeStyle)
                {
                    if (activeStyle is not null)
                        builder.Append(XFETerminalSequences.ResetStyle);
                    builder.Append(style.ToSequence());
                    activeStyle = style;
                }
                builder.Append(cell.Character);
            }
            builder.Append(XFETerminalSequences.ResetStyle);
            if (row < art.Height - 1)
                builder.AppendLine();
        }
        return builder.ToString();
    }

    private static void WriteLegacyColored(RenderedArt art, XFETerminalTitleArtOptions options, TextWriter writer)
    {
        ConsoleColor? original = null;
        try { original = Console.ForegroundColor; }
        catch (IOException) { }

        XFETerminalColor? activeColor = null;
        for (var row = 0; row < art.Height; row++)
        {
            var visibleLength = GetVisibleLength(art.Rows[row]);
            for (var column = 0; column < visibleLength; column++)
            {
                var cell = art.Rows[row][column];
                if (cell.Layer != ArtLayer.None)
                {
                    var color = ColorForLayer(cell.Layer, row, art.Height, options);
                    if (color != activeColor)
                    {
                        try { Console.ForegroundColor = color.ToConsoleColor(); }
                        catch (IOException) { }
                        activeColor = color;
                    }
                    writer.Write(cell.Character);
                }
                else
                {
                    writer.Write(' ');
                }
            }
            if (row < art.Height - 1 || options.WriteLine)
                writer.WriteLine();
        }

        if (original is { } originalColor)
        {
            try { Console.ForegroundColor = originalColor; }
            catch (IOException) { }
        }
        writer.Flush();
    }

    private static int GetVisibleLength(ArtCell[] row)
    {
        var length = row.Length;
        while (length > 0 && row[length - 1].Layer == ArtLayer.None)
            length--;
        return length;
    }

    private static XFETerminalStyle StyleForLayer(
        ArtLayer layer,
        int row,
        int rowCount,
        XFETerminalTitleArtOptions options) => new()
        {
            Foreground = ColorForLayer(layer, row, rowCount, options),
            Bold = layer is ArtLayer.Fill or ArtLayer.Outline,
            Dim = layer == ArtLayer.Extrude
        };

    private static XFETerminalColor ColorForLayer(
        ArtLayer layer,
        int row,
        int rowCount,
        XFETerminalTitleArtOptions options) => layer switch
        {
            ArtLayer.Outline => options.OutlineColor ?? XFETerminalColor.White,
            ArtLayer.Extrude => options.ExtrudeColor ?? XFETerminalColor.FromRgb(0x35, 0x3c, 0x58),
            _ => ColorForRow(row, rowCount, options)
        };

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
        if (!Enum.IsDefined(options.Font)) throw new ArgumentOutOfRangeException(nameof(options.Font));
        if (!Enum.IsDefined(options.Palette)) throw new ArgumentOutOfRangeException(nameof(options.Palette));
        if (!Enum.IsDefined(options.Compatibility)) throw new ArgumentOutOfRangeException(nameof(options.Compatibility));
        if (!Enum.IsDefined(options.ExtrudeDirection)) throw new ArgumentOutOfRangeException(nameof(options.ExtrudeDirection));
        if (options.LetterSpacing is < 0 or > 16)
            throw new ArgumentOutOfRangeException(nameof(options.LetterSpacing), "字母间距必须在 0 到 16 之间。");
        if (options.OutlineWidth is < 0 or > 8)
            throw new ArgumentOutOfRangeException(nameof(options.OutlineWidth), "描边宽度必须在 0 到 8 之间。");
        if (options.ExtrudeDepth is < 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(options.ExtrudeDepth), "立体挤出深度必须在 0 到 32 之间。");
        if (options.FigletFontName is { Length: > 128 } || options.FigletFontName?.Any(char.IsControl) == true)
            throw new ArgumentException("FIGlet 字体名无效。", nameof(options.FigletFontName));
        ValidateEffectCharacter(options.OutlineCharacter, nameof(options.OutlineCharacter));
        ValidateEffectCharacter(options.ExtrudeCharacter, nameof(options.ExtrudeCharacter));
    }

    private static void ValidateEffectCharacter(char? character, string parameterName)
    {
        if (character is { } value && char.IsWhiteSpace(value))
            throw new ArgumentException("效果字符不能是空白或控制字符。", parameterName);
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
        options.UseColor && (options.Color is not null || options.OutlineColor is not null ||
            options.ExtrudeColor is not null || options.Palette != XFETerminalArtPalette.Default);
}
