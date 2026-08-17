using XFEExtension.NetCore.XFEConsole;
using XFEExtension.NetCore.XFEConsole.Terminal;

var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "help";
switch (command)
{
    case "self-test":
        RunSelfTests();
        break;
    case "remote-debug-self-test":
        await RemoteDebugSelfTest.RunAsync();
        break;
    case "art":
        ShowArt();
        break;
    case "progress":
        await ShowProgressAsync();
        break;
    case "game":
        await RunGameAsync();
        break;
    case "log":
        await RunLogStressDemoAsync();
        break;
    default:
        Console.WriteLine("XFEConsole feature demo");
        Console.WriteLine("  self-test  Run deterministic API checks");
        Console.WriteLine("  remote-debug-self-test  Verify server mode authentication and throughput");
        Console.WriteLine("  art        Show every title-art style and built-in font");
        Console.WriteLine("  progress   Show inline + Windows Terminal taskbar progress");
        Console.WriteLine("  game       Run a keyboard/mouse canvas demo (Esc exits)");
        Console.WriteLine("  log        Run the original parallel logging stress demo");
        break;
}

static void RunSelfTests()
{
    var tests = new (string Name, Action Test)[]
    {
        ("VT cursor sequence", () => Equal("\x1b[3;7H", XFETerminalSequences.CursorPosition(3, 7))),
        ("Windows Terminal progress sequence", () => Equal("\x1b]9;4;1;50\x07", XFETerminalSequences.SetProgress(XFETerminalProgressState.Normal, 50))),
        ("Shell integration mark", () => Equal("\x1b]133;D;2\x1b\\", XFETerminalSequences.ShellMark(XFETerminalShellMark.CommandFinished, 2))),
        ("OSC 8 hyperlink", () => Equal("\x1b]8;;https://example.com/\x1b\\docs\x1b]8;;\x1b\\", XFETerminalSequences.Hyperlink("docs", new Uri("https://example.com")))),
        ("RGB style", () => Contains("38;2;1;2;3", new XFETerminalStyle { Foreground = XFETerminalColor.FromRgb(1, 2, 3) }.ToSequence())),
        ("Legacy title art", TestLegacyTitleArt),
        ("Every title art style", TestEveryTitleArtStyle),
        ("Built-in font catalog", TestBuiltInFontCatalog),
        ("Outlined title art", TestOutlinedTitleArt),
        ("3D title art", TestThreeDimensionalTitleArt),
        ("Layered title art color", TestLayeredTitleArtColor),
        ("Modern title art color", TestModernTitleArt),
        ("Unicode title fallback", TestUnicodeTitleArt),
        ("Canvas plain rendering", TestCanvas),
        ("Progress rendering", TestProgress),
        ("Invalid progress rejected", () => Throws<ArgumentOutOfRangeException>(() => XFETerminalSequences.SetProgress(XFETerminalProgressState.Normal, 101)))
    };

    var failures = new List<string>();
    foreach (var test in tests)
    {
        try
        {
            test.Test();
            Console.WriteLine($"PASS {test.Name}");
        }
        catch (Exception exception)
        {
            failures.Add($"FAIL {test.Name}: {exception.Message}");
            Console.WriteLine(failures[^1]);
        }
    }

    if (failures.Count > 0)
        throw new InvalidOperationException($"{failures.Count} self-test(s) failed.");
    Console.WriteLine($"All {tests.Length} self-tests passed.");
}

static void TestLegacyTitleArt()
{
    var art = XFETerminalTitleArt.GeneratePlain(
        "A",
        XFETerminalArtStyle.Compact,
        XFETerminalCompatibility.Legacy);
    Equal(string.Join(Environment.NewLine, " ###", "#   #", "#####", "#   #", "#   #"), art);
    DoesNotContain("\x1b", art);
}

