using XFEExtension.NetCore.XFEConsole;

XFEConsole.UseXFEConsoleLog();

XFEConsole.Log.LogPath = "test.log";

await Parallel.ForAsync(0, 10000, async (i, ct) =>
{
    await Do(i);
});

//var xFELog = new XFEFileLog();
//await Parallel.ForAsync(0, 1000, async (i, ct) =>
//{
//    xFELog.Write($"这是第 {i} 条日志的上半部分", out var isHead);
//    await Task.Delay(10, ct);
//    xFELog.WriteLine($"，这是第 {i} 条日志的下半部分", out isHead);
//});

static async Task Do(int i)
{
    Console.Write($"这是第 {i} 条日志的上半部分");
    await Task.Delay(10);
    Console.WriteLine($"，这是第 {i} 条日志的下半部分");
}

//Console.WriteLine("测试");
//var logs = XFEConsole.Log.Logs;
//while (true)
//{
//    Console.WriteLine(XFEConsole.Log.Export(DateTime.MinValue, DateTime.MaxValue).Length);
//}