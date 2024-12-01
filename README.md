# XFEExtension.NetCore.XFEConsole

## 简述

XFEExtension.NetCore.XFEConsole是一个可以允许用户进行远程输出的调试辅助工具，需要配合XFE工具箱来使用，当然也可以根据本DLL内的架构搭建一个调试工具

## 远程控制台

### 输出至远程控制台

```csharp
await XFEConsole.UseXFEConsole(3280);   // 使用XFE控制台并连接端口为3280的控制台调试终端
                                        // await任务返回一个bool值，该值指示是否与调试终端连接成功

XFEConsole.ShowInDebug = true;          // 是否在本地调试的Debug中展示，默认为 true
XFEConsole.UseConsoleColor = true;      // 使用控制台颜色，默认为 true
XFEConsole.ShowInLocalConsole = true;   // 是否在本地控制台中显示，默认为 false
XFEConsole.AutoAnalyzeObject = true;    // 是否自动解析对象，而非直接输出对象的.ToString()方法，默认为 true

Console.WriteLine("Hello World!");      // 这条语句会在远程控制台中输出Hello World!
```

### 配合XUnit测试框架

```csharp
class Program
{
    [UseXFEConsole]
    [SMTest]
    static void TestMethod()
    {
        Console.WriteLine("使用XUnit框架输出");
    }
}
```

---

## 日志

### 记录日志
```csharp
XFEConsole.UseXFEConsoleLog();          // 使用XFE日志记录每一次Console.Write或者WriteLine的内容
Console.WriteLine("Hello World!");      // 会直接在日志中记录Hello World!

Console.Write("Hello");                 // 缓存直到下一个Console.WriteLine的出现
Console.WriteLine(" World!");           // 此时才会记录Hello World!

Console.WriteLine("[DEBUG]This is a debug message");                // 会记录级别为DEBUG的日志
Console.WriteLine("[INFO]This is a info");                          // 会记录级别为INFO的日志
Console.WriteLine("[TRACE]Throw at Main() on line:24 position:25"); // 会记录级别为TRACE的日志
Console.WriteLine("[ERROR]Exception thrown");                       // 会记录级别为ERROR的日志
Console.WriteLine("[FATAL]Application crashed... unknown reason");  // 会记录级别为FATAL的日志
```

### 导入导出日志
```csharp
var log = XFEConsole.Log.Export();      // 导出日志为文本
XFEConsole.Log.Clear()                  // 清除全部日志
XFEConsole.Log.Import(log);             // 导入日志文本
```