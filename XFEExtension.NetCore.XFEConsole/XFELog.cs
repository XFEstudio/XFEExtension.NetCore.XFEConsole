using XFEExtension.NetCore.XFETransform;

namespace XFEExtension.NetCore.XFEConsole;

/// <summary>
/// XFE日志
/// </summary>
public class XFELog
{
    private bool _isHead = true;
    /// <summary>
    /// 自动添加时间信息
    /// </summary>
    public bool AutoAddTimeInfo { get; set; } = false;
    /// <summary>
    /// 使用缓存，并在WriteLine时记录日志
    /// </summary>
    public bool RecordOnlyOnWriteLine { get; set; } = true;
    /// <summary>
    /// Log日志文件长度上限
    /// </summary>
    public long LogTextMaximizeLength { get; set; } = 500000000;
    /// <summary>
    /// 当前日志文本长度
    /// </summary>
    public long CurrentLogsTextLength
    {
        get
        {
            long length = 0;
            foreach (var entry in Logs)
                length += entry.LogText.Length;
            return length;
        }
    }
    /// <summary>
    /// 日志路径
    /// </summary>
    public string LogPath { get; set; } = "Log";
    /// <summary>
    /// 当前日志
    /// </summary>
    public static XFELog? Current { get; set; }
    /// <summary>
    /// 缓存日志
    /// </summary>
    public XFELogEntry CacheLog { get; set; } = new();
    /// <summary>
    /// 日志条目
    /// </summary>
    public List<XFELogEntry> Logs { get; set; } = [];
    /// <summary>
    /// 日志特殊字符的转换器（如回车，换行等）
    /// </summary>
    public EscapeConverter[] Converters { get; set; } = [new("\n", "\\n", "n", "\\"), new("\r", "\\r", "r", "\\")];
    /// <summary>
    /// XFE日志
    /// </summary>
    public XFELog() => Current = this;
    private void RemoveOverflow()
    {
        var currentLength = CurrentLogsTextLength;
        while (currentLength > LogTextMaximizeLength)
        {
            currentLength -= Logs[0].LogText.Length;
            Logs.RemoveAt(0);
        }
    }
    /// <summary>
    /// 添加一条日志
    /// </summary>
    /// <param name="xFELogEntry">日志条目</param>
    public void AddLog(XFELogEntry xFELogEntry)
    {
        Logs.Add(xFELogEntry);
        RemoveOverflow();
    }
    /// <summary>
    /// 导出当前全部日志为文本
    /// </summary>
    /// <returns>日志文本</returns>
    public string Export() => string.Join("\n", Logs.Select(log => log.ToString(Converters)));
    /// <summary>
    /// 导出指定日期区间的日志为文本
    /// </summary>
    /// <returns>日志文本</returns>
    public string Export(DateTime startDateTime, DateTime endDateTime) => string.Join("\n", Logs.Where(log => log.Time >= startDateTime && log.Time <= endDateTime).Select(log => log.ToString(Converters)));
    /// <summary>
    /// 导出当前全部日志原文为文本
    /// </summary>
    /// <returns>日志文本</returns>
    public string ExportOriginal() => string.Join("\n", Logs.Select(log => log.ToString()));
    /// <summary>
    /// 导入日志
    /// </summary>
    /// <param name="logText">日志文本</param>
    public void Import(string logText)
    {
        foreach (var log in logText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (log is not null && XFELogEntry.FromString(log, Converters) is XFELogEntry xFELogEntry)
                AddLog(xFELogEntry);
        }
    }
    /// <summary>
    /// 清除全部日志
    /// </summary>
    public void Clear() => Logs.Clear();
    /// <summary>
    /// 添加一条日志
    /// </summary>
    /// <param name="logText">日志文本</param>
    /// <param name="dateTime">日志记录时间</param>
    /// <param name="logLevel">日志级别</param>
    /// <returns>日志条目</returns>
    public XFELogEntry AddLog(string logText, DateTime dateTime, LogLevel logLevel = LogLevel.Info)
    {
        var entry = new XFELogEntry
        {
            Time = dateTime,
            Level = logLevel,
            LogText = logText
        };
        AddLog(entry);
        return entry;
    }
    /// <summary>
    /// 添加一条日志
    /// </summary>
    /// <param name="logText">日志文本</param>
    /// <param name="logLevel">日志级别</param>
    /// <returns>日志条目</returns>
    public XFELogEntry AddLog(string logText, LogLevel logLevel = LogLevel.Info) => AddLog(logText, DateTime.Now, logLevel);
    /// <summary>
    /// 记录
    /// </summary>
    /// <param name="logText">文本</param>
    /// <param name="isHead">是否是一行的开头</param>
    /// <returns></returns>
    public XFELogEntry Write(string logText, out bool isHead)
    {
        isHead = _isHead;
        _isHead = false;
        var logLevel = isHead ? LogLevel.Info : CacheLog.Level;
        try
        {
            var split = logText.Split(['[', ']'], StringSplitOptions.RemoveEmptyEntries);
            if (split.Length > 1)
            {
                if (!TryConverterToLogLevel(split[0], out logLevel))
                    logLevel = isHead ? LogLevel.Info : CacheLog.Level;
                else
                    logText = split[1];
            }
        }
        catch { }
        if (RecordOnlyOnWriteLine)
        {
            if (isHead)
            {
                CacheLog = AddLog(logText, logLevel);
                return CacheLog;
            }
            CacheLog.Level = logLevel;
            CacheLog.LogText += logText;
            return CacheLog;
        }
        else
        {
            return AddLog(logText, logLevel);
        }
    }
    /// <summary>
    /// 记录一行
    /// </summary>
    /// <param name="logText">文本</param>
    /// <param name="isHead">是否是一行的开头</param>
    /// <returns></returns>
    public XFELogEntry WriteLine(string logText, out bool isHead)
    {
        isHead = _isHead;
        _isHead = true;
        var logLevel = isHead ? LogLevel.Info : CacheLog.Level;
        try
        {
            var split = logText.Split(['[', ']'], StringSplitOptions.RemoveEmptyEntries);
            if (split.Length > 1)
            {
                if (!TryConverterToLogLevel(split[0], out logLevel))
                    logLevel = isHead ? LogLevel.Info : CacheLog.Level;
                else
                    logText = split[1];
            }
        }
        catch { }
        if (RecordOnlyOnWriteLine)
        {
            if (isHead)
                return AddLog(logText, logLevel);
            CacheLog.Level = logLevel;
            CacheLog.LogText += logText;
            return CacheLog;
        }
        else
        {
            return AddLog(logText, logLevel);
        }
    }
    /// <summary>
    /// 尝试转换为警示等级
    /// </summary>
    /// <param name="logLevelText">警示等级文本</param>
    /// <param name="logLevel">警示等级</param>
    /// <returns>是否成功</returns>
    public static bool TryConverterToLogLevel(string logLevelText, out LogLevel logLevel)
    {
        logLevel = LogLevel.Info;
        switch (logLevelText)
        {
            case "TRACE":
                logLevel = LogLevel.Trace;
                return true;
            case "DEBUG":
                logLevel = LogLevel.Debug;
                return true;
            case "INFO":
                logLevel = LogLevel.Info;
                return true;
            case "WARN":
                logLevel = LogLevel.Warning;
                return true;
            case "ERROR":
                logLevel = LogLevel.Error;
                return true;
            case "FATAL":
                logLevel = LogLevel.Fatal;
                return true;
            default:
                return false;
        }
    }
}