static void TestUnicodeTitleArt()
{
    var art = XFETerminalTitleArt.GeneratePlain(
        "终端",
        XFETerminalArtStyle.Block,
        XFETerminalCompatibility.Legacy);
    Equal(string.Join(Environment.NewLine, "+------+", "| 终端 |", "+------+"), art);
}

static void TestModernTitleArt()
{
    var art = XFETerminalTitleArt.Generate("XFE", new XFETerminalTitleArtOptions
    {
        Style = XFETerminalArtStyle.Shadow,
        Palette = XFETerminalArtPalette.Rainbow,
        Compatibility = XFETerminalCompatibility.Modern
    });
    Contains("\x1b[", art);
    Contains("█", art);
    Contains("▓", art);
}

static void TestBuiltInFontCatalog()
{
    Equal(Enum.GetValues<XFETerminalArtFont>().Length, XFETerminalTitleArt.AvailableFonts.Count);
    var legacyResults = new HashSet<string>(StringComparer.Ordinal);
    var modernResults = new HashSet<string>(StringComparer.Ordinal);
    foreach (var font in XFETerminalTitleArt.AvailableFonts)
    {
        var legacy = XFETerminalTitleArt.GeneratePlain("XFE", new XFETerminalTitleArtOptions
        {
            Font = font,
            Style = XFETerminalArtStyle.Compact,
            Compatibility = XFETerminalCompatibility.Legacy
        });
        var modern = XFETerminalTitleArt.GeneratePlain("XFE", new XFETerminalTitleArtOptions
        {
            Font = font,
            Style = XFETerminalArtStyle.Compact,
            Compatibility = XFETerminalCompatibility.Modern
        });
        if (string.IsNullOrWhiteSpace(legacy) || string.IsNullOrWhiteSpace(modern))
            throw new InvalidOperationException($"{font} rendered an empty string.");
        DoesNotContain("\x1b", legacy);
        DoesNotContain("\x1b", modern);
        if (!legacyResults.Add(legacy))
            throw new InvalidOperationException($"{font} did not produce a distinct legacy visual result.");
        if (!modernResults.Add(modern))
            throw new InvalidOperationException($"{font} did not produce a distinct visual result.");
    }

    var art = XFETerminalTitleArt.GeneratePlain("XFE", new XFETerminalTitleArtOptions
    {
        Font = XFETerminalArtFont.Epic,
        Style = XFETerminalArtStyle.Compact,
        Compatibility = XFETerminalCompatibility.Modern
    });
    Contains("═", art);
    if (art.Split(Environment.NewLine).Length < 5)
        throw new InvalidOperationException("Epic did not render as multi-line built-in art.");
}

static void TestOutlinedTitleArt()
{
    var art = XFETerminalTitleArt.GeneratePlain("I", new XFETerminalTitleArtOptions
    {
        Style = XFETerminalArtStyle.Compact,
        Compatibility = XFETerminalCompatibility.Legacy,
        OutlineWidth = 1,
        OutlineCharacter = '+'
    });
    Equal(string.Join(Environment.NewLine,
        "+++++++",
        "+#####+",
        "+++#+++",
        "  +#+",
        "+++#+++",
        "+#####+",
        "+++++++"), art);
    DoesNotContain("\x1b", art);
}

static void TestThreeDimensionalTitleArt()
{
    var art = XFETerminalTitleArt.GeneratePlain("I", new XFETerminalTitleArtOptions
    {
        Style = XFETerminalArtStyle.Compact,
        Compatibility = XFETerminalCompatibility.Legacy,
        ExtrudeDepth = 2,
        ExtrudeDirection = XFETerminalArtExtrudeDirection.Right,
        ExtrudeCharacter = '>'
    });
    Equal(string.Join(Environment.NewLine,
        "#####>>",
        "  #>>",
        "  #>>",
        "  #>>",
        "#####>>"), art);
}

