using XFEExtension.NetCore.XFETransform;

namespace XFEExtension.NetCore.XFEConsole;

/// <summary>
/// XFE日志
/// </summary>
public abstract class XFELog
{
    /// <summary>
    /// 自动添加时间信息
    /// </summary>
    public bool AutoAddTimeInfo { get; set; } = true;
    /// <summary>
    /// 使用缓存，并在WriteLine时记录日志
    /// </summary>
    public bool RecordOnlyOnWriteLine { get; set; } = true;
    /// <summary>
    /// Log日志文件长度上限
    /// </summary>
    public long LogTextMaximizeLength { get; set; } = -1;
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
    public virtual string LogPath { get; set; } = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
    /// <summary>
    /// 当前日志
    /// </summary>
    public static XFELog? Current { get; set; }
    /// <summary>
    /// 缓存日志
    /// </summary>
    public static AsyncLocal<XFELogEntry?> CacheLog { get; set; } = new();
    /// <summary>
    /// 日志条目
    /// </summary>
    public virtual List<XFELogEntry> Logs { get; protected set; } = [];
    /// <summary>
    /// 日志特殊字符的转换器（如回车，换行等）
    /// </summary>
    public EscapeConverter[] Converters { get; set; } = [new("\n", "\\n", "n", "\\"), new("\r", "\\r", "r", "\\")];

    /// <summary>
    /// 添加一条日志
    /// </summary>
    /// <param name="xFELogEntry">日志条目</param>
    public abstract void AddLog(XFELogEntry xFELogEntry);

    /// <summary>
    /// 导出当前全部日志为文本
    /// </summary>
    /// <returns>日志文本</returns>
    public abstract string Export();

    /// <summary>
    /// 导出指定日期区间的日志为文本
    /// </summary>
    /// <returns>日志文本</returns>
    public abstract string Export(DateTime startDateTime, DateTime endDateTime);

    /// <summary>
    /// 导出当前全部日志原文为文本
    /// </summary>
    /// <returns>日志文本</returns>
    public abstract string ExportOriginal();

    /// <summary>
    /// 导入日志
    /// </summary>
    /// <param name="logText">日志文本</param>
    public abstract void Import(string logText);

    /// <summary>
    /// 清除全部日志
    /// </summary>
    public abstract void Clear();

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
        isHead = CacheLog.Value is null;
        var logLevel = isHead ? LogLevel.Info : CacheLog.Value!.Level;
        try
        {
            var split = logText.Split(['[', ']'], StringSplitOptions.RemoveEmptyEntries);
            if (split.Length > 1)
            {
                if (!TryConverterToLogLevel(split[0], out logLevel))
                    logLevel = isHead ? LogLevel.Info : CacheLog.Value!.Level;
                else
                    logText = split[1];
            }
        }
        catch { }
        if (RecordOnlyOnWriteLine)
        {
            if (isHead)
            {
                var log = new XFELogEntry
                {
                    Time = DateTime.Now,
                    Level = logLevel,
                    LogText = logText
                };
                CacheLog.Value = log;
                return log;
            }
            CacheLog.Value!.Level = logLevel;
            CacheLog.Value!.LogText += logText;
            return CacheLog.Value;
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
        isHead = CacheLog.Value is null;
        var logLevel = isHead ? LogLevel.Info : CacheLog.Value!.Level;
        try
        {
            var split = logText.Split(['[', ']'], StringSplitOptions.RemoveEmptyEntries);
            if (split.Length > 1)
            {
                if (!TryConverterToLogLevel(split[0], out logLevel))
                {
                    logLevel = isHead ? LogLevel.Info : CacheLog.Value!.Level;
                }
                else
                {
                    logText = split[1];
                    if (split.Length > 2)
                    {
                        var inverse = false;
                        for (var i = 2; i < split.Length; i++)
                        {
                            logText += $"{(inverse ? ']' : '[')}{split[i]}";
                            inverse = !inverse;
                        }
                    }
                }
            }
        }
        catch { }

        if (!RecordOnlyOnWriteLine) return AddLog(logText, logLevel);
        if (isHead)
        {
            var log = new XFELogEntry
            {
                Time = DateTime.Now,
                Level = logLevel,
                LogText = logText
            };
            CacheLog.Value = null;
            AddLog(log);
            return log;
        }
        CacheLog.Value!.Level = logLevel;
        CacheLog.Value!.LogText += logText;
        var returnLog = CacheLog.Value;
        CacheLog.Value = null;
        AddLog(returnLog);
        return returnLog;
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

    /// <summary>
    /// 从文本导入日志
    /// </summary>
    /// <param name="logText"></param>
    /// <param name="converters"></param>
    /// <returns></returns>
    public static List<XFELogEntry> ImportFromText(string logText, params EscapeConverter[] converters)
    {
        var logs = new List<XFELogEntry>();
        if (converters.Length == 0)
            converters = [new("\n", "\\n", "n", "\\"), new("\r", "\\r", "r", "\\")];
        foreach (var log in logText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (XFELogEntry.FromString(log, converters) is { } xFELogEntry)
                logs.Add(xFELogEntry);
        }
        return logs;
    }
}
