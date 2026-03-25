
using System.Text;

namespace XFEExtension.NetCore.XFEConsole;

/// <summary>
/// XFE文件日志
/// </summary>
public class XFEFileLog : XFELog
{
    /// <summary>
    /// 当前日志文件流
    /// </summary>
    public FileStream? CurrentLogFileStream { get; set; }

    /// <inheritdoc/>
    public override List<XFELogEntry> Logs
    {
        get
        {
            return ImportFromText(Export());
        }
        protected set => base.Logs = value;
    }

    /// <inheritdoc/>
    public override string LogPath
    {
        get => base.LogPath;
        set
        {
            base.LogPath = value;
            CurrentLogFileStream?.Dispose();
            CurrentLogFileStream = null;
        }
    }

    /// <summary>
    /// XFE文件日志
    /// </summary>
    public XFEFileLog() => Current = this;

    /// <inheritdoc/>
    public override void AddLog(XFELogEntry xFELogEntry)
    {
        if (CurrentLogFileStream is null)
        {
            CurrentLogFileStream = new FileStream(LogPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            CurrentLogFileStream.Seek(0, SeekOrigin.End);
        }
        CurrentLogFileStream.Write(Encoding.UTF8.GetBytes(xFELogEntry.ToString(Converters) + '\n'));
        CurrentLogFileStream.Flush();
    }

    /// <inheritdoc/>
    public override string Export() => new StreamReader(new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)).ReadToEnd();

    /// <inheritdoc/>
    public override string Export(DateTime startDateTime, DateTime endDateTime)
    {
        var allLogs = Export();
        var logs = ImportFromText(allLogs);
        var filteredLogs = logs.Where(log => log.Time >= startDateTime && log.Time <= endDateTime);
        return string.Join("\n", filteredLogs.Select(log => log.ToString(Converters)));
    }

    /// <inheritdoc/>
    public override string ExportOriginal() => string.Join("\n", Logs.Select(log => log.ToString()));

    /// <inheritdoc/>
    public override void Import(string logText)
    {
        CurrentLogFileStream ??= new FileStream(LogPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        CurrentLogFileStream.Write(Encoding.UTF8.GetBytes(logText + '\n'));
    }

    /// <inheritdoc/>
    public override void Clear()
    {
        CurrentLogFileStream ??= new FileStream(LogPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        CurrentLogFileStream.SetLength(0);
        CurrentLogFileStream.Flush();
    }
}
