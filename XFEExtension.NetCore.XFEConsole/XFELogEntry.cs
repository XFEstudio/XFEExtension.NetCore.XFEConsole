using XFEExtension.NetCore.XFETransform;

namespace XFEExtension.NetCore.XFEConsole;

/// <summary>
/// 日志条目
/// </summary>
public class XFELogEntry
{
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
    /// <returns></returns>
    public string ToString(params EscapeConverter[] converters)
    {
        var logText = LogText;
        foreach (var converter in converters)
            logText = converter.Convert(logText);
        return $"{TimeToString(Time)}{LevelToString(Level)}{logText}";
    }

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
        if (split.Length > 2)
        {
            var logText = split[2];
            foreach (var converter in converters)
                logText = converter.Inverse(logText);
            return new()
            {
                Time = StringToTime(split[0]),
                Level = StringToLevel(split[1]),
                LogText = logText
            };
        }
        return null;
    }
}
