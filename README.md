# XFEExtension.NetCore.XFEConsole

[![NuGet](https://img.shields.io/nuget/v/XFEExtension.NetCore.XFEConsole?label=NuGet&logo=NuGet)](https://www.nuget.org/packages/XFEExtension.NetCore.XFEConsole/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/XFEExtension.NetCore.XFEConsole?label=Downloads&logo=NuGet)](https://www.nuget.org/packages/XFEExtension.NetCore.XFEConsole/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download)

> 🌐 English | [简体中文](https://github.com/XFEstudio/XFEExtension.NetCore.XFEConsole/blob/master/README_zh.md)

## Overview

XFEExtension.NetCore.XFEConsole is a debugging aid that allows remote console output. It is designed to work with the XFE Toolbox, but you can also build your own debugging tool based on the architecture provided in this library.

## Installation

```shell
dotnet add package XFEExtension.NetCore.XFEConsole
```

---

## Windows Terminal and Interactive Terminal APIs

These APIs live in `XFEExtension.NetCore.XFEConsole.Terminal`. The library distinguishes Windows Terminal, the legacy Windows console, other VT terminals, and redirected output. Modern-only operations degrade safely when unavailable.

### Capability detection

```csharp
using XFEExtension.NetCore.XFEConsole.Terminal;

XFETerminalCapabilities terminal = XFETerminal.Capabilities;
Console.WriteLine(terminal.Kind);
Console.WriteLine(terminal.SupportsTrueColor);
Console.WriteLine(terminal.SupportsTaskbarProgress);

terminal = XFETerminal.RefreshCapabilities(); // Also tries to enable Windows VT processing
```

### Windows Terminal tab and taskbar progress

```csharp
XFEConsole.SetTerminalProgress(XFETerminalProgressState.Normal, 50);
XFEConsole.SetTerminalProgress(XFETerminalProgressState.Warning, 75);
XFEConsole.SetTerminalProgress(XFETerminalProgressState.Indeterminate);
XFETerminal.ClearTaskbarProgress();

using var progress = XFEConsole.CreateTerminalProgressBar(new XFETerminalProgressBarOptions
{
    Width = 36,
    Prefix = "Building ",
    UseTaskbarProgress = true
});
progress.Report(0.5, "step 50/100");
```

The inline progress bar remains usable in legacy terminals. OSC 9;4 taskbar progress is emitted only in Windows Terminal.

### VT controls and Windows Terminal integration

`XFETerminalSequences` builds reusable strings, while `XFETerminal.WriteRaw` sends them to the local terminal:

```csharp
var style = new XFETerminalStyle
{
    Foreground = XFETerminalColor.FromRgb(70, 190, 255),
    Background = XFETerminalColor.FromIndex(236),
    Bold = true
};

XFETerminal.WriteRaw(
    XFETerminalSequences.CursorPosition(5, 10) +
    style.Apply("24-bit color"));

XFETerminal.SetTitle("Building");
XFETerminal.WriteHyperlink("Project", new Uri("https://github.com/XFEstudio"));
XFETerminal.SetWorkingDirectory(Environment.CurrentDirectory);
```

The sequence API covers relative/absolute cursor movement, save/restore, cursor visibility and shape, erase operations, character/line insertion and deletion, scrolling and margins, wrapping, alternate screen, 16/256/24-bit colors, palette updates, OSC 8 hyperlinks, OSC 9;4 progress, OSC 9;9 working directory, OSC 133 command marks, bracketed paste, focus and SGR mouse reporting, device/cursor queries, soft reset, and explicit OSC 52 clipboard sequence generation.

### Full-screen canvas and game loop

The canvas uses zero-based coordinates and VT differential rendering. Legacy Windows consoles fall back to the Console cursor/color APIs. Terminal state is restored on completion, cancellation, or exception.

```csharp
await XFETerminalGame.RunAsync((game, cancellationToken) =>
{
    if (game.IsKeyPressed(ConsoleKey.LeftArrow))  playerX--;
    if (game.IsKeyPressed(ConsoleKey.RightArrow)) playerX++;

    game.Canvas.Clear();
    game.Canvas.DrawBox(0, 0, game.Canvas.Width, game.Canvas.Height,
        XFETerminalBoxStyle.Rounded);
    game.Canvas.Set(playerX, playerY, '@', new XFETerminalStyle
    {
        Foreground = XFETerminalColor.Red,
        Bold = true
    });
    return ValueTask.CompletedTask;
}, new XFETerminalGameOptions
{
    FramesPerSecond = 30,
    CaptureMouse = true,
    ExitKey = ConsoleKey.Escape
});
```

For custom loops, compose `XFETerminalSession`, `XFETerminalCanvas`, and `XFETerminalInputReader`. Input events include key up/down and modifiers, mouse buttons/movement/double-click/wheels, and window resizing.

### Colored title art

The repository implements 17 fonts itself and has no third-party font-package dependency: `Pixel`, `Standard`, `Big`, `Small`, `Slant`, `AnsiShadow`, `SmallShadow`, `Doom`, `Epic`, `Gothic`, `Ivrit`, `Modular`, `Ogre`, `Rectangles`, `Relief`, `Isometric`, and `Larry3D`. They are generated from the built-in glyph matrix by independent line-connection, double-line, block, module, mirror, relief, shadow, and isometric algorithms. `AvailableFonts` returns the complete enum list.

Fonts and effects are independent and composable. `OutlineWidth` creates a real outer stroke, while `ExtrudeDepth` and the eight `ExtrudeDirection` values create layered 3D extrusion. Fill, outline, and extrusion have separate palette/color controls. `Outline` supplies a one-cell default stroke and `ThreeDimensional` supplies a three-cell down-right extrusion.

```csharp
// Plain text contains the shape and effects, but never ANSI escape sequences.
string plain = XFETerminalTitleArt.GeneratePlain("HELLO", new XFETerminalTitleArtOptions
{
    Font = XFETerminalArtFont.Epic,
    Style = XFETerminalArtStyle.Compact,
    OutlineWidth = 1,
    OutlineCharacter = '+',
    ExtrudeDepth = 2,
    ExtrudeCharacter = '#',
    Compatibility = XFETerminalCompatibility.Legacy
});

string terminalReady = XFEConsole.GenerateTitleArt("XFE", new XFETerminalTitleArtOptions
{
    Font = XFETerminalArtFont.Doom,
    Style = XFETerminalArtStyle.ThreeDimensional,
    Palette = XFETerminalArtPalette.Sunset,
    OutlineWidth = 1,
    OutlineColor = XFETerminalColor.FromRgb(0xff, 0xee, 0xc2),
    ExtrudeDepth = 4,
    ExtrudeColor = XFETerminalColor.FromRgb(0x60, 0x24, 0x60),
    ExtrudeDirection = XFETerminalArtExtrudeDirection.DownRight,
    Compatibility = XFETerminalCompatibility.Modern
});

XFEConsole.WriteTitleArt("HELLO", new XFETerminalTitleArtOptions
{
    Font = XFETerminalArtFont.Larry3D,
    Palette = XFETerminalArtPalette.Ocean,
    Compatibility = XFETerminalCompatibility.Auto
});

foreach (XFETerminalArtFont font in XFETerminalTitleArt.AvailableFonts)
    Console.WriteLine(font);
```

Modern mode uses Unicode effect characters and ANSI true color, including distinct colors within a single row. Legacy mode switches effects to ASCII and uses `ConsoleColor` when writing directly. Built-in glyphs cover Latin letters, digits, and common punctuation; unsupported CJK or emoji text falls back to a frame that preserves the original characters.

Run the feature demos and deterministic checks with:

```shell
dotnet run --project XFEExtension.NetCore.XFEConsole.Test -- self-test
dotnet run --project XFEExtension.NetCore.XFEConsole.Test -- game
```

Protocol references: [Windows Console VT sequences](https://learn.microsoft.com/windows/console/console-virtual-terminal-sequences), [Windows Terminal progress](https://learn.microsoft.com/windows/terminal/tutorials/progress-bar-sequences), and [Windows Terminal shell integration](https://learn.microsoft.com/windows/terminal/tutorials/shell-integration).

---

## Remote Console

### Connect to the Remote Console

```csharp
// Connect to a local console debug terminal on the given port (port and password are optional)
bool connected = await XFEConsole.UseXFEConsole(port: 3280, password: "");

// Connect to a remote console debug terminal at a specific IP address
bool connected = await XFEConsole.UseXFEConsole("ws://192.168.1.100:3280/", "MyApp", Guid.NewGuid().ToString(), "password");
```

Once connected, all `Console.WriteLine` and `Console.Write` output is automatically forwarded to the remote console.

### Mode 2: Let the Toolbox Connect to the Program

```csharp
// Run the debug program as the server. Remote toolboxes must use the same password.
XFEConsoleProgramServer server = await XFEConsole.UseXFEConsoleServer(
    port: 3280,
    localOnly: false,
    password: "your-password",
    programName: "MyApp");
```

Enable "Mode 2" in the toolbox console settings, enter `ws://server-address:3280/` and the same password, then start the console. Keep `localOnly` set to `true` for same-machine debugging. Across networks, use `ws` only on a trusted LAN or VPN; public deployments should place the endpoint behind a TLS reverse proxy and connect with `wss`.

### Properties

```csharp
XFEConsole.ShowInDebug = true;          // Show output in local Debug; default is true
XFEConsole.UseConsoleColor = true;      // Use console colors; default is true
XFEConsole.ShowInLocalConsole = true;   // Show output in local console; default is true
XFEConsole.AutoAnalyzeObject = true;    // Auto-analyze objects instead of calling .ToString(); default is true
```

### Stop the XFE Console

```csharp
await XFEConsole.StopXFEConsole();      // Close all remote connections and restore the original console output
```

### Connect Only (without redirecting output)

```csharp
// Establish a connection without modifying Console's output stream
bool connected = await XFEConsole.ConnectConsole("ws://localhost:3280/", "MyApp", Guid.NewGuid().ToString(), "");
```

### Direct Write Methods

```csharp
XFEConsole.WriteLine("Hello World!");              // Synchronous write line
XFEConsole.Write("Hello ");                        // Synchronous write (no newline)
await XFEConsole.WriteLineAsync("Hello World!");   // Asynchronous write line
await XFEConsole.WriteAsync("Hello ");             // Asynchronous write (no newline)
```

### Output Object Information

```csharp
await XFEConsole.WriteObject(myObject);                                         // Output object details
await XFEConsole.WriteObject(myObject, onlyProperty: true, onlyPublic: true);  // Only public properties
await XFEConsole.WriteObject(myObject, remarkName: "User Object");              // Custom remark name
```

### Use with XUnit Test Framework

```csharp
class Program
{
    [UseXFEConsole]          // Use default port 3280
    [UseXFEConsole(3280)]    // Or specify the port explicitly
    [SMTest]
    static void TestMethod()
    {
        Console.WriteLine("Output via XUnit framework");
    }
}
```

---

## Logging

### Enable Logging

```csharp
// Enable logging with default settings (file log, timestamps enabled)
XFEConsole.UseXFEConsoleLog();

// Configure with an Action builder
XFEConsole.UseXFEConsoleLog(options =>
{
    options.LogType = LogType.MemoryLog;        // Use in-memory log (default: FileLog)
    options.AutoApplyTimeInfo = true;           // Automatically include timestamps (default: true)
    options.UseAnsiConsoleEncoding = true;      // Enable ANSI encoding (default: true)
    options.LogTextMaximizeLength = 1024 * 10;  // Max log length; -1 = unlimited (default: -1)
});

// Or pass an options object directly
var logOptions = new XFEConsoleLogOptions
{
    LogType = LogType.FileLog,
    AutoApplyTimeInfo = true
};
XFEConsole.UseXFEConsoleLog(logOptions);
```

### Write Logs

```csharp
Console.WriteLine("Hello World!");       // Recorded directly in the log

Console.Write("Hello");                  // Buffered until the next WriteLine
Console.WriteLine(" World!");            // Now "Hello World!" is recorded

Console.WriteLine("[DEBUG]This is a debug message");                // Logged at DEBUG level
Console.WriteLine("[INFO]This is an info message");                 // Logged at INFO level
Console.WriteLine("[TRACE]Throw at Main() on line:24 position:25"); // Logged at TRACE level
Console.WriteLine("[WARN]Low memory warning");                      // Logged at WARN level
Console.WriteLine("[ERROR]Exception thrown");                       // Logged at ERROR level
Console.WriteLine("[FATAL]Application crashed... unknown reason");  // Logged at FATAL level
```

### Configure Log Path

```csharp
XFEConsole.Log.LogPath = "my-app.log";  // Set the log file path (file log only)
```

### Export, Import, and Clear Logs

```csharp
string logText  = XFEConsole.Log.Export();                              // Export all logs as text
string rangeLog = XFEConsole.Log.Export(DateTime.Today, DateTime.Now); // Export by date range
string original = XFEConsole.Log.ExportOriginal();                      // Export raw logs (no escaping)

XFEConsole.Log.Import(logText);                                         // Import log text
XFEConsole.Log.Clear();                                                 // Clear all logs
```
