using System.Text;

namespace XFEExtension.NetCore.XFEConsole;

/// <summary>
/// XFE控制台文本写入器
/// </summary>
public class XFEConsoleTextWriter(TextWriter originalTextWriter) : TextWriter
{
    /// <inheritdoc/>
    public override Encoding Encoding => Encoding.UTF8;

    /// <summary>
    /// 原TextWriter
    /// </summary>
    public TextWriter OriginalTextWriter { get; set; } = originalTextWriter;

    /// <inheritdoc/>
    public override void Write(bool value)
    {
        XFEConsole.Write(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void Write(char value)
    {
        XFEConsole.Write(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void Write(char[]? buffer)
    {
        if (buffer is not null)
        {
            var stringBuilder = new StringBuilder();
            foreach (var sigChar in buffer)
            {
                stringBuilder.Append(sigChar);
            }
            XFEConsole.Write(stringBuilder.ToString()).Wait();
        }
    }

    /// <inheritdoc/>
    public override void Write(char[] buffer, int index, int count)
    {
        for (int i = index; i < count; i++)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(buffer[i]);
            XFEConsole.Write(stringBuilder.ToString()).Wait();
        }
    }

    /// <inheritdoc/>
    public override void Write(decimal value)
    {
        XFEConsole.Write(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void Write(double value)
    {
        XFEConsole.Write(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void Write(int value)
    {
        XFEConsole.Write(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void Write(long value)
    {
        XFEConsole.Write(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void Write(object? value)
    {
        XFEConsole.Write(value?.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void Write(float value)
    {
        XFEConsole.Write(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void Write(string? value)
    {
        XFEConsole.Write(value).Wait();
    }

    /// <inheritdoc/>
    public override void Write(StringBuilder? value)
    {
        XFEConsole.Write(value?.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void Write(uint value)
    {
        XFEConsole.Write(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void Write(ulong value)
    {
        XFEConsole.Write(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override async Task WriteAsync(char value)
    {
        await XFEConsole.Write(value.ToString());
    }

    /// <inheritdoc/>
    public override async Task WriteAsync(char[] buffer, int index, int count)
    {
        for (int i = index; i < count; i++)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(buffer[i]);
            await XFEConsole.Write(stringBuilder.ToString());
        }
    }

    /// <inheritdoc/>
    public override async Task WriteAsync(string? value)
    {
        await XFEConsole.Write(value);
    }

    /// <inheritdoc/>
    public override void WriteLine()
    {
        XFEConsole.WriteLine("").Wait();
    }

    /// <inheritdoc/>
    public override void WriteLine(bool value)
    {
        XFEConsole.WriteLine(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void WriteLine(char value)
    {
        XFEConsole.WriteLine(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void WriteLine(char[]? buffer)
    {
        if (buffer is not null)
        {
            var stringBuilder = new StringBuilder();
            foreach (var sigChar in buffer)
            {
                stringBuilder.Append(sigChar);
            }
            XFEConsole.WriteLine(stringBuilder.ToString()).Wait();
        }
    }

    /// <inheritdoc/>
    public override void WriteLine(char[] buffer, int index, int count)
    {
        for (int i = index; i < count; i++)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(buffer[i]);
            XFEConsole.WriteLine(stringBuilder.ToString()).Wait();
        }
    }

    /// <inheritdoc/>
    public override void WriteLine(decimal value)
    {
        XFEConsole.WriteLine(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void WriteLine(double value)
    {
        XFEConsole.WriteLine(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void WriteLine(int value)
    {
        XFEConsole.WriteLine(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void WriteLine(long value)
    {
        XFEConsole.WriteLine(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void WriteLine(object? value)
    {
        if (value is Exception exception)
            XFEConsole.WriteLine($"[foldblock color: white #ff0000 title: 错误：{exception.Message} text: {exception}]").Wait();
        if (XFEConsole.AutoAnalyzeObject)
            XFEConsole.WriteObject(value).Wait();
        else
            XFEConsole.WriteLine(value?.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void WriteLine(float value)
    {
        XFEConsole.WriteLine(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void WriteLine(string? value)
    {
        XFEConsole.WriteLine(value).Wait();
    }

    /// <inheritdoc/>
    public override void WriteLine(StringBuilder? value)
    {
        XFEConsole.WriteLine(value?.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void WriteLine(uint value)
    {
        XFEConsole.WriteLine(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override void WriteLine(ulong value)
    {
        XFEConsole.WriteLine(value.ToString()).Wait();
    }

    /// <inheritdoc/>
    public override async Task WriteLineAsync()
    {
        await XFEConsole.WriteLine("");
    }

    /// <inheritdoc/>
    public override async Task WriteLineAsync(char value)
    {
        await XFEConsole.WriteLine(value.ToString());
    }

    /// <inheritdoc/>
    public override async Task WriteLineAsync(char[] buffer, int index, int count)
    {
        for (int i = index; i < count; i++)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(buffer[i]);
            await XFEConsole.WriteLine(stringBuilder.ToString());
        }
    }

    /// <inheritdoc/>
    public override async Task WriteLineAsync(string? value)
    {
        await XFEConsole.WriteLine(value);
    }
}
