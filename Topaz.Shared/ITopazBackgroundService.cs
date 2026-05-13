namespace Topaz.Shared;

/// <summary>
/// Represents a long-running background service that is started once during host startup
/// and runs for the lifetime of the process.
/// </summary>
public interface ITopazBackgroundService
{
    /// <summary>Display name shown in the host startup output.</summary>
    string Name { get; }

    /// <summary>
    /// Starts the background service loop and keeps running until
    /// <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);
}
