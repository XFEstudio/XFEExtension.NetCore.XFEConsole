using System.Text;

namespace XFEExtension.NetCore.XFEConsole.Terminal;

/// <summary>
/// 终端边框样式。
/// </summary>
public enum XFETerminalBoxStyle
{
    /// <summary>根据终端能力选择 Unicode 单线或传统 ASCII。</summary>
    Auto,
    /// <summary>Unicode 单线。</summary>
    Single,
    /// <summary>Unicode 双线。</summary>
    Double,
    /// <summary>Unicode 圆角。</summary>
    Rounded,
    /// <summary>传统 ASCII。</summary>
    Ascii
}

/// <summary>
/// 终端画布中的一个字符单元格。
/// </summary>
/// <param name="Character">单宽字符。</param>
/// <param name="Style">文字样式。</param>
public readonly record struct XFETerminalCell(char Character, XFETerminalStyle Style)
{
    /// <summary>空白默认单元格。</summary>
    public static XFETerminalCell Empty => new(' ', default);
}

/// <summary>
/// 面向终端小游戏和 TUI 的二维字符画布，支持差量刷新。
/// 坐标从 0 开始。
/// </summary>
public sealed class XFETerminalCanvas
{
    private readonly object syncRoot = new();
    private XFETerminalCell[] cells;
    private XFETerminalCell[]? presentedCells;

    /// <summary>
    /// 创建指定尺寸的画布。
    /// </summary>
    /// <param name="width">字符列数。</param>
    /// <param name="height">字符行数。</param>
    public XFETerminalCanvas(int width, int height)
    {
        ValidateSize(width, height);
        Width = width;
        Height = height;
        cells = Enumerable.Repeat(XFETerminalCell.Empty, checked(width * height)).ToArray();
    }

    /// <summary>画布宽度。</summary>
    public int Width { get; private set; }

    /// <summary>画布高度。</summary>
    public int Height { get; private set; }

    /// <summary>
    /// 读取指定单元格。
    /// </summary>
    public XFETerminalCell this[int x, int y]
    {
        get
        {
            ValidateCoordinates(x, y);
            return cells[y * Width + x];
        }
        set
        {
            ValidateCoordinates(x, y);
            cells[y * Width + x] = value;
        }
    }

    /// <summary>
    /// 调整画布尺寸。
    /// </summary>
    /// <param name="width">新宽度。</param>
    /// <param name="height">新高度。</param>
    /// <param name="preserveContent">是否保留重叠区域。</param>
    public void Resize(int width, int height, bool preserveContent = true)
    {
        ValidateSize(width, height);
        lock (syncRoot)
        {
            if (width == Width && height == Height)
                return;

            var replacement = Enumerable.Repeat(XFETerminalCell.Empty, checked(width * height)).ToArray();
            if (preserveContent)
            {
                var copyWidth = Math.Min(width, Width);
                var copyHeight = Math.Min(height, Height);
                for (var y = 0; y < copyHeight; y++)
                    Array.Copy(cells, y * Width, replacement, y * width, copyWidth);
            }

            Width = width;
            Height = height;
            cells = replacement;
            presentedCells = null;
        }
    }

    /// <summary>使用空格和默认样式清空画布。</summary>
    public void Clear() => Clear(' ', default);

    /// <summary>
    /// 使用指定字符和样式清空画布。
    /// </summary>
    public void Clear(char character, XFETerminalStyle style)
    {
        lock (syncRoot)
            Array.Fill(cells, new XFETerminalCell(character, style));
    }

    /// <summary>
    /// 设置单个字符；越界时安全忽略。
    /// </summary>
    public void Set(int x, int y, char character, XFETerminalStyle style = default)
    {
        if (!Contains(x, y))
            return;
        cells[y * Width + x] = new XFETerminalCell(character, style);
    }

    /// <summary>
    /// 在画布中绘制文本，支持换行并自动裁剪。
    /// </summary>
    public void DrawText(int x, int y, string text, XFETerminalStyle style = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        var startX = x;
        foreach (var character in text)
        {
            if (character == '\r')
                continue;
            if (character == '\n')
            {
                x = startX;
                y++;
                continue;
            }
            Set(x++, y, character, style);
        }
    }

