namespace Topaz.Service.ContainerRegistry.Models.Responses;

internal sealed class CheckNameAvailabilityResponse
{
    public bool NameAvailable { get; init; }
    public string? Reason { get; init; }
    public string? Message { get; init; }
}
