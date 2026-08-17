using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using XFEExtension.NetCore.FormatExtension;
using XFEExtension.NetCore.XFEConsole;

internal static class RemoteDebugSelfTest
{
    private const int MessageCount = 5_000;

    public static async Task RunAsync()
    {
        var port = GetAvailablePort();
        await using var server = new XFEConsoleProgramServer(port, true, "correct-password", "RemoteTestProgram", "program-1");
        await server.StartAsync();

        await VerifyWrongPasswordIsRejectedAsync(port);
        var messagesPerSecond = await VerifyAuthenticatedOutputAsync(server, port);
        await VerifyStaticConsoleServerApiAsync();
        await VerifyOriginalProgramClientModeAsync();

        Console.WriteLine("PASS wrong password rejected");
        Console.WriteLine("PASS authenticated terminal metadata");
        Console.WriteLine($"PASS {MessageCount:N0} remote output messages at {messagesPerSecond:N0} messages/second");
        Console.WriteLine("PASS XFEConsole.UseXFEConsoleServer output redirection");
        Console.WriteLine("PASS original program-client mode compatibility");
    }

    private static async Task VerifyWrongPasswordIsRejectedAsync(int port)
    {
        await using var terminal = new XFEConsoleTerminalClient($"ws://localhost:{port}/", "wrong-password", "RejectedTerminal", "terminal-rejected");
        var accepted = await terminal.ConnectAsync(TimeSpan.FromSeconds(5));
        if (accepted)
            throw new InvalidOperationException("The server accepted an incorrect password.");
        if (string.IsNullOrWhiteSpace(terminal.AuthenticationFailureReason))
            throw new InvalidOperationException("The server did not return an authentication failure reason.");
    }

    private static async Task<double> VerifyAuthenticatedOutputAsync(XFEConsoleProgramServer server, int port)
    {
        await using var terminal = new XFEConsoleTerminalClient($"ws://localhost:{port}/", "correct-password", "TestToolbox", "terminal-1");
        var received = 0;
        var receivedAll = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        terminal.MessageReceived += (_, message) =>
        {
            var dictionary = new XFEDictionary(message);
            if (dictionary["IsLine"] != "true" || dictionary["Text"] is null)
                receivedAll.TrySetException(new InvalidDataException("Received an invalid console output message."));
            if (Interlocked.Increment(ref received) == MessageCount)
                receivedAll.TrySetResult();
        };

        if (!await terminal.ConnectAsync(TimeSpan.FromSeconds(5)))
            throw new InvalidOperationException($"Authentication failed: {terminal.AuthenticationFailureReason}");
        if (terminal.RemoteProgramName != "RemoteTestProgram" || terminal.RemoteProgramId != "program-1")
            throw new InvalidOperationException("The authenticated program metadata is incorrect.");

        var stopwatch = Stopwatch.StartNew();
        for (var index = 0; index < MessageCount; index++)
            await server.OutputMessage($"line-{index}", true);
        await receivedAll.Task.WaitAsync(TimeSpan.FromSeconds(20));
        stopwatch.Stop();

        if (received != MessageCount)
            throw new InvalidOperationException($"Expected {MessageCount} messages but received {received}.");
        return MessageCount / stopwatch.Elapsed.TotalSeconds;
    }

    private static async Task VerifyStaticConsoleServerApiAsync()
    {
        var port = GetAvailablePort();
        var originalShowInDebug = XFEConsole.ShowInDebug;
        var originalShowInLocalConsole = XFEConsole.ShowInLocalConsole;
        XFEConsole.ShowInDebug = false;
        XFEConsole.ShowInLocalConsole = false;
        try
        {
            await XFEConsole.UseXFEConsoleServer(port, true, "static-password", "StaticProgram", "static-program");
            await using var terminal = new XFEConsoleTerminalClient($"ws://localhost:{port}/", "static-password");
            var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            terminal.MessageReceived += (_, message) => received.TrySetResult(message);
            if (!await terminal.ConnectAsync(TimeSpan.FromSeconds(5)))
                throw new InvalidOperationException("The static XFEConsole server rejected the correct password.");

            Console.WriteLine("static-server-output");
            var dictionary = new XFEDictionary(await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
            if (dictionary["Text"]?.Contains("static-server-output", StringComparison.Ordinal) != true)
                throw new InvalidOperationException("Console.Out was not redirected to the program server.");
        }
        finally
        {
            await XFEConsole.StopXFEConsole();
            XFEConsole.ShowInDebug = originalShowInDebug;
            XFEConsole.ShowInLocalConsole = originalShowInLocalConsole;
        }
    }

    private static async Task VerifyOriginalProgramClientModeAsync()
    {
        var port = GetAvailablePort();
        var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var server = new XFEConsoleTerminalServer(port, true, "original-password");
        server.Connected += (_, _) => connected.TrySetResult();
        server.MessageReceived += (_, message) => received.TrySetResult(message);
        await server.StartAsync();
        try
        {
            var rejectedClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var rejectedClient = new XFEConsoleProgramClient($"ws://localhost:{port}/", "RejectedProgram", "rejected-program", "wrong-password");
            rejectedClient.Client.ConnectionClosedHandler = cancellationToken =>
            {
                rejectedClosed.TrySetResult();
                return ValueTask.CompletedTask;
            };
            await rejectedClient.Connect();
            await rejectedClosed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (connected.Task.IsCompleted)
                throw new InvalidOperationException("The original terminal server authenticated an incorrect password.");

            var client = new XFEConsoleProgramClient($"ws://localhost:{port}/", "OriginalProgram", "original-program", "original-password");
            if (!await client.Connect())
                throw new InvalidOperationException("The original program client could not connect.");
            await connected.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await client.OutputMessage("original-mode-output", true);
            var dictionary = new XFEDictionary(await received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
            if (dictionary["Text"] != "original-mode-output")
                throw new InvalidOperationException("The original program-client output was not preserved.");
            await client.Client.DisconnectAsync();
        }
        finally
        {
            await server.StopAsync();
        }
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
