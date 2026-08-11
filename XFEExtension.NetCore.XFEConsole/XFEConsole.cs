using System.Diagnostics;
using XFEExtension.NetCore.XFEConsole.Options;
using XFEExtension.NetCore.XFEConsole.Terminal;
using XFEExtension.NetCore.XFEConsole.Utilities.Helpers;
using XFEExtension.NetCore.XFETransform;
using XFEExtension.NetCore.XFETransform.ObjectInfoAnalyzer;
using XFEExtension.NetCore.XFETransform.StringConverter;

namespace XFEExtension.NetCore.XFEConsole;

/// <summary>
/// XFE控制台
/// </summary>
public static class XFEConsole
{
    /// <summary>
    /// 是否在本地调试的Debug中展示
    /// </summary>
    /// <remarks>
    /// 默认为 true
    /// </remarks>
    public static bool ShowInDebug { get; set; } = true;
    /// <summary>
    /// 使用控制台颜色
    /// </summary>
    /// <remarks>
    /// 默认为 true
    /// </remarks>
    public static bool UseConsoleColor { get; set; } = true;
    /// <summary>
    /// 是否在本地控制台中显示
    /// </summary>
    /// <remarks>
    /// 默认为 false
    /// </remarks>
    public static bool ShowInLocalConsole { get; set; } = true;
    /// <summary>
    /// 是否自动解析对象，而非直接输出对象的.ToString()方法
    /// </summary>
    /// <remarks>
    /// 默认为 true
    /// </remarks>
    public static bool AutoAnalyzeObject { get; set; } = true;
    /// <summary>
    /// 是否启动日志记录
    /// </summary>
    public static bool EnableLog { get; set; }
    /// <summary>
    /// 当前日志
    /// </summary>
    public static XFELog Log { get; set; } = new XFEFileLog();
    /// <summary>
    /// 客户端列表
    /// </summary>
    public static List<XFEConsoleProgramClient> ClientList { get; set; } = [];
    /// <summary>
    /// 当前控制台输出流
    /// </summary>
    public static XFEConsoleTextWriter? CurrentConsoleTextWriter { get; set; }
    /// <summary>
    /// 当前本地终端能力快照。
    /// </summary>
    public static XFETerminalCapabilities TerminalCapabilities => XFETerminal.Capabilities;
    /// <summary>
    /// 设置 Windows Terminal 标签页及任务栏进度。
    /// </summary>
    /// <param name="state">进度状态。</param>
    /// <param name="progress">0 到 100 的进度。</param>
    /// <returns>是否已发送给 Windows Terminal。</returns>
    public static bool SetTerminalProgress(XFETerminalProgressState state, int progress = 0) =>
        XFETerminal.SetTaskbarProgress(state, progress);
    /// <summary>
    /// 创建同时支持行内显示与 Windows Terminal 任务栏的进度条。
    /// </summary>
    /// <param name="options">进度条选项。</param>
    /// <param name="writer">可选输出目标。</param>
    /// <returns>进度条对象。</returns>
    public static XFETerminalProgressBar CreateTerminalProgressBar(
        XFETerminalProgressBarOptions? options = null,
        TextWriter? writer = null) => XFETerminal.CreateProgressBar(options, writer);
    /// <summary>
    /// 生成可直接输出的终端标题艺术字。
    /// </summary>
    /// <param name="text">标题文本。</param>
    /// <param name="options">艺术字选项。</param>
    /// <returns>艺术字字符串。</returns>
    public static string GenerateTitleArt(string text, XFETerminalTitleArtOptions? options = null) =>
        XFETerminalTitleArt.Generate(text, options);
    /// <summary>
    /// 直接向本地终端显示标题艺术字。
    /// </summary>
    /// <param name="text">标题文本。</param>
    /// <param name="options">艺术字选项。</param>
    /// <param name="writer">可选输出目标。</param>
    public static void WriteTitleArt(
        string text,
        XFETerminalTitleArtOptions? options = null,
        TextWriter? writer = null) => XFETerminalTitleArt.Write(text, options, writer);
    /// <summary>
    /// 使用XFE控制台
    /// </summary>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="name">客户端名称</param>
    /// <param name="id">客户端ID</param>
    /// <param name="password">密码</param>
    /// <returns>是否连接成功</returns>
    public static async Task<bool> UseXFEConsole(string ipAddress, string name, string id, string password)
    {
        SetConsoleOutput();
        return await ConnectConsole(ipAddress, name, id, password);
    }
    /// <summary>
    /// 使用XFE控制台
    /// </summary>
    /// <param name="port">端口</param>
    /// <param name="password">密码</param>
    /// <returns>是否连接成功</returns>
    public static async Task<bool> UseXFEConsole(int port = 3280, string password = "") => await UseXFEConsole($"ws://localhost:{port}/", AppDomain.CurrentDomain.FriendlyName, Guid.NewGuid().ToString(), password);
    /// <summary>
    /// 使用XFE控制台日志
    /// </summary>
    public static void UseXFEConsoleLog(XFEConsoleLogOptions? xFEConsoleLogOptions = null)
    {
        EnableLog = true;
        xFEConsoleLogOptions ??= new XFEConsoleLogOptions();
        switch (xFEConsoleLogOptions.LogType)
        {
            case Models.LogType.MemoryLog:
                Log = new XFEMemoryLog();
                break;
            case Models.LogType.FileLog:
            default:
                break;
        }
        Log.AutoAddTimeInfo = xFEConsoleLogOptions.AutoApplyTimeInfo;
        Log.LogTextMaximizeLength = xFEConsoleLogOptions.LogTextMaximizeLength;
        if (xFEConsoleLogOptions.UseAnsiConsoleEncoding)
            ConsoleAnsi.Enable();
        SetConsoleOutput();
    }
    /// <summary>
    /// 使用XFE控制台日志
    /// </summary>
    /// <param name="optionBuilder"></param>
    public static void UseXFEConsoleLog(Action<XFEConsoleLogOptions> optionBuilder)
    {
        var options = new XFEConsoleLogOptions();
        optionBuilder(options);
        UseXFEConsoleLog(options);
    }
    /// <summary>
    /// 停止XFE控制台
    /// </summary>
    /// <returns></returns>
    public static async Task StopXFEConsole()
    {
        if (CurrentConsoleTextWriter is not null)
            Console.SetOut(CurrentConsoleTextWriter.OriginalTextWriter);
        foreach (var client in ClientList)
            await client.Client.CloseCyberCommClient();
    }
    /// <summary>
    /// 设置XFE控制台
    /// </summary>
    /// <returns></returns>
    public static void SetConsoleOutput()
    {
        CurrentConsoleTextWriter = new(Console.Out);
        Console.SetOut(CurrentConsoleTextWriter);
    }
    /// <summary>
    /// 连接XFE控制台
    /// </summary>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="name">客户端名称</param>
    /// <param name="id">客户端ID</param>
    /// <param name="password">密码</param>
    /// <returns>是否连接成功</returns>
    public static async Task<bool> ConnectConsole(string ipAddress, string name, string id, string password)
    {
        var client = new XFEConsoleProgramClient(ipAddress, name, id, password);
        if (await client.Connect())
        {
            ClientList.Add(client);
            return true;
        }

        return false;
    }
    /// <summary>
    /// 输出对象信息
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="remarkName">对象注释</param>
    /// <param name="onlyProperty">仅解析属性</param>
    /// <param name="onlyPublic">仅解析公共属性或字段</param>
    /// <returns></returns>
    public static async Task WriteObject(object? obj, bool onlyProperty = false, bool onlyPublic = true, string remarkName = "分析对象")
    {
        string? objectInfo;
        try
        {
            objectInfo = obj is null ? $"[foldblock color: white #9898e7 title: 分析对象：{obj?.GetType().Name ?? "空对象"} text: 对象内容为空]" : $"[foldblock color: white #9898e7 title: 分析对象：{obj.GetType().Name} text: {XFEConverter.GetObjectInfo(StringConverter.ColoredObjectAnalyzer, remarkName, ObjectPlace.Main, 0, [obj], obj.GetType(), obj, onlyProperty, onlyPublic).OutPutObject()}]";
        }
        catch (Exception ex)
        {
            objectInfo = $"[foldblock color: white #ff0000 title: 错误：{ex.Message} text: {ex}]";
        }
        if (ShowInDebug)
            Debug.WriteLine(objectInfo);
        if (ShowInLocalConsole)
            await (CurrentConsoleTextWriter?.OriginalTextWriter.WriteLineAsync(objectInfo) ?? Task.CompletedTask);
        foreach (var client in ClientList)
            await client.OutputMessage(objectInfo, true);
    }
    /// <summary>
    /// 向已连接的XFE控制台输出一条消息
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns></returns>
    public static void WriteLine(string? text)
    {
        if (text is null) return;
        if (ShowInDebug)
            Debug.WriteLine(text);
        if (EnableLog)
        {
            var log = Log.WriteLine(text, out _);
            if (ShowInLocalConsole)
                CurrentConsoleTextWriter?.OriginalTextWriter.WriteLine(log.ToString(UseConsoleColor));
        }
        else if (ShowInLocalConsole)
        {
            CurrentConsoleTextWriter?.OriginalTextWriter.WriteLine(text);
        }
        foreach (var client in ClientList)
            client.OutputMessage($"[color {ConvertConsoleColorToString(Console.ForegroundColor)} {ConvertConsoleColorToString(Console.BackgroundColor)}]{text}", true).Wait();
    }
    /// <summary>
    /// 向已连接的XFE控制台输出一条消息
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns></returns>
    public static void Write(string? text)
    {
        if (text is null) return;
        if (ShowInDebug)
            Debug.WriteLine(text);
        if (EnableLog)
        {
            var log = Log.Write(text, out var isHead);
            if (ShowInLocalConsole && !Log.RecordOnlyOnWriteLine)
                CurrentConsoleTextWriter?.OriginalTextWriter.Write($"{(Log.AutoAddTimeInfo && isHead ? XFELogEntry.TimeToString(log.Time) : string.Empty)}{text}");
        }
        else if (ShowInLocalConsole)
        {
            CurrentConsoleTextWriter?.OriginalTextWriter.Write(text);
        }
        foreach (var client in ClientList)
            client.OutputMessage($"[color {ConvertConsoleColorToString(Console.ForegroundColor)} {ConvertConsoleColorToString(Console.BackgroundColor)}]{text}", false).Wait();
    }
    /// <summary>
    /// 向已连接的XFE控制台输出一条消息
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns></returns>
    public static async Task WriteLineAsync(string? text)
    {
        if (text is null) return;
        if (ShowInDebug)
            Debug.WriteLine(text);
        if (EnableLog)
        {
            var log = Log.WriteLine(text, out _);
            if (ShowInLocalConsole)
                await (CurrentConsoleTextWriter?.OriginalTextWriter.WriteLineAsync(log.ToString(UseConsoleColor)) ?? Task.CompletedTask);
        }
        else if (ShowInLocalConsole)
        {
            await (CurrentConsoleTextWriter?.OriginalTextWriter.WriteLineAsync(text) ?? Task.CompletedTask);
        }
        foreach (var client in ClientList)
            await client.OutputMessage($"[color {ConvertConsoleColorToString(Console.ForegroundColor)} {ConvertConsoleColorToString(Console.BackgroundColor)}]{text}", true);
    }
    /// <summary>
    /// 向已连接的XFE控制台输出一条消息
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns></returns>
    public static async Task WriteAsync(string? text)
    {
        if (text is not null)
        {
            if (ShowInDebug)
                Debug.WriteLine(text);
            if (EnableLog)
            {
                var log = Log.Write(text, out var isHead);
                if (ShowInLocalConsole && !Log.RecordOnlyOnWriteLine)
                    await (CurrentConsoleTextWriter?.OriginalTextWriter.WriteAsync($"{(Log.AutoAddTimeInfo && isHead ? XFELogEntry.TimeToString(log.Time) : string.Empty)}{text}") ?? Task.CompletedTask);
            }
            else if (ShowInLocalConsole)
            {
                await (CurrentConsoleTextWriter?.OriginalTextWriter.WriteAsync(text) ?? Task.CompletedTask);
            }
            foreach (var client in ClientList)
                await client.OutputMessage($"[color {ConvertConsoleColorToString(Console.ForegroundColor)} {ConvertConsoleColorToString(Console.BackgroundColor)}]{text}", false);
        }
    }
    /// <summary>
    /// 将控制台颜色转为颜色代码
    /// </summary>
    /// <param name="consoleColor">控制台颜色</param>
    /// <returns>颜色代码</returns>
    public static string ConvertConsoleColorToString(ConsoleColor consoleColor) => consoleColor switch
    {
        ConsoleColor.Black => "black",
        ConsoleColor.DarkBlue => "#0037da",
        ConsoleColor.DarkGreen => "#13a10e",
        ConsoleColor.DarkCyan => "#3a96dd",
        ConsoleColor.DarkRed => "#c50f1f",
        ConsoleColor.DarkMagenta => "#881798",
        ConsoleColor.DarkYellow => "#c19c00",
        ConsoleColor.Gray => "#cccccc",
        ConsoleColor.DarkGray => "#767676",
        ConsoleColor.Blue => "#0037da",
        ConsoleColor.Green => "#16c60c",
        ConsoleColor.Cyan => "#61d6d6",
        ConsoleColor.Red => "#e74856",
        ConsoleColor.Magenta => "#b4009e",
        ConsoleColor.Yellow => "#f9f1a5",
        ConsoleColor.White => "white",
        _ => "Transparent",
    };
}
