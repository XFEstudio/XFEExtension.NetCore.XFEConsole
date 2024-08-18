namespace XFEExtension.NetCore.XFEConsole;

/// <summary>
/// 使用XFE控制台输出
/// </summary>
/// <param name="consolePort">控制台端口</param>
[AttributeUsage(AttributeTargets.Method)]
public class UseXFEConsoleAttribute(int consolePort = 3280) : Attribute
{
    /// <summary>
    /// 控制台端口
    /// </summary>
    public int ConsolePort { get; set; } = consolePort;
}
