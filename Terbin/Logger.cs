using System;

namespace Terbin;

public enum LogLevel
{
    Info,
    Success,
    Warning,
    Error
}

public class Logger
{
    private readonly object _lock = new();
    public bool UseColors { get; set; } = true;

    private void WithColor(ConsoleColor color, Action action)
    {
        if (!UseColors)
        {
            action();
            return;
        }

        var prev = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            action();
        }
        finally
        {
            Console.ForegroundColor = prev;
        }
    }

    private static string Prefix(LogLevel level) => level switch
    {
        LogLevel.Info => "[INFO]",
        LogLevel.Success => "[OK]  ",
        LogLevel.Warning => "[WARN]",
        LogLevel.Error => "[ERR ]",
        _ => "[LOG ]"
    };

    public void Log(LogLevel level, string message)
    {
        lock (_lock)
        {
            var line = $"{Prefix(level)} {message}";
            switch (level)
            {
                case LogLevel.Info:
                    WithColor(ConsoleColor.Gray, () => Console.WriteLine(line));
                    break;
                case LogLevel.Success:
                    WithColor(ConsoleColor.Green, () => Console.WriteLine(line));
                    break;
                case LogLevel.Warning:
                    WithColor(ConsoleColor.Yellow, () => Console.WriteLine(line));
                    break;
                case LogLevel.Error:
                    WithColor(ConsoleColor.Red, () => Console.Error.WriteLine(line));
                    break;
                default:
                    Console.WriteLine(line);
                    break;
            }
        }
    }

    public void Info(string message) => Log(LogLevel.Info, message);
    public void Success(string message) => Log(LogLevel.Success, message);
    public void Warn(string message) => Log(LogLevel.Warning, message);
    public void Error(string message) => Log(LogLevel.Error, message);

    public void Section(string title)
    {
        lock (_lock)
        {
            var bar = new string('=', Math.Min(Math.Max(title.Length, 10), 30));
            WithColor(ConsoleColor.Cyan, () => Console.WriteLine($"\n{bar}"));
            WithColor(ConsoleColor.Cyan, () => Console.WriteLine(title));
            WithColor(ConsoleColor.Cyan, () => Console.WriteLine($"{bar}\n"));
        }
    }

    public string Ask(string prompt)
    {
        lock (_lock)
        {
            if (UseColors)
            {
                var prev = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(prompt);
                Console.ForegroundColor = prev;
            }
            else
            {
                Console.Write(prompt);
            }
        }
        return Console.ReadLine() ?? string.Empty;
    }

    public bool Confirm(string message, bool defaultNo = true)
    {
        var suffix = defaultNo ? " (y/N): " : " (Y/n): ";
        lock (_lock)
        {
            if (UseColors)
            {
                var prev = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(message);
                Console.ForegroundColor = prev;
            }
            else
            {
                Console.Write(message);
            }
            Console.Write(suffix);
        }
        var key = Console.ReadKey();
        Console.WriteLine();
        return key.Key == ConsoleKey.Y;
    }

    public void Box(string title, IEnumerable<string> lines)
    {
        lock (_lock)
        {
            var maxLen = Math.Max(title.Length, lines.Any() ? lines.Max(l => l.Length) : 0);
            var bar = new string('-', Math.Min(Math.Max(maxLen, 16), 60));
            WithColor(ConsoleColor.Cyan, () => Console.WriteLine());
            WithColor(ConsoleColor.Cyan, () => Console.WriteLine(title));
            WithColor(ConsoleColor.Cyan, () => Console.WriteLine(bar));
            foreach (var l in lines)
            {
                Console.WriteLine(l);
            }
            WithColor(ConsoleColor.Cyan, () => Console.WriteLine(bar));
            WithColor(ConsoleColor.Cyan, () => Console.WriteLine());
        }
    }
}
