namespace Topaz.Shared;

/// <summary>
/// A no-op logger used in CLI mode so that internal control-plane and
/// resource-provider diagnostics are not written to the user's terminal.
/// CLI commands write user-facing output directly via AnsiConsole.
/// </summary>
public sealed class SilentTopazLogger : ITopazLogger
{
    public LogLevel LogLevel => LogLevel.Error;

    public void LogInformation(string message) { }
    public void LogDebug(string message) { }
    public void LogDebug(string methodName, string message) { }
    public void LogDebug(string className, string methodName, params object[] parameters) { }
    public void LogDebug(string className, string methodName, string template, params object?[] parameters) { }
    public void LogError(Exception ex) { }
    public void LogError(string message) { }
    public void LogError(string className, string methodName, string template, params object?[] parameters) { }
    public void LogWarning(string message) { }
    public void SetLoggingLevel(LogLevel level) { }
    public void EnableLoggingToFile(bool refreshLog) { }
    public void ConfigureIdFactory(CorrelationIdFactory idFactory) { }
}
