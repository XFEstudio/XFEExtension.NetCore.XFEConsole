# XFEExtension.NetCore.XFEConsole

[![NuGet Version](https://img.shields.io/nuget/v/XFEExtension.NetCore.XFEConsole.svg)](https://www.nuget.org/packages/XFEExtension.NetCore.XFEConsole/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/XFEExtension.NetCore.XFEConsole.svg)](https://www.nuget.org/packages/XFEExtension.NetCore.XFEConsole/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-blueviolet)](https://dotnet.microsoft.com/download)

> 🌐 English | [中文](README_zh.md)

## Overview

XFEExtension.NetCore.XFEConsole is a debugging aid that allows remote console output. It is designed to work with the XFE Toolbox, but you can also build your own debugging tool based on the architecture provided in this library.

## Installation

```shell
dotnet add package XFEExtension.NetCore.XFEConsole
```

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