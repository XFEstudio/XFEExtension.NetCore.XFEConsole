# XFEExtension.NetCore.XFEConsole

## 简述

XFEExtension.NetCore.XFEConsole是一个可以运行用户进行远程输出的调试辅助工具，需要配合XFE工具箱来使用，当然也可以根据本DLL内的架构搭建一个调试工具

## 使用方法

```csharp
await XFEConsole.UseXFEConsole(3280);   // 使用XFE控制台并连接端口为3280的控制台调试终端
                                        // await任务返回一个bool值，该值指示是否与调试终端连接成功

XFEConsole.ShowInDebug = true;          // 是否在本地调试的Debug中展示，默认为 true
XFEConsole.UseConsoleColor = true;      // 使用控制台颜色，默认为 true
XFEConsole.ShowInLocalConsole = false;  // 是否在本地控制台中显示，默认为 false
XFEConsole.AutoAnalyzeObject = true;    // 是否自动解析对象，而非直接输出对象的.ToString()方法，默认为 true
```

## 配合XUnit测试框架

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