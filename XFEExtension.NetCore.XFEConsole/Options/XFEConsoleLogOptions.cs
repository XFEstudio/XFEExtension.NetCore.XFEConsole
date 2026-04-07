using XFEExtension.NetCore.XFEConsole.Models;

namespace XFEExtension.NetCore.XFEConsole.Options;

/// <summary>
/// XFE控制台日志选项
/// </summary>
public class XFEConsoleLogOptions
{
    /// <summary>
    /// 是否启用日志输出（默认值为 true）。如果启用，日志将被输出到控制台或文件中，具体取决于 LogType 的设置。
    /// </summary>
    public bool EnableLogging { get; set; } = true;
    /// <summary>
    /// 是否自动应用时间信息（默认值为 true）。如果启用，日志将自动包含时间戳信息。
    /// </summary>
    public bool AutoApplyTimeInfo { get; set; } = true;
    /// <summary>
    /// 是否使用 ANSI 控制台编码（默认值为 true）。如果启用，日志输出将使用 ANSI 编码，这对于在控制台中正确显示特殊字符和颜色非常重要。
    /// </summary>
    public bool UseAnsiConsoleEncoding { get; set; } = true;
    /// <summary>
    /// 日志文本最大长度，超过该长度的日志文本将被截断（默认值为 -1）。
    /// </summary>
    public int LogTextMaximizeLength { get; set; } = -1;
    /// <summary>
    /// 日志类型（默认值为 LogType.FileLog）
    /// </summary>
    public LogType LogType { get; set; } = LogType.FileLog;
}
