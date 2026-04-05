using XFEExtension.NetCore.XFETransform;
using System.Text.RegularExpressions;

namespace XFEExtension.NetCore.XFEConsole;

/// <summary>
/// 日志条目
/// </summary>
public class XFELogEntry
{
    private const string ResetColorCode = "\e[0m";
    private const string TimeColorCode = "\e[90m";
    private const string NumberColorCode = "\e[32m";

    /// <summary>
    /// 日志记录时间
    /// </summary>
    public DateTime Time { get; set; } = DateTime.Now;
    /// <summary>
    /// 日志级别
    /// </summary>
    public LogLevel Level { get; set; } = LogLevel.Info;
    /// <summary>
    /// 日志文本
    /// </summary>
    public string LogText { get; set; } = string.Empty;
    /// <summary>
    /// 转为日志文本（含时间）
    /// </summary>
    /// <returns></returns>
    public override string ToString() => $"{TimeToString(Time)}{LevelToString(Level)}{LogText.Replace("\n", "\\n").Replace("\r", "\\r")}";

    /// <summary>
    /// 转为日志文本（含时间）
    /// </summary>
    /// <param name="useConsoleColor">是否使用控制台颜色</param>
    /// <returns></returns>
    public string ToString(bool useConsoleColor)
    {
        var timeText = TimeToString(Time);
        var levelText = LevelToString(Level);
        var logText = LogText.Replace("\n", "\\n").Replace("\r", "\\r");

        if (!useConsoleColor)
            return $"{timeText}{levelText}{logText}";

        var coloredLogText = HighlightNumbers(logText);
        return $"{TimeColorCode}{timeText}{ResetColorCode}{LevelColorCode(Level)}{levelText}{ResetColorCode}{coloredLogText}{ResetColorCode}";
    }

    /// <summary>
    /// 转为日志文本（含时间）
    /// </summary>
    /// <returns></returns>
    public string ToString(params EscapeConverter[] converters) => $"{TimeToString(Time)}{LevelToString(Level)}{converters.Aggregate(LogText, (current, converter) => converter.Convert(current))}";

    /// <summary>
    /// 转为日志文本（含时间）
    /// </summary>
    /// <param name="useConsoleColor">是否使用控制台颜色</param>
    /// <param name="converters">转换器</param>
    /// <returns></returns>
    public string ToString(bool useConsoleColor, params EscapeConverter[] converters)
    {
        var timeText = TimeToString(Time);
        var levelText = LevelToString(Level);
        var logText = converters.Aggregate(LogText, (current, converter) => converter.Convert(current));

        if (!useConsoleColor)
            return $"{timeText}{levelText}{logText}";

        var coloredLogText = HighlightNumbers(logText);
        return $"{TimeColorCode}{timeText}{ResetColorCode}{LevelColorCode(Level)}{levelText}{ResetColorCode}{coloredLogText}{ResetColorCode}";
    }

    private static string LevelColorCode(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "\e[90m",
        LogLevel.Debug => "\e[36m",
        LogLevel.Info => "\e[96m",
        LogLevel.Warning => "\e[33m",
        LogLevel.Error => "\e[31m",
        LogLevel.Fatal => "\e[35m",
        _ => "\e[96m"
    };

    private static string HighlightNumbers(string logText) => Regex.Replace(logText, "\\d+", m => $"{NumberColorCode}{m.Value}{ResetColorCode}");

    /// <summary>
    /// 日期转字符串
    /// </summary>
    public static string TimeToString(DateTime dateTime) => $"[{dateTime:yyyy-MM-dd HH:mm:ss}]";
    /// <summary>
    /// 字符串转日期
    /// </summary>
    public static DateTime StringToTime(string dateTime) => DateTime.Parse(dateTime.Replace("[", string.Empty).Replace("]", string.Empty));
    /// <summary>
    /// 警示级转字符串
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static string LevelToString(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "[TRACE]",
        LogLevel.Debug => "[DEBUG]",
        LogLevel.Info => "[INFO]",
        LogLevel.Warning => "[WARN]",
        LogLevel.Error => "[ERROR]",
        LogLevel.Fatal => "[FATAL]",
        _ => throw new NotImplementedException()
    };
    /// <summary>
    /// 字符串转警示级别
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static LogLevel StringToLevel(string logLevel) => logLevel.Replace("[", string.Empty).Replace("]", string.Empty) switch
    {
        "TRACE" => LogLevel.Trace,
        "DEBUG" => LogLevel.Debug,
        "INFO" => LogLevel.Info,
        "WARN" => LogLevel.Warning,
        "ERROR" => LogLevel.Error,
        "FATAL" => LogLevel.Fatal,
        _ => throw new NotImplementedException()
    };
    /// <summary>
    /// 从字符串中创建
    /// </summary>
    /// <param name="logString">日志文本</param>
    /// <param name="converters">转换器</param>
    /// <returns></returns>
    public static XFELogEntry? FromString(string logString, params EscapeConverter[] converters)
    {
        var split = logString.Split(['[', ']'], StringSplitOptions.RemoveEmptyEntries);
        if (split.Length <= 2) return null;
        var logText = split[2];
        if (split.Length > 3)
        {
            var inverse = false;
            for (var i = 3; i < split.Length; i++)
            {
                logText += $"{(inverse ? ']' : '[')}{split[i]}";
                inverse = !inverse;
            }
        }
        logText = converters.Aggregate(logText, (current, converter) => converter.Inverse(current));
        return new()
        {
            Time = StringToTime(split[0]),
            Level = StringToLevel(split[1]),
            LogText = logText
        };
    }
}
