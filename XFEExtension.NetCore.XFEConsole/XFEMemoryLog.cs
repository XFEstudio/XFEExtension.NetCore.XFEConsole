namespace XFEExtension.NetCore.XFEConsole;

/// <summary>
/// XFE内存日志
/// </summary>
public class XFEMemoryLog : XFELog
{
    /// <summary>
    /// XFE内存日志
    /// </summary>
    public XFEMemoryLog() => Current = this;

    /// <inheritdoc/>
    public override string Export() => string.Join("\n", Logs.Select(log => log.ToString(Converters)));

    /// <inheritdoc/>
    public override string Export(DateTime startDateTime, DateTime endDateTime) => string.Join("\n", Logs.Where(log => log.Time >= startDateTime && log.Time <= endDateTime).Select(log => log.ToString(Converters)));

    /// <summary>
    /// 移除超出长度限制的日志
    /// </summary>
    private void RemoveOverflow()
    {
        if (LogTextMaximizeLength == -1)
            return;
        var currentLength = CurrentLogsTextLength;
        while (currentLength > LogTextMaximizeLength)
        {
            currentLength -= Logs[0].LogText.Length;
            Logs.RemoveAt(0);
        }
    }

    /// <inheritdoc/>
    public override void AddLog(XFELogEntry xFELogEntry)
    {
        Logs.Add(xFELogEntry);
        RemoveOverflow();
    }

    /// <inheritdoc/>
    public override string ExportOriginal() => string.Join("\n", Logs.Select(log => log.ToString()));

    /// <inheritdoc/>
    public override void Import(string logText)
    {
        var logs = ImportFromText(logText);
        Logs.AddRange(logs);
    }

    /// <inheritdoc/>
    public override void Clear() => Logs.Clear();
}
