namespace Topaz.Service.Storage.Models;

public enum ContainerLeaseState
{
    Available,
    Leased,
    Breaking,
    Broken,
    Expired
}

public sealed class ContainerLease
{
    public string? LeaseId { get; set; }
    public ContainerLeaseState State { get; set; } = ContainerLeaseState.Available;
    /// <summary>Duration in seconds; -1 means infinite.</summary>
    public int Duration { get; set; }
    /// <summary>UTC time when the lease expires; null for infinite leases.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }
    /// <summary>UTC time when a Breaking lease becomes Broken.</summary>
    public DateTimeOffset? BreakTime { get; set; }

    /// <summary>Returns true if the lease has passed its expiry time and should be treated as Available.</summary>
    public bool IsExpired()
    {
        if (State == ContainerLeaseState.Leased && ExpiresAt.HasValue && DateTimeOffset.UtcNow >= ExpiresAt.Value)
            return true;
        if (State == ContainerLeaseState.Breaking && BreakTime.HasValue && DateTimeOffset.UtcNow >= BreakTime.Value)
            return true;
        return false;
    }

    public ContainerLeaseState EffectiveState()
    {
        if (State == ContainerLeaseState.Leased && ExpiresAt.HasValue && DateTimeOffset.UtcNow >= ExpiresAt.Value)
            return ContainerLeaseState.Expired;
        if (State == ContainerLeaseState.Breaking && BreakTime.HasValue && DateTimeOffset.UtcNow >= BreakTime.Value)
            return ContainerLeaseState.Broken;
        return State;
    }
}
