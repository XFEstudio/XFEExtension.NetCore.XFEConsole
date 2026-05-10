# XFEExtension.NetCore.XFEConsole

[![NuGet](https://img.shields.io/nuget/v/XFEExtension.NetCore.XFEConsole?label=NuGet&logo=NuGet)](https://www.nuget.org/packages/XFEExtension.NetCore.XFEConsole/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/XFEExtension.NetCore.XFEConsole?label=Downloads&logo=NuGet)](https://www.nuget.org/packages/XFEExtension.NetCore.XFEConsole/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download)

> 🌐 [English](https://github.com/XFEstudio/XFEExtension.NetCore.XFEConsole/blob/master/README.md) | 中文

## 简述

XFEExtension.NetCore.XFEConsole 是一个可以允许用户进行远程输出的调试辅助工具，需要配合 XFE 工具箱来使用，当然也可以根据本 DLL 内的架构搭建一个自定义调试工具。

## 安装

```shell
dotnet add package XFEExtension.NetCore.XFEConsole
```

---

## 远程控制台

### 连接至远程控制台

```csharp
// 连接至本地端口为 3280 的控制台调试终端（port 和 password 均为可选参数）
bool connected = await XFEConsole.UseXFEConsole(port: 3280, password: "");

// 连接至指定 IP 地址的控制台调试终端
bool connected = await XFEConsole.UseXFEConsole("ws://192.168.1.100:3280/", "MyApp", Guid.NewGuid().ToString(), "password");
```

连接后，所有 `Console.WriteLine` 和 `Console.Write` 的输出都会被自动转发到远程控制台。

### 属性配置

```csharp
XFEConsole.ShowInDebug = true;          // 是否在本地调试的 Debug 中展示，默认为 true
XFEConsole.UseConsoleColor = true;      // 是否使用控制台颜色，默认为 true
XFEConsole.ShowInLocalConsole = true;   // 是否在本地控制台中显示，默认为 true
XFEConsole.AutoAnalyzeObject = true;    // 是否自动解析对象，默认为 true
```

### 停止 XFE 控制台

```csharp
await XFEConsole.StopXFEConsole();      // 关闭所有远程连接并恢复原始控制台输出流
```

### 仅连接（不修改输出流）

```csharp
// 仅建立连接，不修改 Console 的输出流
bool connected = await XFEConsole.ConnectConsole("ws://localhost:3280/", "MyApp", Guid.NewGuid().ToString(), "");
```

### 直接输出方法

```csharp
XFEConsole.WriteLine("Hello World!");              // 同步输出一行
XFEConsole.Write("Hello ");                        // 同步输出（不换行）
await XFEConsole.WriteLineAsync("Hello World!");   // 异步输出一行
await XFEConsole.WriteAsync("Hello ");             // 异步输出（不换行）
```

### 输出对象信息

```csharp
await XFEConsole.WriteObject(myObject);                                         // 输出对象信息
await XFEConsole.WriteObject(myObject, onlyProperty: true, onlyPublic: true);  // 仅输出公共属性
await XFEConsole.WriteObject(myObject, remarkName: "用户对象");                  // 自定义对象注释名称
```

### 配合 XUnit 测试框架

```csharp
class Program
{
    [UseXFEConsole]          // 使用默认端口 3280
    [UseXFEConsole(3280)]    // 或显式指定端口
    [SMTest]
    static void TestMethod()
    {
        Console.WriteLine("使用 XUnit 框架输出");
    }
}
```

---

## 日志

### 开启日志记录

```csharp
// 使用默认配置开启日志（文件日志，自动记录时间）
XFEConsole.UseXFEConsoleLog();

// 使用 Action 构造器配置选项
XFEConsole.UseXFEConsoleLog(options =>
{
    options.LogType = LogType.MemoryLog;       // 使用内存日志（默认为文件日志）
    options.AutoApplyTimeInfo = true;          // 自动添加时间信息（默认为 true）
    options.UseAnsiConsoleEncoding = true;     // 启用 ANSI 编码（默认为 true）
    options.LogTextMaximizeLength = 1024 * 10; // 日志最大长度，-1 表示不限制（默认为 -1）
});

// 或直接传入选项对象
var logOptions = new XFEConsoleLogOptions
{
    LogType = LogType.FileLog,
    AutoApplyTimeInfo = true
};
XFEConsole.UseXFEConsoleLog(logOptions);
```

### 记录日志

```csharp
Console.WriteLine("Hello World!");      // 直接在日志中记录 Hello World!

Console.Write("Hello");                 // 缓存，等待下一个 WriteLine
Console.WriteLine(" World!");           // 此时才会记录完整的 Hello World!

Console.WriteLine("[DEBUG]This is a debug message");                // 记录级别为 DEBUG
Console.WriteLine("[INFO]This is an info message");                 // 记录级别为 INFO
Console.WriteLine("[TRACE]Throw at Main() on line:24 position:25"); // 记录级别为 TRACE
Console.WriteLine("[WARN]Low memory warning");                      // 记录级别为 WARN
Console.WriteLine("[ERROR]Exception thrown");                       // 记录级别为 ERROR
Console.WriteLine("[FATAL]Application crashed... unknown reason");  // 记录级别为 FATAL
```

### 配置日志路径

```csharp
XFEConsole.Log.LogPath = "my-app.log";  // 设置日志文件路径（仅文件日志有效）
```

### 导出、导入与清除日志

```csharp
string logText = XFEConsole.Log.Export();                           // 导出全部日志为文本
string rangeLog = XFEConsole.Log.Export(DateTime.Today, DateTime.Now); // 导出指定时间范围的日志
string original  = XFEConsole.Log.ExportOriginal();                 // 导出原始日志（不含转义）

XFEConsole.Log.Import(logText);                                     // 导入日志文本
XFEConsole.Log.Clear();                                             // 清除全部日志
```
