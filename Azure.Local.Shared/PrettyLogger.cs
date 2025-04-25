using Spectre.Console;

namespace Azure.Local.Shared;

public sealed class PrettyLogger
{
    public static void LogInformation(string message)
    {
        Log(message, LogLevel.Information);
    }

    public static void LogDebug(string message)
    {
        Log(message, LogLevel.Debug);
    }

    private static void Log(string message, LogLevel logLevel)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        AnsiConsole.WriteLine($"[{logLevel}][{timestamp}]: {message}");
    }
}

internal enum LogLevel
{
    Debug,
    Information
}
