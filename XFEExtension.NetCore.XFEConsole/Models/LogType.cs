namespace XFEExtension.NetCore.XFEConsole.Models;

/// <summary>
/// 日志类型
/// </summary>
public enum LogType
{
    /// <summary>
    /// 内存日志，日志内容保存在内存中，适合短期使用和调试，程序结束后日志将丢失
    /// </summary>
    MemoryLog = 0,
    /// <summary>
    /// 文件日志，日志内容保存在文件中，适合长期保存和分析，程序结束后日志不会丢失
    /// </summary>
    FileLog = 1
}