static void TestLayeredTitleArtColor()
{
    var art = XFETerminalTitleArt.Generate("I", new XFETerminalTitleArtOptions
    {
        Style = XFETerminalArtStyle.Compact,
        Compatibility = XFETerminalCompatibility.Modern,
        Color = XFETerminalColor.FromRgb(1, 2, 3),
        OutlineWidth = 1,
        OutlineColor = XFETerminalColor.FromRgb(4, 5, 6),
        ExtrudeDepth = 1,
        ExtrudeColor = XFETerminalColor.FromRgb(7, 8, 9)
    });
    Contains("38;2;1;2;3", art);
    Contains("38;2;4;5;6", art);
    Contains("38;2;7;8;9", art);
}

static void TestEveryTitleArtStyle()
{
    foreach (var style in Enum.GetValues<XFETerminalArtStyle>())
    {
        var art = XFETerminalTitleArt.GeneratePlain("XFE", style, XFETerminalCompatibility.Legacy);
        if (string.IsNullOrWhiteSpace(art))
            throw new InvalidOperationException($"{style} rendered an empty string.");
    }
}

static void TestCanvas()
{
    var canvas = new XFETerminalCanvas(4, 2);
    canvas.DrawText(0, 0, "AB");
    Equal("AB  \n    ", canvas.Render(includeStyles: false));
    DoesNotContain("\x1b", canvas.Render());
    canvas.Set(-1, 0, 'X');
    Equal('A', canvas[0, 0].Character);
    using var writer = new StringWriter();
    canvas.Present(writer, compatibility: XFETerminalCompatibility.Legacy);
    Equal("AB  \n    ", writer.ToString());
    canvas.Invalidate();
    using var modernWriter = new StringWriter();
    canvas.Present(modernWriter, compatibility: XFETerminalCompatibility.Modern);
    Contains(XFETerminalSequences.CursorPosition(1, 1), modernWriter.ToString());
}

static void TestProgress()
{
    using var progress = new XFETerminalProgressBar(new XFETerminalProgressBarOptions
    {
        Width = 4,
        CompletedCharacter = '#',
        RemainingCharacter = '-',
        ShowPercentage = false,
        UseTaskbarProgress = false,
        WriteLineOnDispose = false,
        Compatibility = XFETerminalCompatibility.Legacy
    }, TextWriter.Null);
    Equal("[##--]", progress.Render(0.5));

    using var legacyDefaults = new XFETerminalProgressBar(new XFETerminalProgressBarOptions
    {
        Width = 4,
        ShowPercentage = false,
        UseTaskbarProgress = false,
        WriteLineOnDispose = false,
        Compatibility = XFETerminalCompatibility.Legacy
    }, TextWriter.Null);
    Equal("[##--]", legacyDefaults.Render(0.5));
}

static void ShowArt()
{
    foreach (var style in Enum.GetValues<XFETerminalArtStyle>())
    {
        Console.WriteLine($"\n{style}");
        XFETerminalTitleArt.Write("XFE", new XFETerminalTitleArtOptions
        {
            Style = style,
            Palette = XFETerminalArtPalette.Rainbow
        });
    }

    Console.WriteLine($"\nAll built-in fonts ({XFETerminalTitleArt.AvailableFonts.Count})");
    for (var index = 0; index < XFETerminalTitleArt.AvailableFonts.Count; index++)
    {
        var font = XFETerminalTitleArt.AvailableFonts[index];
        Console.WriteLine($"\n[{index + 1}/{XFETerminalTitleArt.AvailableFonts.Count}] {font}");
        XFETerminalTitleArt.Write("XFE", new XFETerminalTitleArtOptions
        {
            Font = font,
            Style = XFETerminalArtStyle.Compact,
            Palette = XFETerminalArtPalette.Ocean
        });
    }

    Console.WriteLine("\nOutlined + 3D");
    XFETerminalTitleArt.Write("XFE", new XFETerminalTitleArtOptions
    {
        Font = XFETerminalArtFont.Doom,
        Style = XFETerminalArtStyle.ThreeDimensional,
        Palette = XFETerminalArtPalette.Sunset,
        OutlineWidth = 1,
        OutlineColor = XFETerminalColor.FromRgb(0xff, 0xee, 0xc2),
        ExtrudeDepth = 4,
        ExtrudeColor = XFETerminalColor.FromRgb(0x60, 0x24, 0x60),
        ExtrudeDirection = XFETerminalArtExtrudeDirection.DownRight
    });

    XFETerminalTitleArt.Write("终端艺术字", new XFETerminalTitleArtOptions
    {
        Style = XFETerminalArtStyle.Framed,
        Palette = XFETerminalArtPalette.Ocean
    });
}