    /// <summary>
    /// 填充矩形区域，超出画布的部分会被裁剪。
    /// </summary>
    public void FillRectangle(int x, int y, int width, int height, char character, XFETerminalStyle style = default)
    {
        if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));
        var left = Math.Max(0, x);
        var top = Math.Max(0, y);
        var right = Math.Min(Width, x + width);
        var bottom = Math.Min(Height, y + height);
        for (var row = top; row < bottom; row++)
            for (var column = left; column < right; column++)
                Set(column, row, character, style);
    }

    /// <summary>
    /// 绘制矩形边框。
    /// </summary>
    public void DrawBox(
        int x,
        int y,
        int width,
        int height,
        XFETerminalBoxStyle boxStyle = XFETerminalBoxStyle.Auto,
        XFETerminalStyle style = default)
    {
        if (width < 2) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 2) throw new ArgumentOutOfRangeException(nameof(height));
        if (boxStyle == XFETerminalBoxStyle.Auto)
            boxStyle = XFETerminal.Capabilities.SupportsUnicode
                ? XFETerminalBoxStyle.Single
                : XFETerminalBoxStyle.Ascii;
        var symbols = boxStyle switch
        {
            XFETerminalBoxStyle.Single => ('┌', '┐', '└', '┘', '─', '│'),
            XFETerminalBoxStyle.Double => ('╔', '╗', '╚', '╝', '═', '║'),
            XFETerminalBoxStyle.Rounded => ('╭', '╮', '╰', '╯', '─', '│'),
            XFETerminalBoxStyle.Ascii => ('+', '+', '+', '+', '-', '|'),
            _ => throw new ArgumentOutOfRangeException(nameof(boxStyle))
        };

        Set(x, y, symbols.Item1, style);
        Set(x + width - 1, y, symbols.Item2, style);
        Set(x, y + height - 1, symbols.Item3, style);
        Set(x + width - 1, y + height - 1, symbols.Item4, style);
        for (var column = x + 1; column < x + width - 1; column++)
        {
            Set(column, y, symbols.Item5, style);
            Set(column, y + height - 1, symbols.Item5, style);
        }
        for (var row = y + 1; row < y + height - 1; row++)
        {
            Set(x, row, symbols.Item6, style);
            Set(x + width - 1, row, symbols.Item6, style);
        }
    }

    /// <summary>
    /// 生成完整画布文本而不输出。
    /// </summary>
    /// <param name="includeStyles">是否包含 ANSI 样式序列。</param>
    /// <param name="newLine">行分隔符。</param>
    /// <returns>画布文本。</returns>
    public string Render(bool includeStyles = true, string newLine = "\n")
    {
        ArgumentNullException.ThrowIfNull(newLine);
        lock (syncRoot)
        {
            var builder = new StringBuilder(Width * Height + Height * newLine.Length);
            var currentStyle = default(XFETerminalStyle);
            var hasCurrentStyle = false;
            var styleIsActive = false;
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var cell = cells[y * Width + x];
                    if (includeStyles && (!hasCurrentStyle || cell.Style != currentStyle))
                    {
                        if (styleIsActive)
                            builder.Append(XFETerminalSequences.ResetStyle);
                        var sequence = cell.Style.ToSequence();
                        builder.Append(sequence);
                        styleIsActive = sequence.Length > 0;
                        currentStyle = cell.Style;
                        hasCurrentStyle = true;
                    }
                    builder.Append(cell.Character);
                }
                if (y < Height - 1)
                    builder.Append(newLine);
            }
            if (includeStyles && styleIsActive)
                builder.Append(XFETerminalSequences.ResetStyle);
            return builder.ToString();
        }
    }

    /// <summary>
    /// 将画布刷新到终端。VT 终端只写入变化的单元格；旧控制台使用 Console 光标和颜色 API。
    /// </summary>
    /// <param name="writer">输出目标。</param>
    /// <param name="forceFullRedraw">是否忽略差量缓存并重绘全部。</param>
    /// <param name="compatibility">强制 VT 或传统输出；Auto 表示自动检测。</param>
    public void Present(
        TextWriter? writer = null,
        bool forceFullRedraw = false,
        XFETerminalCompatibility compatibility = XFETerminalCompatibility.Auto)
    {
        var writesToLocalTerminal = writer is null;
        writer ??= XFETerminal.GetLocalWriter();
        var useVirtualTerminal = compatibility switch
        {
            XFETerminalCompatibility.Auto => XFETerminal.Capabilities.SupportsVirtualTerminal,
            XFETerminalCompatibility.Modern => true,
            XFETerminalCompatibility.Legacy => false,
            _ => throw new ArgumentOutOfRangeException(nameof(compatibility))
        };
        lock (syncRoot)
        {
            if (useVirtualTerminal)
                PresentVirtualTerminal(writer, forceFullRedraw);
            else if (!writesToLocalTerminal || !XFETerminal.Capabilities.IsInteractive)
                writer.Write(Render(includeStyles: false));
            else
                PresentLegacy(writer, forceFullRedraw);
            presentedCells = (XFETerminalCell[])cells.Clone();
            writer.Flush();
        }
    }

    /// <summary>清除差量刷新缓存，使下一帧完整重绘。</summary>
    public void Invalidate() => presentedCells = null;

    private void PresentVirtualTerminal(TextWriter writer, bool forceFullRedraw)
    {
        var builder = new StringBuilder();
        var previous = forceFullRedraw ? null : presentedCells;
        XFETerminalStyle? activeStyle = null;

        for (var y = 0; y < Height; y++)
        {
            var x = 0;
            while (x < Width)
            {
                var index = y * Width + x;
                if (previous is not null && previous[index] == cells[index])
                {
                    x++;
                    continue;
                }

                builder.Append(XFETerminalSequences.CursorPosition(y + 1, x + 1));
                while (x < Width)
                {
                    index = y * Width + x;
                    if (previous is not null && previous[index] == cells[index])
                        break;
                    var cell = cells[index];
                    if (activeStyle is null || activeStyle.Value != cell.Style)
                    {
                        builder.Append(XFETerminalSequences.ResetStyle);
                        builder.Append(cell.Style.ToSequence());
                        activeStyle = cell.Style;
                    }
                    builder.Append(cell.Character);
                    x++;
                }
            }
        }

        if (builder.Length > 0)
            builder.Append(XFETerminalSequences.ResetStyle);
        writer.Write(builder);
    }

    private void PresentLegacy(TextWriter writer, bool forceFullRedraw)
    {
        var previous = forceFullRedraw ? null : presentedCells;
        var originalForeground = SafeForegroundColor();
        var originalBackground = SafeBackgroundColor();
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
            {
                var index = y * Width + x;
                if (previous is not null && previous[index] == cells[index])
                    continue;
                TrySetCursorPosition(x, y);
                var cell = cells[index];
                if (cell.Style.Foreground is { } foreground)
                    TrySetForegroundColor(foreground.ToConsoleColor());
                if (cell.Style.Background is { } background)
                    TrySetBackgroundColor(background.ToConsoleColor());
                writer.Write(cell.Character);
            }
        if (originalForeground is { } foregroundColor) TrySetForegroundColor(foregroundColor);
        if (originalBackground is { } backgroundColor) TrySetBackgroundColor(backgroundColor);
    }

    private bool Contains(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    private void ValidateCoordinates(int x, int y)
    {
        if (!Contains(x, y))
            throw new ArgumentOutOfRangeException($"({x}, {y})", "坐标超出画布范围。");
    }

    private static void ValidateSize(int width, int height)
    {
        if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
        if ((long)width * height > 16_777_216)
            throw new ArgumentOutOfRangeException(nameof(width), "画布最多包含 16777216 个单元格。");
    }

    private static void TrySetCursorPosition(int x, int y)
    {
        try { Console.SetCursorPosition(x + Console.WindowLeft, y + Console.WindowTop); }
        catch (IOException) { }
        catch (ArgumentOutOfRangeException) { }
    }

    private static ConsoleColor? SafeForegroundColor()
    {
        try { return Console.ForegroundColor; }
        catch (IOException) { return null; }
    }

    private static ConsoleColor? SafeBackgroundColor()
    {
        try { return Console.BackgroundColor; }
        catch (IOException) { return null; }
    }

    private static void TrySetForegroundColor(ConsoleColor color)
    {
        try { Console.ForegroundColor = color; }
        catch (IOException) { }
    }

    private static void TrySetBackgroundColor(ConsoleColor color)
    {
        try { Console.BackgroundColor = color; }
        catch (IOException) { }
    }
}
