using XFEExtension.NetCore.FileExtension;
using XFEExtension.NetCore.XFEConsole;

var log = new XFELog();
log.Import(@"C:\Users\XFEstudio\Documents\2025年01月23日_00时00分00秒至2025年01月30日_23时59分59秒-日志文件.log".ReadOut()!);
Console.WriteLine(log.Export());