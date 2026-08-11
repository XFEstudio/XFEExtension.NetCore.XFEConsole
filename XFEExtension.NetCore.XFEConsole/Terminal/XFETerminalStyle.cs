namespace XFEExtension.NetCore.XFEConsole.Terminal;

/// <summary>
/// 一组可组合的终端文字样式。
/// </summary>
public readonly record struct XFETerminalStyle
{
    /// <summary>前景色；为空时不修改。</summary>
    public XFETerminalColor? Foreground { get; init; }

    /// <summary>背景色；为空时不修改。</summary>
    public XFETerminalColor? Background { get; init; }

    /// <summary>粗体或高亮。</summary>
    public bool Bold { get; init; }

    /// <summary>暗淡显示。</summary>
    public bool Dim { get; init; }

    /// <summary>斜体。</summary>
    public bool Italic { get; init; }

    /// <summary>下划线。</summary>
    public bool Underline { get; init; }

    /// <summary>缓慢闪烁。</summary>
    public bool Blink { get; init; }

    /// <summary>交换前景色与背景色。</summary>
    public bool Inverse { get; init; }

    /// <summary>隐藏文字。</summary>
    public bool Hidden { get; init; }

    /// <summary>删除线。</summary>
    public bool Strikethrough { get; init; }

    /// <summary>
    /// 生成启用当前样式的 SGR 序列。
    /// </summary>
    /// <returns>ANSI SGR 字符串。</returns>
    public string ToSequence()
    {
        var parameters = new List<string>(10);
        if (Bold) parameters.Add("1");
        if (Dim) parameters.Add("2");
        if (Italic) parameters.Add("3");
        if (Underline) parameters.Add("4");
        if (Blink) parameters.Add("5");
        if (Inverse) parameters.Add("7");
        if (Hidden) parameters.Add("8");
        if (Strikethrough) parameters.Add("9");
        if (Foreground is { } foreground) parameters.Add(foreground.ToSgrParameters(true));
        if (Background is { } background) parameters.Add(background.ToSgrParameters(false));
        return parameters.Count == 0 ? string.Empty : $"\x1b[{string.Join(';', parameters)}m";
    }

    /// <summary>
    /// 把当前样式应用到文本，并可在结尾恢复默认样式。
    /// </summary>
    /// <param name="text">待装饰文本。</param>
    /// <param name="reset">是否追加样式重置序列。</param>
    /// <returns>带 ANSI 样式的文本。</returns>
    public string Apply(string text, bool reset = true) =>
        $"{ToSequence()}{text}{(reset ? XFETerminalSequences.ResetStyle : string.Empty)}";
}
