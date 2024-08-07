﻿using System.Diagnostics;
using XFEExtension.NetCore.XFETransform.ObjectInfoAnalyzer;
using XFEExtension.NetCore.XFETransform;
using XFEExtension.NetCore.XFETransform.StringConverter;

namespace XFEExtension.NetCore.XFEConsole;

/// <summary>
/// XFE控制台
/// </summary>
public class XFEConsole
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
    public static bool ShowInLocalConsole { get; set; } = false;
    /// <summary>
    /// 是否自动解析对象，而非直接输出对象的.ToString()方法
    /// </summary>
    /// <remarks>
    /// 默认为 true
    /// </remarks>
    public static bool AutoAnalyzeObject { get; set; } = true;
    /// <summary>
    /// 客户端列表
    /// </summary>
    public static List<XFEConsoleProgramClient> ClientList { get; set; } = [];
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
    public static async Task<bool> UseXFEConsole(int port, string password = "") => await UseXFEConsole($"ws://localhost:{port}/", AppDomain.CurrentDomain.FriendlyName, Guid.NewGuid().ToString(), password);
    /// <summary>
    /// 设置XFE控制台
    /// </summary>
    /// <returns></returns>
    public static void SetConsoleOutput()
    {
        Console.SetOut(new XFEConsoleTextWriter(Console.Out));
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
    public static async Task WriteObject(object? obj, string remarkName = "分析对象", bool onlyProperty = false, bool onlyPublic = true)
    {
        var objectInfo = "[color red]无法分析对象：[color white]";
        try
        {
            objectInfo = XFEConverter.GetObjectInfo(StringConverter.ColoredObjectAnalyzer, remarkName, ObjectPlace.Main, 0, obj?.GetType(), obj, onlyProperty, onlyPublic).OutPutObject();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            objectInfo += ex.StackTrace;
        }
        if (ShowInDebug)
            Debug.WriteLine(objectInfo);
        if (ShowInLocalConsole && Console.Out is XFEConsoleTextWriter)
            (Console.Out as XFEConsoleTextWriter)?.OriginalTextWriter.WriteLine(objectInfo);
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
            if (ShowInLocalConsole && Console.Out is XFEConsoleTextWriter)
                (Console.Out as XFEConsoleTextWriter)?.OriginalTextWriter.WriteLine(text);
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
            if (ShowInLocalConsole && Console.Out is XFEConsoleTextWriter)
                (Console.Out as XFEConsoleTextWriter)?.OriginalTextWriter.Write(text);
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