static async Task ShowProgressAsync()
{
    using var progress = XFETerminal.CreateProgressBar(new XFETerminalProgressBarOptions
    {
        Prefix = "Building ",
        UseTaskbarProgress = true
    });
    for (var value = 0; value <= 100; value++)
    {
        progress.Report(value / 100d, $"step {value}/100");
        await Task.Delay(25);
    }
}

static async Task RunGameAsync()
{
    var playerX = 4;
    var playerY = 4;
    await XFETerminalGame.RunAsync((context, _) =>
    {
        foreach (var key in context.InputEvents.OfType<XFETerminalKeyEvent>().Where(input => input.IsKeyDown))
        {
            if (key.Key is ConsoleKey.LeftArrow or ConsoleKey.A) playerX--;
            if (key.Key is ConsoleKey.RightArrow or ConsoleKey.D) playerX++;
            if (key.Key is ConsoleKey.UpArrow or ConsoleKey.W) playerY--;
            if (key.Key is ConsoleKey.DownArrow or ConsoleKey.S) playerY++;
        }
        foreach (var mouse in context.InputEvents.OfType<XFETerminalMouseEvent>().Where(input => input.Action == XFEMouseAction.ButtonPressed))
        {
            playerX = mouse.X;
            playerY = mouse.Y;
        }

        playerX = Math.Clamp(playerX, 1, Math.Max(1, context.Canvas.Width - 2));
        playerY = Math.Clamp(playerY, 2, Math.Max(2, context.Canvas.Height - 2));
        context.Canvas.Clear();
        context.Canvas.DrawBox(0, 0, context.Canvas.Width, context.Canvas.Height, XFETerminalBoxStyle.Rounded,
            new XFETerminalStyle { Foreground = XFETerminalColor.Cyan });
        context.Canvas.DrawText(2, 1, "Arrows/WASD or mouse • Esc exits",
            new XFETerminalStyle { Foreground = XFETerminalColor.Yellow, Bold = true });
        context.Canvas.Set(playerX, playerY, '@', new XFETerminalStyle
        {
            Foreground = XFETerminalColor.FromRgb(0xff, 0x4d, 0x8d),
            Bold = true
        });
        return ValueTask.CompletedTask;
    }, new XFETerminalGameOptions { FramesPerSecond = 30 });
}

static async Task RunLogStressDemoAsync()
{
    XFEConsole.UseXFEConsoleLog();
    XFEConsole.Log.LogPath = "test.log";
    await Parallel.ForAsync(0, 10_000, async (index, _) =>
    {
        var levels = new[] { "[INFO]", "[WARN]", "[ERROR]", "[DEBUG]", "[TRACE]", "[FATAL]" };
        Console.Write($"{levels[Random.Shared.Next(levels.Length)]}这是第 {index} 条日志的上半部分");
        await Task.Delay(10);
        Console.WriteLine($"，这是第 {index} 条日志的下半部分");
    });
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected [{expected}], actual [{actual}].");
}

static void Contains(string expected, string actual)
{
    if (!actual.Contains(expected, StringComparison.Ordinal))
        throw new InvalidOperationException($"Expected [{actual}] to contain [{expected}].");
}

static void DoesNotContain(string expected, string actual)
{
    if (actual.Contains(expected, StringComparison.Ordinal))
        throw new InvalidOperationException($"Expected [{actual}] not to contain [{expected}].");
}

static void Throws<TException>(Action action) where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }
    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}
