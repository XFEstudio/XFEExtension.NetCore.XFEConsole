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

## Windows Terminal 与交互式终端

这些 API 位于 `XFEExtension.NetCore.XFEConsole.Terminal` 命名空间。库会区分 Windows Terminal、传统 Windows 控制台、其他 VT 终端和重定向输出；现代功能不可用时会降级或安全忽略。

### 能力检测

```csharp
using XFEExtension.NetCore.XFEConsole.Terminal;

XFETerminalCapabilities terminal = XFETerminal.Capabilities;
Console.WriteLine(terminal.Kind);                    // WindowsTerminal / LegacyWindowsConsole / ...
Console.WriteLine(terminal.SupportsTrueColor);
Console.WriteLine(terminal.SupportsTaskbarProgress);

// 环境变化后可重新检测；Windows 下会尝试启用 ENABLE_VIRTUAL_TERMINAL_PROCESSING
terminal = XFETerminal.RefreshCapabilities();
```

### Windows Terminal 标签页与任务栏进度

```csharp
// Windows Terminal 1.6+：标签页显示进度圆环，Windows 任务栏显示进度条
XFEConsole.SetTerminalProgress(XFETerminalProgressState.Normal, 50);
XFEConsole.SetTerminalProgress(XFETerminalProgressState.Warning, 75);
XFEConsole.SetTerminalProgress(XFETerminalProgressState.Error, 90);
XFEConsole.SetTerminalProgress(XFETerminalProgressState.Indeterminate);
XFETerminal.ClearTaskbarProgress();

// 同时显示行内进度和 Windows Terminal 任务栏进度
using var progress = XFEConsole.CreateTerminalProgressBar(new XFETerminalProgressBarOptions
{
    Width = 36,
    Prefix = "正在构建 ",
    CompletedColor = XFETerminalColor.FromRgb(53, 199, 89),
    UseTaskbarProgress = true
});

for (var i = 0; i <= 100; i++)
    progress.Report(i / 100d, $"{i}/100");
```

传统控制台仍会显示无 ANSI 污染的行内进度；Windows Terminal 专有的任务栏进度会安全忽略。

### 光标、屏幕、颜色与 Windows Terminal 集成

`XFETerminalSequences` 可以单独生成字符串，`XFETerminal.WriteRaw` 可以直接发送序列：

```csharp
var style = new XFETerminalStyle
{
    Foreground = XFETerminalColor.FromRgb(70, 190, 255),
    Background = XFETerminalColor.FromIndex(236),
    Bold = true,
    Underline = true
};

XFETerminal.WriteRaw(
    XFETerminalSequences.CursorPosition(5, 10) +
    style.Apply("24 位真彩色"));

XFETerminal.SetTitle("构建中");
XFETerminal.WriteHyperlink("项目主页", new Uri("https://github.com/XFEstudio"));
XFETerminal.SetWorkingDirectory(Environment.CurrentDirectory); // Windows Terminal 复制标签页时沿用目录
```

已提供的序列覆盖：相对/绝对光标移动、保存/恢复光标、光标显示与形状、清屏/清行、字符和行插入/删除、滚动、滚动区域、自动换行、备用屏幕、16/256/24 位颜色、调色板、OSC 8 超链接、OSC 9;4 进度、OSC 9;9 当前目录、OSC 133 命令标记、括号粘贴、焦点报告、SGR 鼠标跟踪、设备/光标查询、软重置，以及显式的 OSC 52 剪贴板序列生成。

### 全屏画布和小游戏循环

画布坐标从 0 开始，现代终端采用差量 VT 刷新，传统 Windows 控制台使用光标与颜色 API 降级。`XFETerminalSession` 会在异常、取消或正常退出时恢复备用屏幕、光标、换行和文本样式。

```csharp
await XFETerminalGame.RunAsync((game, cancellationToken) =>
{
    if (game.IsKeyPressed(ConsoleKey.LeftArrow))  playerX--;
    if (game.IsKeyPressed(ConsoleKey.RightArrow)) playerX++;

    foreach (var mouse in game.InputEvents.OfType<XFETerminalMouseEvent>())
    {
        if (mouse.Action == XFEMouseAction.ButtonPressed)
            (playerX, playerY) = (mouse.X, mouse.Y);
    }

    game.Canvas.Clear();
    game.Canvas.DrawBox(0, 0, game.Canvas.Width, game.Canvas.Height,
        XFETerminalBoxStyle.Rounded,
        new XFETerminalStyle { Foreground = XFETerminalColor.Cyan });
    game.Canvas.Set(playerX, playerY, '@',
        new XFETerminalStyle { Foreground = XFETerminalColor.Red, Bold = true });

    return ValueTask.CompletedTask;
}, new XFETerminalGameOptions
{
    FramesPerSecond = 30,
    CaptureMouse = true,
    ExitKey = ConsoleKey.Escape
});
```

需要自行控制循环时，可直接组合 `XFETerminalSession`、`XFETerminalCanvas` 和 `XFETerminalInputReader`。输入读取器提供键盘按下/释放、修饰键、鼠标按键/移动/双击/滚轮和窗口尺寸事件。

### 彩色标题艺术字

内置 `Block`、`Compact`、`Dots`、`Outline`、`Shadow`、`Slant` 和 `Framed` 七种样式，以及 `Cyan`、`Rainbow`、`Ocean`、`Sunset`、`Forest`、`Fire`、`Neon` 配色。点阵字体支持 A-Z、0-9 和常用符号；中文、Emoji 等字符会自动使用保留原文字的边框样式。

```csharp
// 生成字符串，开发者自行存储或输出
string plain = XFETerminalTitleArt.GeneratePlain(
    "XFE",
    XFETerminalArtStyle.Shadow,
    XFETerminalCompatibility.Legacy);

string terminalReady = XFEConsole.GenerateTitleArt("XFE", new XFETerminalTitleArtOptions
{
    Style = XFETerminalArtStyle.Outline,
    Palette = XFETerminalArtPalette.Rainbow,
    Compatibility = XFETerminalCompatibility.Modern
});

// 直接显示；Auto 自动区分新旧终端
XFEConsole.WriteTitleArt("终端艺术字", new XFETerminalTitleArtOptions
{
    Style = XFETerminalArtStyle.Framed,
    Palette = XFETerminalArtPalette.Ocean,
    Compatibility = XFETerminalCompatibility.Auto
});
```

现代模式使用 Unicode 绘图字符和 ANSI 真彩色；传统模式返回纯 ASCII 字符串，直接显示时通过 `ConsoleColor` 着色，不会把转义字符打印到旧终端。

示例项目提供 `self-test`、`art`、`progress` 和 `game` 四个入口：

```shell
dotnet run --project XFEExtension.NetCore.XFEConsole.Test -- self-test
dotnet run --project XFEExtension.NetCore.XFEConsole.Test -- game
```

相关协议参考：[Windows Console VT 序列](https://learn.microsoft.com/windows/console/console-virtual-terminal-sequences)、[Windows Terminal 进度](https://learn.microsoft.com/windows/terminal/tutorials/progress-bar-sequences)、[Windows Terminal Shell Integration](https://learn.microsoft.com/windows/terminal/tutorials/shell-integration)。

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
