using XFEExtension.NetCore.XFEConsole;

await XFEConsole.UseXFEConsole();
Console.WriteLine("准备测试...");
await Task.Delay(1000);
Console.WriteLine("3");
await Task.Delay(1000);
Console.WriteLine("2");
await Task.Delay(1000);
Console.WriteLine("1");
await Task.Delay(1000);
for (int i = 0; i < 10000; i++)
{
    Console.WriteLine($"循环压力测试：{i}");
}
await Task.Delay(1000);
Console.WriteLine("测试结束");
await Task.Delay(3000);