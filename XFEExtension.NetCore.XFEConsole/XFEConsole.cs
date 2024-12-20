﻿using System.Diagnostics;
using XFEExtension.NetCore.XFETransform.ObjectInfoAnalyzer;
using XFEExtension.NetCore.XFETransform;
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
    public static bool EnableLog { get; set; } = false;
    /// <summary>
    /// 当前日志
    /// </summary>
    public static XFELog Log { get; set; } = new();
    /// <summary>
    /// 客户端列表
    /// </summary>
    public static List<XFEConsoleProgramClient> ClientList { get; set; } = [];
    /// <summary>
    /// 当前控制台输出流
    /// </summary>
    public static XFEConsoleTextWriter? CurrentConsoleTextWriter { get; set; }
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
    public static void UseXFEConsoleLog(bool autoAddTimeInfo = true, long logTextMaximizeLength = 500000000)
    {
        EnableLog = true;
        Log.AutoAddTimeInfo = autoAddTimeInfo;
        Log.LogTextMaximizeLength = logTextMaximizeLength;
        SetConsoleOutput();
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
        else
        {
            return false;
        }
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
            objectInfo = $"[foldblock color: white #9898e7 title: 分析对象：{obj?.GetType().Name} text: {XFEConverter.GetObjectInfo(StringConverter.ColoredObjectAnalyzer, remarkName, ObjectPlace.Main, 0, [obj], obj?.GetType(), obj, onlyProperty, onlyPublic).OutPutObject()}]";
        }
        catch (Exception ex)
        {
            objectInfo = $"[foldblock color: white #ff0000 title: 错误：{ex.Message} text: {ex}]";
        }
        if (ShowInDebug)
            Debug.WriteLine(objectInfo);
        if (ShowInLocalConsole)
            CurrentConsoleTextWriter?.OriginalTextWriter.WriteLine(objectInfo);
        foreach (var client in ClientList)
            await client.OutputMessage(objectInfo, true);
    }
    /// <summary>
    /// 向已连接的XFE控制台输出一条消息
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns></returns>
    public static async Task WriteLine(string? text)
    {
        if (text is not null)
        {
            if (ShowInDebug)
                Debug.WriteLine(text);
            if (EnableLog)
            {
                var log = Log.WriteLine(text, out var isHead);
                if (ShowInLocalConsole)
                    CurrentConsoleTextWriter?.OriginalTextWriter.WriteLine($"{(Log.AutoAddTimeInfo && isHead ? XFELogEntry.TimeToString(log.Time) : string.Empty)}{text}");
            }
            else if (ShowInLocalConsole)
            {
                CurrentConsoleTextWriter?.OriginalTextWriter.WriteLine(text);
            }
            foreach (var client in ClientList)
                await client.OutputMessage($"[color {ConvertConsoleColorToString(Console.ForegroundColor)} {ConvertConsoleColorToString(Console.BackgroundColor)}]{text}", true);
        }
    }
    /// <summary>
    /// 向已连接的XFE控制台输出一条消息
    /// </summary>
    /// <param name="text">文本</param>
    /// <returns></returns>
    public static async Task Write(string? text)
    {
        if (text is not null)
        {
            if (ShowInDebug)
                Debug.WriteLine(text);
            if (EnableLog)
            {
                var log = Log.Write(text, out var isHead);
                if (ShowInLocalConsole)
                    CurrentConsoleTextWriter?.OriginalTextWriter.Write($"{(Log.AutoAddTimeInfo && isHead ? XFELogEntry.TimeToString(log.Time) : string.Empty)}{text}");
            }
            else if (ShowInLocalConsole)
            {
                CurrentConsoleTextWriter?.OriginalTextWriter.Write(text);
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
