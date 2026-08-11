using System.Globalization;
using System.Text;

namespace XFEExtension.NetCore.XFEConsole.Terminal;

/// <summary>
/// 行内进度条显示选项。
/// </summary>
public sealed class XFETerminalProgressBarOptions
{
    /// <summary>进度条单元格宽度。默认为 32。</summary>
    public int Width { get; set; } = 32;

    /// <summary>完成区域字符；为空时现代终端用 █，传统终端用 #。</summary>
    public char? CompletedCharacter { get; set; }

    /// <summary>未完成区域字符；为空时现代终端用 ░，传统终端用 -。</summary>
    public char? RemainingCharacter { get; set; }

    /// <summary>进度条前缀。</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>进度条后缀。</summary>
    public string Suffix { get; set; } = string.Empty;

    /// <summary>是否显示百分比。</summary>
    public bool ShowPercentage { get; set; } = true;

    /// <summary>是否同步 Windows Terminal 标签页及任务栏进度。</summary>
    public bool UseTaskbarProgress { get; set; } = true;

    /// <summary>释放进度条时是否换行。</summary>
    public bool WriteLineOnDispose { get; set; } = true;

    /// <summary>完成区域颜色。</summary>
    public XFETerminalColor CompletedColor { get; set; } = XFETerminalColor.FromRgb(0x35, 0xc7, 0x59);

    /// <summary>未完成区域颜色。</summary>
    public XFETerminalColor RemainingColor { get; set; } = XFETerminalColor.FromRgb(0x76, 0x76, 0x76);

    /// <summary>强制现代或传统输出，Auto 表示自动检测。</summary>
    public XFETerminalCompatibility Compatibility { get; set; } = XFETerminalCompatibility.Auto;
}

/// <summary>
/// 同时支持行内显示和 Windows Terminal 任务栏进度的进度条。
/// </summary>
public sealed class XFETerminalProgressBar : IProgress<double>, IDisposable
{
    private readonly object syncRoot = new();
    private readonly TextWriter writer;
    private readonly XFETerminalProgressBarOptions options;
    private readonly bool modern;
    private readonly bool writesToLocalTerminal;
    private readonly char completedCharacter;
    private readonly char remainingCharacter;
    private XFETerminalProgressState state = XFETerminalProgressState.Normal;
    private bool disposed;
    private int pulse;
    private int lastPlainTextLength;

    /// <summary>
    /// 创建进度条。
    /// </summary>
    /// <param name="options">显示选项。</param>
    /// <param name="writer">输出目标。</param>
    public XFETerminalProgressBar(XFETerminalProgressBarOptions? options = null, TextWriter? writer = null)
    {
        this.options = options ?? new XFETerminalProgressBarOptions();
        if (this.options.Width is < 1 or > 4096)
            throw new ArgumentOutOfRangeException(nameof(options), "进度条宽度必须在 1 到 4096 之间。");

        writesToLocalTerminal = writer is null;
        this.writer = writer ?? XFETerminal.GetLocalWriter();
        modern = this.options.Compatibility switch
        {
            XFETerminalCompatibility.Modern => true,
            XFETerminalCompatibility.Legacy => false,
            _ => XFETerminal.Capabilities.SupportsVirtualTerminal
        };
        completedCharacter = this.options.CompletedCharacter ?? (modern ? '█' : '#');
        remainingCharacter = this.options.RemainingCharacter ?? (modern ? '░' : '-');
    }

    /// <summary>当前进度状态。</summary>
    public XFETerminalProgressState State
    {
        get => state;
        set
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            state = value;
        }
    }

    /// <summary>
    /// 报告 0.0 到 1.0 的进度。
    /// </summary>
    /// <param name="value">进度比例，超出范围时会被截断。</param>
    public void Report(double value) => Report(value, null);

    /// <summary>
    /// 报告进度并显示状态文本。
    /// </summary>
    /// <param name="value">进度比例，超出范围时会被截断。</param>
    /// <param name="message">可选状态文本。</param>
    public void Report(double value, string? message)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (double.IsNaN(value))
            throw new ArgumentOutOfRangeException(nameof(value));
        value = Math.Clamp(value, 0d, 1d);

        lock (syncRoot)
        {
            var output = Render(value, message);
            WriteFrame(output);
            writer.Flush();
            if (writesToLocalTerminal && options.UseTaskbarProgress && XFETerminal.Capabilities.SupportsTaskbarProgress)
            {
                writer.Write(XFETerminalSequences.SetProgress(state, (int)Math.Round(value * 100, MidpointRounding.AwayFromZero)));
                writer.Flush();
            }
        }
    }

    /// <summary>
    /// 显示一个无法确定百分比的动画帧。
    /// </summary>
    /// <param name="message">可选状态文本。</param>
    public void Pulse(string? message = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        lock (syncRoot)
        {
            state = XFETerminalProgressState.Indeterminate;
            var width = options.Width;
            var position = pulse++ % Math.Max(1, width);
            var bar = new string(remainingCharacter, position) + completedCharacter +
                new string(remainingCharacter, Math.Max(0, width - position - 1));
            var text = $"{options.Prefix}[{bar}]{FormatMessage(message)}{options.Suffix}";
            WriteFrame(text);
            if (writesToLocalTerminal && options.UseTaskbarProgress && XFETerminal.Capabilities.SupportsTaskbarProgress)
                writer.Write(XFETerminalSequences.SetProgress(XFETerminalProgressState.Indeterminate));
            writer.Flush();
        }
    }

    /// <summary>
    /// 生成一帧进度条文本而不输出。
    /// </summary>
    /// <param name="value">0.0 到 1.0 的进度比例。</param>
    /// <param name="message">可选状态文本。</param>
    /// <returns>终端就绪的进度条文本。</returns>
    public string Render(double value, string? message = null)
    {
        if (double.IsNaN(value))
            throw new ArgumentOutOfRangeException(nameof(value));
        value = Math.Clamp(value, 0d, 1d);
        var completed = (int)Math.Round(value * options.Width, MidpointRounding.AwayFromZero);
        var completedText = new string(completedCharacter, completed);
        var remainingText = new string(remainingCharacter, options.Width - completed);
        if (modern)
        {
            completedText = new XFETerminalStyle { Foreground = options.CompletedColor }.Apply(completedText);
            remainingText = new XFETerminalStyle { Foreground = options.RemainingColor }.Apply(remainingText);
        }

        var percentage = options.ShowPercentage
            ? $" {value.ToString("P0", CultureInfo.CurrentCulture),4}"
            : string.Empty;
        return $"{options.Prefix}[{completedText}{remainingText}]{percentage}{FormatMessage(message)}{options.Suffix}";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (syncRoot)
        {
            if (disposed)
                return;
            disposed = true;
            if (writesToLocalTerminal && options.UseTaskbarProgress && XFETerminal.Capabilities.SupportsTaskbarProgress)
                writer.Write(XFETerminalSequences.SetProgress(XFETerminalProgressState.Hidden));
            if (options.WriteLineOnDispose)
                writer.WriteLine();
            writer.Flush();
        }
        GC.SuppressFinalize(this);
    }

    private static string FormatMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;
        return $" {message.Replace('\r', ' ').Replace('\n', ' ')}";
    }

    private void WriteFrame(string text)
    {
        if (modern)
        {
            writer.Write($"\r{XFETerminalSequences.EraseLine(XFETerminalEraseMode.All)}{text}");
            return;
        }

        writer.Write('\r');
        writer.Write(text);
        if (text.Length < lastPlainTextLength)
            writer.Write(new string(' ', lastPlainTextLength - text.Length));
        lastPlainTextLength = text.Length;
    }
}
